namespace RailWeldApp.Options
{
    /// <summary>
    ///  PLC配置选项
    /// </summary>
    public sealed class PlcConfigurationOptions
    {
        public const string SectionName = "PLC";

        public string CpuType { get; set; } = string.Empty;

        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        public short Rack { get; set; }

        public short Slot { get; set; }

        public int ReadTimeout { get; set; }

        public int WriteTimeout { get; set; }

        public int HeartbeatInterval { get; set; }

        public int HeartbeatFailureThreshold { get; set; }

        public int HeartbeatDbNumber { get; set; }

        public int HeartbeatStartByte { get; set; }

        public int HeartbeatByteCount { get; set; }
    }
}