using System.Globalization;
using Microsoft.Extensions.Logging;
using S7.Net;

namespace RailWeldApp.Services;

public sealed partial class PlcService
{
    // 心跳循环：定期读取心跳字节，连续失败达到阈值就断开连接。

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
}