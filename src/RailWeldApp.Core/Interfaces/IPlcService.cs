namespace RailWeldApp.Interfaces;

public interface IPlcService : IAsyncDisposable
{
    #region Test

    //bool IsConnected { get; }
    //string? LastError { get; }

    //event EventHandler<bool>? ConnectionStateChanged;

    //Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    //Task DisconnectAsync(CancellationToken cancellationToken = default);

    //Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);

    //Task<byte[]> ReadBytesAsync(int dbNumber, int startByte, int count, CancellationToken cancellationToken = default);

    //Task<T> ReadAsync<T>(string address, CancellationToken cancellationToken = default);

    //Task WriteAsync(string address, object value, CancellationToken cancellationToken = default);

    #endregion Test

    bool IsConnected { get; }

    string? LastError { get; }

    event EventHandler<bool> ConnectionStateChanged;

    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default);

    Task<byte[]> ReadBytesAsync(int dbNumber, int startByte, int count, CancellationToken cancellationToken = default);

    Task<T> ReadAsync<T>(string address, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, object?>> ReadManyAsync(
        IReadOnlyDictionary<string, string> points,
        CancellationToken cancellationToken = default);

    Task WriteAsync(string address, object value, CancellationToken cancellationToken = default);
}
