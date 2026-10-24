namespace RailWeldApp.Interfaces;

public interface IPlcPollingBackgroundService
{
    bool IsRunning { get; }

    void Dispose();

    ValueTask DisposeAsync();

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync();
}
