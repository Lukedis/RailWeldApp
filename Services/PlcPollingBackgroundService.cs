using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using RailWeldApp.Interfaces;
using RailWeldApp.Models;

namespace RailWeldApp.Services;

public sealed class PlcPollingBackgroundService(IPlcService plcService, PlcDataCache dataCache, ILogger<PlcPollingBackgroundService> logger)
    : IDisposable, IAsyncDisposable, IPlcPollingBackgroundService
{
    private const int _snapshotDbNumber = 4;

    private const int _snapshotByteCount = 141;

    private readonly IPlcService _plcService = plcService;
    private readonly PlcDataCache _dataCache = dataCache;
    private readonly ILogger<PlcPollingBackgroundService> _logger = logger;

    private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(200);
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

                        if (!connected)
                        {
                            _dataCache.LastError = _plcService.LastError ?? "PLC 连接失败";

                            await Task.Delay(_reconnectInterval, cancellationToken).ConfigureAwait(false);

                            continue;
                        }

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

    private async Task<PlcSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        var datas = await _plcService
            .ReadBytesAsync(_snapshotDbNumber, 0, _snapshotByteCount, cancellationToken)
            .ConfigureAwait(false);

        if (datas.Length < _snapshotByteCount)
        {
            throw new InvalidOperationException(
                $"PLC 数据长度不足，预期 {_snapshotByteCount} 字节，实际 {datas.Length} 字节。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new PlcSnapshot
        {
            Timestamp = DateTime.Now,
            IsValid = true,

            // 电气参数（0~19）
            Voltage = ReadFloat(datas, 0),
            Current = ReadFloat(datas, 4),
            Frequency = ReadFloat(datas, 8),
            Power = ReadFloat(datas, 12),
            Energy = ReadFloat(datas, 16),

            // X 轴参数（20~39）
            XActPos = ReadFloat(datas, 20),
            XActSpeed = ReadFloat(datas, 24),
            XSetPos = ReadFloat(datas, 28),
            XSetSpeed = ReadFloat(datas, 32),
            XActCurrent = ReadFloat(datas, 36),

            // Y 轴参数（40~59）
            YActPos = ReadFloat(datas, 40),
            YActSpeed = ReadFloat(datas, 44),
            YSetPos = ReadFloat(datas, 48),
            YSetSpeed = ReadFloat(datas, 52),
            YActCurrent = ReadFloat(datas, 56),

            // Z 轴参数（60~79）
            ZActPos = ReadFloat(datas, 60),
            ZActSpeed = ReadFloat(datas, 64),
            ZSetPos = ReadFloat(datas, 68),
            ZSetSpeed = ReadFloat(datas, 72),
            ZActCurrent = ReadFloat(datas, 76),

            // 温度参数（80~95）
            ActTemp1 = ReadFloat(datas, 80),
            ActTemp2 = ReadFloat(datas, 84),
            ActTemp3 = ReadFloat(datas, 88),
            ActTemp4 = ReadFloat(datas, 92),

            // 流量与压力（96~111）
            ActFlow1 = ReadFloat(datas, 96),
            ActFlow2 = ReadFloat(datas, 100),
            ActFlow3 = ReadFloat(datas, 104),
            ActPressure = ReadFloat(datas, 108),

            // 运行状态（112~113）
            HeatRun = ReadBool(datas, 112),
            CoolingRun = ReadBool(datas, 113),

            // 报警代码（114~117）
            ActAlarm1 = ReadUShort(datas, 114),
            ActAlarm2 = ReadUShort(datas, 116),

            // 长度参数（118~125）
            ActLenght1 = ReadFloat(datas, 118),
            ActLenght2 = ReadFloat(datas, 122),

            // 位置开关（126~137）
            Y_CDJ_QGQW = ReadBool(datas, 126),
            Y_CDJ_QGHW = ReadBool(datas, 127),
            Z_CDJ_QGQW = ReadBool(datas, 128),
            Z_CDJ_QGHW = ReadBool(datas, 129),
            JLG_1_SW = ReadBool(datas, 130),
            JLG_1_XW = ReadBool(datas, 131),
            JLG_2_SW = ReadBool(datas, 132),
            JLG_2_XW = ReadBool(datas, 133),
            JLG_3_SW = ReadBool(datas, 134),
            JLG_3_XW = ReadBool(datas, 135),
            JLG_4_SW = ReadBool(datas, 136),
            JLG_4_XW = ReadBool(datas, 137),

            // 设备状态（138~140）
            DY_ZT = ReadBool(datas, 138),
            PQ_ZT = ReadBool(datas, 139),
            ProcessOk = ReadBool(datas, 140),
        };
    }

    private static float ReadFloat(byte[] data, int offset)
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, 4));

        return BitConverter.Int32BitsToSingle(value);
    }

    private static bool ReadBool(byte[] data, int offset)
        => data[offset] != 0;

    private static ushort ReadUShort(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await StopAsync().ConfigureAwait(false);

        // _lifecycleLock 不显式释放：单例随进程回收，避免与仍在退出的调用方竞态。
    }
}
