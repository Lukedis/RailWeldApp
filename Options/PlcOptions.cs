namespace RailWeldApp.Options
{
    public sealed class PlcOptions
    {
        public const string SectionName = "PLC";

        public string CpuType { get; set; } = "S7300";

        public string Ip { get; set; } = "127.0.0.1";

        public int Port { get; set; } = 102;

        public short Rack { get; set; }

        public short Slot { get; set; } = 2;

        public int ReadTimeout { get; set; } = 2000;

        public int WriteTimeout { get; set; } = 2000;

        public int HeartbeatInterval { get; set; } = 2000;

        public int HeartbeatFailureThreshold { get; set; } = 3;

        public int HeartbeatDbNumber { get; set; } = 3;

        public int HeartbeatStartByte { get; set; }

        public int HeartbeatByteCount { get; set; } = 1;
    }
}
