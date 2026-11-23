namespace RailWeldApp.Options
{
    /// <summary>
    ///  手动控制点配置选项。
    ///  地址来自 appsettings.json 的 ManualControlAddress，缺省值保持旧系统点表。
    /// </summary>
    public sealed class ManualControlPointConfigurationOptions
    {
        public const string SectionName = "ManualControlAddress";

        public string PowerOn { get; set; } = "DB7.DBX66.0";
        public string PowerOff { get; set; } = "DB7.DBX66.1";
        public string AirBlowStart { get; set; } = "DB7.DBX72.0";
        public string AirBlowStop { get; set; } = "DB7.DBX72.1";
        public string YAxisLeft { get; set; } = "DB7.DBX74.0";
        public string YAxisRight { get; set; } = "DB7.DBX74.1";
        public string ZAxisUp { get; set; } = "DB7.DBX74.2";
        public string ZAxisDown { get; set; } = "DB7.DBX74.3";
        public string Jack1Up { get; set; } = "DB7.DBX76.0";
        public string Jack1Down { get; set; } = "DB7.DBX76.1";
        public string Jack2Up { get; set; } = "DB7.DBX76.2";
        public string Jack2Down { get; set; } = "DB7.DBX76.3";
        public string Jack3Up { get; set; } = "DB7.DBX76.4";
        public string Jack3Down { get; set; } = "DB7.DBX76.5";
        public string Jack4Up { get; set; } = "DB7.DBX76.6";
        public string Jack4Down { get; set; } = "DB7.DBX76.7";
        public string PowerSetpoint { get; set; } = "DB7.DBD80";
        public string TimeSetpoint { get; set; } = "DB7.DBD68";
    }
}
