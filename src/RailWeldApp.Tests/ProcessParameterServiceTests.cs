using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Options;
using RailWeldApp.Services;

namespace RailWeldApp.Tests;

[TestClass]
public sealed class ProcessParameterServiceTests
{
    [TestMethod]
    public async Task LoadAsync_WritesMappedValuesToConfiguredAddresses()
    {
        var options = new ProcessParametersConfigurationOptions
        {
            ActWeldingHome = "DB2.DBD0",
            ActWeldingHeight = "DB2.DBD4",
            YAutoCompensationEnabled = "DB2.DBX186.0",
            ActHaetTempSet1 = "DB2.DBD320",
            ActPressureMax = "DB2.DBD226",
        };

        var plc = new FakePlcService();
        var service = new ProcessParameterService(
            plc,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<ProcessParameterService>.Instance);

        var recipe = new ParameterSettingRecord
        {
            YWeldSeamZero = "12.5",
            WeldSeamHeight = "25",
            IsCompensationEnabled = true,
            StartTemp = "500",
            AirPressureMax = "0.6",
        };

        await service.LoadAsync(recipe);

        Assert.AreEqual(12.5f, plc.Writes["DB2.DBD0"]);
        Assert.AreEqual(25f, plc.Writes["DB2.DBD4"]);
        Assert.AreEqual(true, plc.Writes["DB2.DBX186.0"]);
        Assert.AreEqual(500f, plc.Writes["DB2.DBD320"]);
        Assert.AreEqual(0.6f, plc.Writes["DB2.DBD226"]);
    }

    [TestMethod]
    public async Task LoadAsync_WhenDisconnected_Throws()
    {
        var plc = new FakePlcService { IsConnected = false };
        var service = new ProcessParameterService(
            plc,
            Microsoft.Extensions.Options.Options.Create(new ProcessParametersConfigurationOptions()),
            NullLogger<ProcessParameterService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadAsync(new ParameterSettingRecord()));
    }

    private sealed class FakePlcService : IPlcService
    {
        public Dictionary<string, object?> Writes { get; } = [];

        public bool IsConnected { get; set; } = true;

        public string? LastError => null;

        public event EventHandler<bool>? ConnectionStateChanged;

        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult(IsConnected);

        public Task<byte[]> ReadBytesAsync(int dbNumber, int startByte, int count, CancellationToken cancellationToken = default)
            => Task.FromResult(new byte[count]);

        public Task<T> ReadAsync<T>(string address, CancellationToken cancellationToken = default)
            => Task.FromResult(default(T)!);

        public Task<IReadOnlyDictionary<string, object?>> ReadManyAsync(
            IReadOnlyDictionary<string, string> points,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>());

        public Task WriteAsync(string address, object value, CancellationToken cancellationToken = default)
        {
            Writes[address] = value;
            ConnectionStateChanged?.Invoke(this, IsConnected);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
