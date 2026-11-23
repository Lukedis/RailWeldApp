using System.Globalization;
using Microsoft.Extensions.Logging;
using S7.Net;

namespace RailWeldApp.Services;

public sealed partial class PlcService
{
    // 连接生命周期：连接、断开、心跳检查和关闭底层 S7.Net 连接。

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
}