using System.Globalization;
using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailWeldApp.Interfaces;
using RailWeldApp.Options;
using S7.Net;

namespace RailWeldApp.Services;

public sealed class PlcService(IOptions<PlcOptions> options, ILogger<PlcService> logger) : IPlcService, IDisposable
{
    private static readonly Dictionary<string, CpuType> _cpuTypeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S7200"] = CpuType.S7200,
        ["S7-200"] = CpuType.S7200,
        ["S7200SMART"] = CpuType.S7200Smart,
        ["S7-200SMART"] = CpuType.S7200Smart,
        ["S7300"] = CpuType.S7300,
        ["S7-300"] = CpuType.S7300,
        ["S7400"] = CpuType.S7400,
        ["S7-400"] = CpuType.S7400,
        ["S71200"] = CpuType.S71200,
        ["S7-1200"] = CpuType.S71200,
        ["S71500"] = CpuType.S71500,
        ["S7-1500"] = CpuType.S71500,
        ["LOGO0BA8"] = CpuType.Logo0BA8,
    };

    private readonly PlcOptions _options = options.Value;
    private readonly ILogger<PlcService> _logger = logger;

    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private Plc? _plc;
    private CancellationTokenSource? _heartbeatCts;
    private Task? _heartbeatTask;
    private int _heartbeatFailures;
    private int _isConnected;
    private int _disposed;
    private string? _lastError;

    // 断线时 Connect/Read 会按 2~3 秒重试，同一错误没必要每次都写日志（离线一天会写几万行）。
    private long _lastFailureLogTimestamp;

    private string? _lastFailureLogMessage;

    public bool IsConnected => Volatile.Read(ref _isConnected) == 1;
    public string? LastError => Volatile.Read(ref _lastError);

    public event EventHandler<bool>? ConnectionStateChanged;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ThrowIfDisposed();

            if (IsConnected && _plc is not null)
                return true;

            await StopHeartbeatAsync().ConfigureAwait(false);

            await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await ClosePlcCoreAsync().ConfigureAwait(false);

                CpuType cpuType = ResolveCpuType(_options.CpuType);

                var candidate = new Plc(cpuType, _options.Ip, _options.Port, _options.Rack, _options.Slot)
                {
                    // S7NetPlus 的默认超时是 Timeout.Infinite，必须显式设置。
                    ReadTimeout = _options.ReadTimeout,
                    WriteTimeout = _options.WriteTimeout,
                };

                var handedOver = false;

                try
                {
                    await candidate.OpenAsync(cancellationToken).ConfigureAwait(false);

                    // 建链后先读一次心跳地址：确认 Rack/Slot/DB 真的可用，避免"连上了但什么都读不到"。
                    var probe = await candidate
                        .ReadBytesAsync(
                            DataType.DataBlock,
                            _options.HeartbeatDbNumber,
                            _options.HeartbeatStartByte,
                            _options.HeartbeatByteCount,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (probe.Length != _options.HeartbeatByteCount)
                    {
                        throw new InvalidOperationException(
                            $"心跳地址校验返回 {probe.Length} 字节，期望 {_options.HeartbeatByteCount} 字节。");
                    }

                    _plc = candidate;

                    handedOver = true;

                    _heartbeatFailures = 0;

                    SetLastError(null);

                    SetConnectionState(true);

                    _logger.LogInformation(
                        "PLC 连接成功：{Ip}:{Port}，CPU {CpuType}，Rack/Slot {Rack}/{Slot}",
                        _options.Ip, _options.Port, cpuType, _options.Rack, _options.Slot);
                }
                finally
                {
                    // 未成功交接的连接必须就地释放，避免半开连接泄漏。
                    if (!handedOver)
                        await Task.Run(() => ClosePlc(candidate), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _ioLock.Release();
            }

            StartHeartbeat();

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetLastError(Describe(ex));
            SetConnectionState(false);
            LogFailure(ex, "PLC 连接失败：{Ip}:{Port}", _options.Ip, _options.Port);
            return false;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposed) == 1)
            return;

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _disposed) == 1)
                return;

            await StopHeartbeatAsync().ConfigureAwait(false);

            await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var wasConnected = IsConnected;

                await ClosePlcCoreAsync().ConfigureAwait(false);

                if (wasConnected)
                {
                    SetLastError(null);
                }

                SetConnectionState(false);

                if (wasConnected)
                    _logger.LogInformation("PLC 已主动断开连接。");
            }
            finally
            {
                _ioLock.Release();
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!IsConnected || _plc is null)
                return false;

            try
            {
                var data = await _plc
                    .ReadBytesAsync(
                        DataType.DataBlock,
                        _options.HeartbeatDbNumber,
                        _options.HeartbeatStartByte,
                        _options.HeartbeatByteCount,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (data.Length != _options.HeartbeatByteCount)
                {
                    throw new InvalidOperationException(
                        $"心跳读取返回 {data.Length} 字节，期望 {_options.HeartbeatByteCount} 字节。");
                }

                _heartbeatFailures = 0;

                SetLastError(null);

                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // 服务正在停止，不算通信故障
            }
            catch (Exception ex)
            {
                _heartbeatFailures++;

                SetLastError(Describe(ex));

                // 同一错误只在首次和每 30 次（约 1 分钟）记一条，避免离线时刷日志。
                if (_heartbeatFailures == 1 || _heartbeatFailures % 30 == 0)
                {
                    _logger.LogWarning(ex, "PLC 心跳失败 {Failures}/{Threshold} 次。", _heartbeatFailures, _options.HeartbeatFailureThreshold);
                }

                if (_heartbeatFailures >= _options.HeartbeatFailureThreshold)
                {
                    // 达到阈值：关闭连接并置为断开（调用方必须持有 _ioLock）。
                    await ClosePlcCoreAsync().ConfigureAwait(false);

                    SetConnectionState(false);

                    _logger.LogError("PLC 心跳连续失败 {Count} 次，已判定断线。", _heartbeatFailures);
                }

                return false;
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public Task<byte[]> ReadBytesAsync(int dbNumber, int startByte, int count, CancellationToken cancellationToken = default)
    {
        if (dbNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(dbNumber), dbNumber, "DB 号必须大于 0。");

        if (startByte < 0)
            throw new ArgumentOutOfRangeException(nameof(startByte), startByte, "起始字节不能为负数。");

        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "读取字节数必须大于 0。");

        return ExecuteAsync(
            (plc, ct) => plc.ReadBytesAsync(DataType.DataBlock, dbNumber, startByte, count, ct),
            cancellationToken);
    }

    public Task<T> ReadAsync<T>(string address, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        return ExecuteAsync<T>(
            async (plc, ct) =>
            {
                var raw = await plc.ReadAsync(address, ct).ConfigureAwait(false) ?? throw new InvalidOperationException($"地址 {address} 读取结果为空。");

                // 先按原类型直取；类型不符时再尝试转换（例如把 int 读成 long）。
                if (raw is T typed)
                    return typed;

                try
                {
                    return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
                }
                catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
                {
                    throw new InvalidOperationException(
                        $"地址 {address} 返回 {raw.GetType().Name}，无法转换为 {typeof(T).Name}。", ex);
                }
            },
            cancellationToken);
    }

    public Task WriteAsync(string address, object value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentNullException.ThrowIfNull(value);

        return ExecuteAsync(
            async (plc, ct) =>
            {
                await plc.WriteAsync(address, value, ct).ConfigureAwait(false);
                return true;
            },
            cancellationToken);
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await _lifecycleLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await StopHeartbeatAsync().ConfigureAwait(false);

            await _ioLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await ClosePlcCoreAsync().ConfigureAwait(false);
            }
            finally
            {
                _ioLock.Release();
            }

            SetConnectionState(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }

        // 两个信号量不显式释放：本服务是单例，随进程回收即可；
        // 若在此释放，可能与仍在退出的调用方（正在 WaitAsync）竞态并抛 ObjectDisposedException。
    }

    private async Task<T> ExecuteAsync<T>(Func<Plc, CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            Plc plc = _plc ?? throw new InvalidOperationException("PLC 尚未连接，请先调用 ConnectAsync。");

            if (!IsConnected)
                throw new InvalidOperationException("PLC 当前处于断开状态，请先重新连接。");

            try
            {
                return await action(plc, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw; // 调用方主动取消，不写成通信错误
            }
            catch (Exception ex)
            {
                SetLastError(Describe(ex));
                LogFailure(ex, "PLC 读写失败。");
                throw;
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private void StartHeartbeat()
    {
        var cts = new CancellationTokenSource();
        _heartbeatCts = cts;
        _heartbeatTask = HeartbeatLoopAsync(cts.Token);
    }

    private async Task StopHeartbeatAsync()
    {
        CancellationTokenSource? cts = _heartbeatCts;

        Task? task = _heartbeatTask;

        _heartbeatCts = null;
        _heartbeatTask = null;

        if (cts is null)
            return;

        await cts.CancelAsync().ConfigureAwait(false);

        try
        {
            if (task is not null)
                await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(_options.HeartbeatInterval, token).ConfigureAwait(false);

                var ok = await CheckConnectionAsync(token).ConfigureAwait(false);

                // 单次失败不停心跳；只有达到阈值、连接已被关闭时才退出。
                if (!ok && !IsConnected)
                    break;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 主动断开或服务释放
        }
        catch (ObjectDisposedException)
        {
            // 服务已释放，正常退出
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PLC 心跳任务异常退出。");
        }
    }

    /// <summary>
    ///  异步关闭PLC
    /// </summary>
    private async Task ClosePlcCoreAsync()
    {
        Plc? plc = _plc;

        _plc = null;

        if (plc is null)
            return;

        await Task.Run(() => ClosePlc(plc)).ConfigureAwait(false);
    }

    /// <summary>
    ///  同步关闭PLC
    /// </summary>
    private static void ClosePlc(Plc plc)
    {
        try
        {
            plc.Close();
        }
        catch (Exception)
        {
            // 关闭阶段的异常无需上抛：对象即将丢弃
        }
        finally
        {
            (plc as IDisposable)?.Dispose();
        }
    }

    private void SetConnectionState(bool connected)
    {
        var value = connected ? 1 : 0;

        if (Interlocked.Exchange(ref _isConnected, value) == value)
            return;

        _logger.LogInformation("PLC 连接状态：{State}", connected ? "已连接" : "未连接");

        try
        {
            ConnectionStateChanged?.Invoke(this, connected);
        }
        catch (Exception ex)
        {
            // 事件处理器的异常不能影响通信服务本身
            _logger.LogError(ex, "PLC 连接状态事件的处理程序抛出异常。");
        }
    }

    private void SetLastError(string? message) => Volatile.Write(ref _lastError, message);

    /// <summary>
    /// 同一错误 1 分钟内只写一条 Error 日志；错误内容变化或超过 1 分钟才再记。
    /// PLC 离线时轮询会每 2~3 秒重试，不做限流的话一天能写出几万行日志，既占磁盘也拖慢采集线程。
    /// </summary>
    private void LogFailure(Exception ex, string message, params object?[] args)
    {
        var now = Environment.TickCount64;

        if (ex.Message == _lastFailureLogMessage && now - _lastFailureLogTimestamp < 60_000)
            return;

        _lastFailureLogMessage = ex.Message;
        _lastFailureLogTimestamp = now;

        _logger.LogError(ex, message, args);
    }

    /// <summary>
    ///  解析CPU
    /// </summary>
    private static CpuType ResolveCpuType(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (_cpuTypeAliases.TryGetValue(value.Trim(), out CpuType alias))
                return alias;

            if (Enum.TryParse(value, true, out CpuType parsed))
                return parsed;
        }

        throw new InvalidOperationException(
            $"无法识别的 PLC 型号 \"{value}\"。可用值：S71200 / S71500 / S7300 / S7400 / S7200 / S7200Smart / Logo0BA8。");
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

    private static string Describe(Exception ex) => ex switch
    {
        PlcException { ErrorCode: ErrorCode.IPAddressNotAvailable } pe =>
            $"PLC 的 IP 不可达：{pe.Message}",
        PlcException { ErrorCode: ErrorCode.ConnectionError } pe =>
            $"PLC 连接被拒绝或中断（请检查 Rack/Slot、PUT/GET 与网线）：{pe.Message}",
        PlcException { ErrorCode: ErrorCode.WrongCPU_Type } pe =>
            $"PLC 型号与配置不符：{pe.Message}",
        PlcException { ErrorCode: ErrorCode.WrongVarFormat } pe =>
            $"PLC 地址格式错误：{pe.Message}",
        PlcException { ErrorCode: ErrorCode.WrongNumberReceivedBytes } pe =>
            $"PLC 返回字节数不符：{pe.Message}",
        PlcException { ErrorCode: ErrorCode.SendData } pe =>
            $"PLC 发送数据失败（可能超时或已断线）：{pe.Message}",
        PlcException { ErrorCode: ErrorCode.ReadData } pe =>
            $"PLC 读取数据失败（可能超时或已断线）：{pe.Message}",
        PlcException { ErrorCode: ErrorCode.WriteData } pe =>
            $"PLC 写入数据失败（可能超时或已断线）：{pe.Message}",
        PlcException pe => pe.Message,
        IOException => "PLC 通信中断（读写超时或连接被重置）。",
        TimeoutException => "PLC 通信超时。",
        SocketException se => $"PLC 网络异常：{se.Message}",
        _ => ex.Message,
    };
}
