using RailWeldApp.Models;

namespace RailWeldApp.Interfaces;

public interface IModbusRtuService : IAsyncDisposable
{
    bool IsConnected { get; }
    string? LastError { get; }
    bool LastOperationSucceeded { get; }

    event EventHandler<bool>? ConnectionStatusChanged;

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    Task WriteSingleCoilAsync(byte slaveAddress, ushort address, bool value, CancellationToken cancellationToken = default);

    Task WriteSingleRegisterAsync(byte slaveAddress, ushort address, ushort value, CancellationToken cancellationToken = default);

    Task WriteMultipleCoilsAsync(byte slaveAddress, ushort startAddress, bool[] values, CancellationToken cancellationToken = default);

    Task WriteMultipleRegistersAsync(byte slaveAddress, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default);
}
