using System.Globalization;
using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailWeldApp.Interfaces;
using RailWeldApp.Options;
using S7.Net;
using S7.Net.Types;

namespace RailWeldApp.Services;

/// <summary>
/// PLC 通信服务（部分类）。
/// 主文件只放字段、状态和公共辅助；连接、读取、写入、心跳分别在对应 partial 文件。
/// </summary>
public sealed partial class PlcService(IOptions<PlcConfigurationOptions> options, ILogger<PlcService> logger) : IPlcService, IDisposable
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

    private readonly PlcConfigurationOptions _options = options.Value;
    private readonly ILogger<PlcService> _logger = logger;

    // _ioLock 保证同一时刻只有一个 PLC 网络请求；_lifecycleLock 保证连接/断开不会并发。
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