namespace RailWeldApp.Services;

public sealed partial class PlcService
{
    // 写单个地址；地址类型由 S7.Net 根据 DBX/DBD/DBW 判断。

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
}