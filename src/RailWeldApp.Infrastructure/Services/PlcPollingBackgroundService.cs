using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailWeldApp.Interfaces;
using RailWeldApp.Models;
using RailWeldApp.Options;

namespace RailWeldApp.Services;

public sealed class PlcPollingBackgroundService(
    IPlcService plcService,
    PlcDataCache dataCache,
    IOptions<RealtimeDataOptions> realtimeOptions,
    ILogger<PlcPollingBackgroundService> logger)
    : IDisposable, IAsyncDisposable, IPlcPollingBackgroundService
{
    private readonly IPlcService _plcService = plcService;
    private readonly PlcDataCache _dataCache = dataCache;
    private readonly ILogger<PlcPollingBackgroundService> _logger = logger;
    private readonly RealtimeDataOptions _realtime = realtimeOptions.Value;

    /// <summary>
    ///  轮询间隔
    /// </summary>
    private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    ///  重连间隔
    /// </summary>
    private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(3);

    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private int _disposed;

    // PLC 离线时每 3 秒重试一次；同一错误 1 分钟内只写一条日志。
    private long _lastFailureLogTimestamp;

    private string? _lastFailureLogMessage;
    public bool IsRunning => _pollingTask is { IsCompleted: false };

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

            if (_pollingTask is { IsCompleted: false })
                return; // 已在采集

            EnsureRealtimeConfiguration();

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _pollingTask = PollingLoopAsync(_cts.Token);

            _logger.LogInformation("PLC 采集任务已启动。");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lifecycleLock.WaitAsync().ConfigureAwait(false);

        try
        {
            CancellationTokenSource? cts = _cts;

            Task? task = _pollingTask;

            _cts = null;

            _pollingTask = null;

            if (cts is null)
                return;

            await cts.CancelAsync().ConfigureAwait(false);

            if (task is not null)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // 正常停止
                }
            }

            cts.Dispose();

            _logger.LogInformation("PLC 采集任务已停止。");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private async Task PollingLoopAsync(CancellationToken cancellationToken)
    {
        //_logger.LogInformation("PLC 采集循环已启动。");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var cycleStarted = Environment.TickCount64;

                try
                {
                    if (!_plcService.IsConnected)
                    {
                        _dataCache.IsConnected = false;

                        var connected = await _plcService.ConnectAsync(cancellationToken).ConfigureAwait(false);

                        // 连接失败
                        if (!connected)
                        {
                            _dataCache.LastError = _plcService.LastError ?? "PLC 连接失败";

                            await Task.Delay(_reconnectInterval, cancellationToken).ConfigureAwait(false);

                            continue;
                        }

                        // 连接成功
                        _dataCache.IsConnected = true;
                        _dataCache.LastError = null;
                    }

                    PlcSnapshot snapshot = await ReadSnapshotAsync(cancellationToken).ConfigureAwait(false);

                    _dataCache.Update(snapshot);
                    _dataCache.IsConnected = true;
                    _dataCache.LastError = null;

                    // 按"周期开始时间"补剩余时间，避免读一次 + Delay(200) 让采样周期越来越长。
                    TimeSpan remaining = _pollInterval - TimeSpan.FromMilliseconds(Environment.TickCount64 - cycleStarted);

                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 采集失败不退出循环：记录状态后等一会儿重试。
                    // 断线判定由 IPlcService 内部的心跳负责，这里只反映它给出的结论。
                    _dataCache.IsConnected = _plcService.IsConnected;
                    _dataCache.LastError = _plcService.LastError ?? ex.Message;

                    LogFailureThrottled(ex, _reconnectInterval);

                    await Task.Delay(_reconnectInterval, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常停止
        }
        finally
        {
            try
            {
                await _plcService.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "停止采集时断开 PLC 失败。");
            }

            _dataCache.IsConnected = false;

            _logger.LogInformation("PLC 采集循环已停止。");
        }
    }

    private void LogFailureThrottled(Exception ex, TimeSpan retryInterval)
    {
        var now = Environment.TickCount64;

        if (ex.Message == _lastFailureLogMessage && now - _lastFailureLogTimestamp < 60_000)
            return;

        _lastFailureLogMessage = ex.Message;

        _lastFailureLogTimestamp = now;

        _logger.LogWarning(ex, "PLC 数据采集失败，{Seconds} 秒后重试。", retryInterval.TotalSeconds);
    }

    private IReadOnlyDictionary<string, string> GetRealtimePoints()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Voltage"] = _realtime.Voltage,
            ["Current"] = _realtime.Current,
            ["Frequency"] = _realtime.Frequency,
            ["Power"] = _realtime.Power,
            ["Energy"] = _realtime.Energy,

            ["XActPos"] = _realtime.XActPos,
            ["XActSpeed"] = _realtime.XActSpeed,
            ["XSetPos"] = _realtime.XSetPos,
            ["XSetSpeed"] = _realtime.XSetSpeed,
            ["XActCurrent"] = _realtime.XActCurrent,

            ["YActPos"] = _realtime.YActPos,
            ["YActSpeed"] = _realtime.YActSpeed,
            ["YSetPos"] = _realtime.YSetPos,
            ["YSetSpeed"] = _realtime.YSetSpeed,
            ["YActCurrent"] = _realtime.YActCurrent,

            ["ZActPos"] = _realtime.ZActPos,
            ["ZActSpeed"] = _realtime.ZActSpeed,
            ["ZSetPos"] = _realtime.ZSetPos,
            ["ZSetSpeed"] = _realtime.ZSetSpeed,
            ["ZActCurrent"] = _realtime.ZActCurrent,

            ["ActTemp1"] = _realtime.ActTemp1,
            ["ActTemp2"] = _realtime.ActTemp2,
            ["ActTemp3"] = _realtime.ActTemp3,
            ["ActTemp4"] = _realtime.ActTemp4,

            ["ActFlow1"] = _realtime.ActFlow1,
            ["ActFlow2"] = _realtime.ActFlow2,
            ["ActFlow3"] = _realtime.ActFlow3,
            ["ActPressure"] = _realtime.ActPressure,

            ["HeatRun"] = _realtime.HeatRun,
            ["CoolingRun"] = _realtime.CoolingRun,

            ["ActAlarm1"] = _realtime.ActAlarm1,
            ["ActAlarm2"] = _realtime.ActAlarm2,

            ["ActLenght1"] = _realtime.ActLenght1,
            ["ActLenght2"] = _realtime.ActLenght2,

            ["Y_CDJ_QGQW"] = _realtime.Y_CDJ_QGQW,
            ["Y_CDJ_QGHW"] = _realtime.Y_CDJ_QGHW,
            ["Z_CDJ_QGQW"] = _realtime.Z_CDJ_QGQW,
            ["Z_CDJ_QGHW"] = _realtime.Z_CDJ_QGHW,

            ["JLG_1_SW"] = _realtime.JLG_1_SW,
            ["JLG_1_XW"] = _realtime.JLG_1_XW,
            ["JLG_2_SW"] = _realtime.JLG_2_SW,
            ["JLG_2_XW"] = _realtime.JLG_2_XW,
            ["JLG_3_SW"] = _realtime.JLG_3_SW,
            ["JLG_3_XW"] = _realtime.JLG_3_XW,
            ["JLG_4_SW"] = _realtime.JLG_4_SW,
            ["JLG_4_XW"] = _realtime.JLG_4_XW,

            ["DY_ZT"] = _realtime.DY_ZT,
            ["PQ_ZT"] = _realtime.PQ_ZT,
            ["ProcessOk"] = _realtime.ProcessOk,
        };
    }

    private void EnsureRealtimeConfiguration()
    {
        foreach (var point in GetRealtimePoints())
        {
            if (string.IsNullOrWhiteSpace(point.Value))
            {
                throw new InvalidOperationException($"RealtimeData:{point.Key} 未配置地址。");
            }

            _ = S7.Net.Types.DataItem.FromAddress(point.Value);
        }
    }

    private async Task<PlcSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> points = GetRealtimePoints();

        IReadOnlyDictionary<string, object?> values = await _plcService
            .ReadManyAsync(points, cancellationToken)
            .ConfigureAwait(false);

        return new PlcSnapshot
        {
            Timestamp = DateTime.Now,
            IsValid = true,

            Voltage = ToFloat(values, "Voltage"),
            Current = ToFloat(values, "Current"),
            Frequency = ToFloat(values, "Frequency"),
            Power = ToFloat(values, "Power"),
            Energy = ToFloat(values, "Energy"),

            XActPos = ToFloat(values, "XActPos"),
            XActSpeed = ToFloat(values, "XActSpeed"),
            XSetPos = ToFloat(values, "XSetPos"),
            XSetSpeed = ToFloat(values, "XSetSpeed"),
            XActCurrent = ToFloat(values, "XActCurrent"),

            YActPos = ToFloat(values, "YActPos"),
            YActSpeed = ToFloat(values, "YActSpeed"),
            YSetPos = ToFloat(values, "YSetPos"),
            YSetSpeed = ToFloat(values, "YSetSpeed"),
            YActCurrent = ToFloat(values, "YActCurrent"),

            ZActPos = ToFloat(values, "ZActPos"),
            ZActSpeed = ToFloat(values, "ZActSpeed"),
            ZSetPos = ToFloat(values, "ZSetPos"),
            ZSetSpeed = ToFloat(values, "ZSetSpeed"),
            ZActCurrent = ToFloat(values, "ZActCurrent"),

            ActTemp1 = ToFloat(values, "ActTemp1"),
            ActTemp2 = ToFloat(values, "ActTemp2"),
            ActTemp3 = ToFloat(values, "ActTemp3"),
            ActTemp4 = ToFloat(values, "ActTemp4"),

            ActFlow1 = ToFloat(values, "ActFlow1"),
            ActFlow2 = ToFloat(values, "ActFlow2"),
            ActFlow3 = ToFloat(values, "ActFlow3"),
            ActPressure = ToFloat(values, "ActPressure"),

            HeatRun = ToBool(values, "HeatRun"),
            CoolingRun = ToBool(values, "CoolingRun"),

            ActAlarm1 = ToWord(values, "ActAlarm1"),
            ActAlarm2 = ToWord(values, "ActAlarm2"),

            ActLenght1 = ToFloat(values, "ActLenght1"),
            ActLenght2 = ToFloat(values, "ActLenght2"),

            Y_CDJ_QGQW = ToBool(values, "Y_CDJ_QGQW"),
            Y_CDJ_QGHW = ToBool(values, "Y_CDJ_QGHW"),
            Z_CDJ_QGQW = ToBool(values, "Z_CDJ_QGQW"),
            Z_CDJ_QGHW = ToBool(values, "Z_CDJ_QGHW"),

            JLG_1_SW = ToBool(values, "JLG_1_SW"),
            JLG_1_XW = ToBool(values, "JLG_1_XW"),
            JLG_2_SW = ToBool(values, "JLG_2_SW"),
            JLG_2_XW = ToBool(values, "JLG_2_XW"),
            JLG_3_SW = ToBool(values, "JLG_3_SW"),
            JLG_3_XW = ToBool(values, "JLG_3_XW"),
            JLG_4_SW = ToBool(values, "JLG_4_SW"),
            JLG_4_XW = ToBool(values, "JLG_4_XW"),

            DY_ZT = ToBool(values, "DY_ZT"),
            PQ_ZT = ToBool(values, "PQ_ZT"),
            ProcessOk = ToBool(values, "ProcessOk"),
        };
    }

    private static float ToFloat(IReadOnlyDictionary<string, object?> values, string name)
    {
        object? value = GetValue(values, name);

        // DataItem.FromAddress 会把 DBD 识别成 DWord，Value 是原始 32 位值。
        // 这里按 IEEE-754 位模式还原成 float，而不是把整数值直接 Convert.ToSingle。
        return value switch
        {
            float f => f,
            double d => (float)d,
            int i => BitConverter.Int32BitsToSingle(i),
            uint u => BitConverter.Int32BitsToSingle(unchecked((int)u)),
            short s => s,
            ushort us => us,
            _ => Convert.ToSingle(value, CultureInfo.InvariantCulture),
        };
    }

    private static bool ToBool(IReadOnlyDictionary<string, object?> values, string name)
    {
        object? value = GetValue(values, name);

        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    private static ushort ToWord(IReadOnlyDictionary<string, object?> values, string name)
    {
        object? value = GetValue(values, name);

        return Convert.ToUInt16(value, CultureInfo.InvariantCulture);
    }

    private static object GetValue(IReadOnlyDictionary<string, object?> values, string name)
    {
        if (!values.TryGetValue(name, out object? value) || value is null)
        {
            throw new InvalidOperationException($"PLC 未返回 {name} 的值。");
        }

        return value;
    }
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await StopAsync().ConfigureAwait(false);

        // _lifecycleLock 不显式释放：单例随进程回收，避免与仍在退出的调用方竞态。
    }
}