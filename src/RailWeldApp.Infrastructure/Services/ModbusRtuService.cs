using System.IO;
using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NModbus;
using NModbus.Serial;
using RailWeldApp.Interfaces;
using RailWeldApp.Options;

namespace RailWeldApp.Services;

public sealed class ModbusRtuService(ILogger<ModbusRtuService> logger, IOptionsMonitor<ModbusRtuConfigurationOptions> options)
    : IModbusRtuService
{
    private const ushort _maxReadCoils = 2000;       // 最大读取线圈
    private const ushort _maxReadRegisters = 125;    // 最大读取寄存器
    private const int _maxWriteCoils = 1968;         // 最大写线圈（协议上限，NModbus 参数为 ushort）
    private const int _maxWriteRegisters = 123;      // 最大写寄存器（协议上限，NModbus 参数为 ushort）

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private SerialPort? _serialPort;                // 本机串口
    private IModbusSerialMaster? _master;           // 主站

    private int _disposedFlag;                      // 0=未释放，1=已释放
    private volatile bool _isConnected;
    private volatile string? _lastError;
    private volatile bool _lastOperationSucceeded;

    public bool IsConnected => _isConnected;

    public string? LastError => _lastError;

    public bool LastOperationSucceeded => _lastOperationSucceeded;

    public event EventHandler<bool>? ConnectionStatusChanged;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        SerialPort? temporaryPort = null;            // 临时串口

        IModbusSerialMaster? temporaryMaster = null; // 临时主站

        var raiseConnected = false;                 // 需要触发“已连接”事件

        var raiseDisconnected = false;              // 需要触发“已断开”事件

        ModbusRtuConfigurationOptions? currentOptions = null;

        try
        {
            ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);

            if (IsConnected)
                return;

            // 若之前残留连接，先清理，并在释放信号量后补发断开事件。
            raiseDisconnected = DisconnectCore();

            _lastError = null;

            _lastOperationSucceeded = false;

            currentOptions = options.CurrentValue;

            ValidateOptions(currentOptions);

            if (currentOptions.SlaveAddress is < 1 or > 247)
            {
                throw new ArgumentOutOfRangeException(nameof(currentOptions.SlaveAddress), "Modbus RTU 从站地址必须在 1～247 之间。");
            }

            cancellationToken.ThrowIfCancellationRequested();

            temporaryPort = new SerialPort(
                currentOptions.PortName,
                currentOptions.BaudRate,
                currentOptions.Parity,
                currentOptions.DataBits,
                currentOptions.StopBits)
            {
                ReadTimeout = currentOptions.ReadTimeout,
                WriteTimeout = currentOptions.WriteTimeout
            };

            // 1. 打开本机 COM 口。
            await Task.Run(temporaryPort.Open, CancellationToken.None).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // 2. 创建 Master，但暂不标记为已连接。
            temporaryMaster = new ModbusFactory().CreateRtuMaster(temporaryPort);

            IModbusSerialMaster master = temporaryMaster;

            var slaveAddress = currentOptions.SlaveAddress;

            var registerAddress = currentOptions.VerificationRegisterAddress;

            // 3. 通过读取一个保持寄存器验证从站通信是否正常。
            var response = await Task.Run(
                () => master.ReadHoldingRegisters(slaveAddress, registerAddress, 1),
                CancellationToken.None).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (response is null || response.Length != 1)
                throw new IOException("从站返回的寄存器数据无效。");

            // 4. 验证成功，正式保存通信资源。
            _serialPort = temporaryPort;

            _master = temporaryMaster;

            temporaryPort = null;

            temporaryMaster = null;

            _isConnected = true;

            _lastOperationSucceeded = true;

            _lastError = null;

            raiseConnected = true;

            //logger.LogInformation(
            //    "Modbus RTU 连接验证成功：串口 {PortName}，从站 {SlaveAddress}，验证寄存器 {RegisterAddress}，返回值 {RegisterValue}",
            //    currentOptions.PortName, slaveAddress, registerAddress, response[0]);
        }
        catch (TimeoutException ex)
        {
            _lastError = $"从站连接验证超时：{ex.Message}";

            _lastOperationSucceeded = false;

            _isConnected = false;

            logger.LogWarning(ex, "Modbus RTU 连接验证超时：从站 {SlaveAddress} 未正常响应", currentOptions?.SlaveAddress ?? options.CurrentValue.SlaveAddress);

            throw; // 调用方需要感知失败
        }
        catch (OperationCanceledException)
        {
            _lastError = "连接操作已取消。";

            _lastOperationSucceeded = false;

            throw;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;

            _lastOperationSucceeded = false;

            _isConnected = false;

            //logger.LogError(ex, "Modbus RTU 连接或从站验证失败");

            throw;
        }
        finally
        {
            try
            {
                temporaryMaster?.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "释放临时 Modbus Master 时发生异常");
            }

            try
            {
                temporaryPort?.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "释放临时串口时发生异常");
            }

            _semaphore.Release();

            // 事件在释放信号量之后触发，避免订阅者回调本类方法时死锁。
            if (raiseDisconnected)
                RaiseConnectionStatusChanged(false);

            if (raiseConnected)
                RaiseConnectionStatusChanged(true);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        var raiseDisconnected = false;

        try
        {
            ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);

            raiseDisconnected = DisconnectCore();

            _lastError = null;

            _lastOperationSucceeded = false;
        }
        finally
        {
            _semaphore.Release();

            if (raiseDisconnected)
                RaiseConnectionStatusChanged(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        // 保证只执行一次；多次调用直接返回。
        if (Interlocked.Exchange(ref _disposedFlag, 1) == 1)
            return;

        bool raiseDisconnected;

        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            raiseDisconnected = DisconnectCore();
        }
        finally
        {
            _semaphore.Release();
        }

        // 事件在释放信号量之后触发，避免订阅者回调本类方法时死锁。
        if (raiseDisconnected)
            RaiseConnectionStatusChanged(false);

        // 不主动 Dispose 信号量：并发等待者可能仍在使用，
        // 交由 GC 回收即可（SemaphoreSlim 仅在使用 AvailableWaitHandle 时才需显式释放）。
    }

    public Task<bool[]> ReadCoilsAsync(
        byte slaveAddress,
        ushort startAddress,
        ushort count,
        CancellationToken cancellationToken = default)
    {
        ValidateReadCount(count, _maxReadCoils);

        return ExecuteAsync(master => master.ReadCoils(slaveAddress, startAddress, count), cancellationToken);
    }

    public Task<ushort[]> ReadHoldingRegistersAsync(
        byte slaveAddress,
        ushort startAddress,
        ushort count,
        CancellationToken cancellationToken = default)
    {
        ValidateReadCount(count, _maxReadRegisters);

        return ExecuteAsync(master => master.ReadHoldingRegisters(slaveAddress, startAddress, count), cancellationToken);
    }

    public Task<ushort[]> ReadInputRegistersAsync(
        byte slaveAddress,
        ushort startAddress,
        ushort count,
        CancellationToken cancellationToken = default)
    {
        ValidateReadCount(count, _maxReadRegisters);

        return ExecuteAsync(master => master.ReadInputRegisters(slaveAddress, startAddress, count), cancellationToken);
    }

    public Task<bool[]> ReadInputsAsync(
        byte slaveAddress,
        ushort startAddress,
        ushort count,
        CancellationToken cancellationToken = default)
    {
        ValidateReadCount(count, _maxReadCoils);

        return ExecuteAsync(master => master.ReadInputs(slaveAddress, startAddress, count), cancellationToken);
    }

    public Task WriteMultipleCoilsAsync(
        byte slaveAddress,
        ushort startAddress,
        bool[] values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length is < 1 or > _maxWriteCoils)
        {
            throw new ArgumentOutOfRangeException(
                nameof(values), $"线圈写入数量必须为 1～{_maxWriteCoils}。");
        }

        var data = values.ToArray();

        return ExecuteAsync(master => master.WriteMultipleCoils(slaveAddress, startAddress, data), cancellationToken);
    }

    public Task WriteMultipleRegistersAsync(
        byte slaveAddress,
        ushort startAddress,
        ushort[] values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length is < 1 or > _maxWriteRegisters)
        {
            throw new ArgumentOutOfRangeException(
                nameof(values), $"寄存器写入数量必须为 1～{_maxWriteRegisters}。");
        }

        var data = values.ToArray();

        return ExecuteAsync(master => master.WriteMultipleRegisters(slaveAddress, startAddress, data), cancellationToken);
    }

    public Task WriteSingleCoilAsync(
        byte slaveAddress,
        ushort address,
        bool value,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(master => master.WriteSingleCoil(slaveAddress, address, value), cancellationToken);

    public Task WriteSingleRegisterAsync(
        byte slaveAddress,
        ushort address,
        ushort value,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(master => master.WriteSingleRegister(slaveAddress, address, value), cancellationToken);

    private bool DisconnectCore()
    {
        var wasConnected = _isConnected;

        _isConnected = false;

        // 释放 Modbus 主站
        try
        {
            _master?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "释放 Modbus Master 时发生异常");
        }
        finally
        {
            _master = null;
        }

        // 释放串口
        try
        {
            _serialPort?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "释放串口时发生异常");
        }
        finally
        {
            _serialPort = null;
        }

        if (wasConnected)
            logger.LogInformation("Modbus RTU 已断开");

        return wasConnected;
    }

    private static void ValidateOptions(ModbusRtuConfigurationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PortName))
            throw new ArgumentException("串口名称不能为空。");

        if (options.BaudRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.BaudRate), "波特率必须大于 0。");

        if (options.DataBits is < 5 or > 8)
            throw new ArgumentOutOfRangeException(nameof(options.DataBits), "数据位必须在 5～8 之间。");

        if (!System.Enum.IsDefined(options.Parity))
            throw new ArgumentOutOfRangeException(nameof(options.Parity), "校验位配置无效。");

        if (!System.Enum.IsDefined(options.StopBits) || options.StopBits == StopBits.None)
            throw new ArgumentOutOfRangeException(nameof(options.StopBits), "停止位配置无效。");

        if (options.ReadTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.ReadTimeout), "读取超时必须大于 0。");

        if (options.WriteTimeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.WriteTimeout), "写入超时必须大于 0。");
    }

    private void RaiseConnectionStatusChanged(bool connected)
    {
        EventHandler<bool>? handlers = ConnectionStatusChanged;

        if (handlers is null)
            return;

        // 逐个调用订阅者，避免单个订阅者的异常影响其他订阅者。
        foreach (EventHandler<bool> handler in handlers.GetInvocationList())
        {
            try
            {
                handler.Invoke(this, connected);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "处理连接状态变化事件时发生异常");
            }
        }
    }

    private static bool IsTransportFailure(Exception ex)
    {
        if (ex is TimeoutException or IOException or UnauthorizedAccessException or ObjectDisposedException)
            return true;

        return ex.InnerException is not null && IsTransportFailure(ex.InnerException);
    }

    private async Task<T> ExecuteAsync<T>(Func<IModbusSerialMaster, T> operation, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        var raiseDisconnected = false;

        try
        {
            ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);

            _lastOperationSucceeded = false;

            IModbusSerialMaster? master = _master;

            if (!IsConnected || master is null)
            {
                _lastError = "Modbus RTU 尚未连接，请先连接设备。";

                logger.LogWarning("{Error}", _lastError);

                // 明确失败，避免调用方误用 default! 造成 NullReferenceException。
                throw new InvalidOperationException(_lastError);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // NModbus 使用同步通信，在线程池中执行以避免阻塞 UI 线程。
            T? result = await Task.Run(() => operation(master), CancellationToken.None).ConfigureAwait(false);

            _lastError = null;

            _lastOperationSucceeded = true;

            return result;
        }
        catch (TimeoutException ex)
        {
            _lastError = $"Modbus RTU 通信超时：{ex.Message}";

            _lastOperationSucceeded = false;

            logger.LogWarning(ex, "Modbus RTU 通信超时，准备断开连接");

            raiseDisconnected = DisconnectCore();

            // 通信失败需让调用方感知；已释放连接资源，需重新连接后再试。
            throw new TimeoutException(_lastError, ex);
        }
        catch (OperationCanceledException)
        {
            _lastError = "通信操作已取消。";

            _lastOperationSucceeded = false;

            throw;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;

            _lastOperationSucceeded = false;

            logger.LogError(ex, "Modbus RTU 通信失败");

            if (IsTransportFailure(ex))
                raiseDisconnected = DisconnectCore();

            throw;
        }
        finally
        {
            _semaphore.Release();

            if (raiseDisconnected)
                RaiseConnectionStatusChanged(false);
        }
    }

    private Task ExecuteAsync(Action<IModbusSerialMaster> operation, CancellationToken cancellationToken)
        => ExecuteAsync(master => { operation(master); return true; }, cancellationToken);

    private static void ValidateReadCount(ushort count, ushort maxCount)
    {
        if (count == 0 || count > maxCount)
            throw new ArgumentOutOfRangeException(nameof(count), $"读取数量必须为 1～{maxCount}。");
    }
}