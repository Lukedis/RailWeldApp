using System.IO.Ports;

namespace RailWeldApp.Options;

public sealed class ModbusRtuOptions
{
    public string PortName { get; set; } = string.Empty;
    public int BaudRate { get; set; }
    public int DataBits { get; set; }
    public Parity Parity { get; set; }
    public StopBits StopBits { get; set; }
    public int ReadTimeout { get; set; }
    public int WriteTimeout { get; set; }
    public byte SlaveAddress { get; set; }
    public ushort VerificationRegisterAddress { get; set; }
}
