using System.Globalization;
using Microsoft.Extensions.Logging;
using S7.Net;
using S7.Net.Types;

namespace RailWeldApp.Services;

public sealed partial class PlcService
{
    // 读取方法。ReadManyAsync 会把多个地址按 12 个一批发送，减少 PLC 往返次数。

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

    public Task<IReadOnlyDictionary<string, object?>> ReadManyAsync(IReadOnlyDictionary<string, string> points, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<string, object?>>(
                new Dictionary<string, object?>(StringComparer.Ordinal));
        }

        return ExecuteAsync<IReadOnlyDictionary<string, object?>>(
            async (plc, ct) =>
            {
                const int batchSize = 12;

                KeyValuePair<string, string>[] pointsArray = [.. points];

                var values = new Dictionary<string, object?>(pointsArray.Length, StringComparer.Ordinal);

                for (int offset = 0; offset < pointsArray.Length; offset += batchSize)
                {
                    int count = Math.Min(batchSize, pointsArray.Length - offset);

                    List<DataItem> items = new(count);

                    List<string> names = new(count);

                    for (int index = 0; index < count; index++)
                    {
                        KeyValuePair<string, string> point = pointsArray[offset + index];

                        names.Add(point.Key);
                        items.Add(DataItem.FromAddress(point.Value));
                    }

                    await plc.ReadMultipleVarsAsync(items, ct).ConfigureAwait(false);

                    for (int index = 0; index < items.Count; index++)
                    {
                        values[names[index]] = items[index].Value;
                    }
                }

                return values;
            },
            cancellationToken);
    }
}