namespace RailWeldApp.Options;

/// <summary>
/// 实时数据点配置。
/// 每个字段独立配置 S7 地址，支持不连续地址；地址格式为
/// DB4.DBD0（REAL）、DB4.DBW114（WORD）、DB4.DBX112.0（BOOL）。
/// </summary>
public sealed class RealtimeDataOptions
{
    public const string SectionName = "RealtimeData";

    public string Voltage { get; set; } = "DB4.DBD0";
    public string Current { get; set; } = "DB4.DBD4";
    public string Frequency { get; set; } = "DB4.DBD8";
    public string Power { get; set; } = "DB4.DBD12";
    public string Energy { get; set; } = "DB4.DBD16";

    public string XActPos { get; set; } = "DB4.DBD20";
    public string XActSpeed { get; set; } = "DB4.DBD24";
    public string XSetPos { get; set; } = "DB4.DBD28";
    public string XSetSpeed { get; set; } = "DB4.DBD32";
    public string XActCurrent { get; set; } = "DB4.DBD36";

    public string YActPos { get; set; } = "DB4.DBD40";
    public string YActSpeed { get; set; } = "DB4.DBD44";
    public string YSetPos { get; set; } = "DB4.DBD48";
    public string YSetSpeed { get; set; } = "DB4.DBD52";
    public string YActCurrent { get; set; } = "DB4.DBD56";

    public string ZActPos { get; set; } = "DB4.DBD60";
    public string ZActSpeed { get; set; } = "DB4.DBD64";
    public string ZSetPos { get; set; } = "DB4.DBD68";
    public string ZSetSpeed { get; set; } = "DB4.DBD72";
    public string ZActCurrent { get; set; } = "DB4.DBD76";

    public string ActTemp1 { get; set; } = "DB4.DBD80";
    public string ActTemp2 { get; set; } = "DB4.DBD84";
    public string ActTemp3 { get; set; } = "DB4.DBD88";
    public string ActTemp4 { get; set; } = "DB4.DBD92";

    public string ActFlow1 { get; set; } = "DB4.DBD96";
    public string ActFlow2 { get; set; } = "DB4.DBD100";
    public string ActFlow3 { get; set; } = "DB4.DBD104";
    public string ActPressure { get; set; } = "DB4.DBD108";

    public string HeatRun { get; set; } = "DB4.DBX112.0";
    public string CoolingRun { get; set; } = "DB4.DBX113.0";

    public string ActAlarm1 { get; set; } = "DB4.DBW114";
    public string ActAlarm2 { get; set; } = "DB4.DBW116";

    public string ActLenght1 { get; set; } = "DB4.DBD118";
    public string ActLenght2 { get; set; } = "DB4.DBD122";

    public string Y_CDJ_QGQW { get; set; } = "DB4.DBX126.0";
    public string Y_CDJ_QGHW { get; set; } = "DB4.DBX127.0";
    public string Z_CDJ_QGQW { get; set; } = "DB4.DBX128.0";
    public string Z_CDJ_QGHW { get; set; } = "DB4.DBX129.0";

    public string JLG_1_SW { get; set; } = "DB4.DBX130.0";
    public string JLG_1_XW { get; set; } = "DB4.DBX131.0";
    public string JLG_2_SW { get; set; } = "DB4.DBX132.0";
    public string JLG_2_XW { get; set; } = "DB4.DBX133.0";
    public string JLG_3_SW { get; set; } = "DB4.DBX134.0";
    public string JLG_3_XW { get; set; } = "DB4.DBX135.0";
    public string JLG_4_SW { get; set; } = "DB4.DBX136.0";
    public string JLG_4_XW { get; set; } = "DB4.DBX137.0";

    public string DY_ZT { get; set; } = "DB4.DBX138.0";
    public string PQ_ZT { get; set; } = "DB4.DBX139.0";
    public string ProcessOk { get; set; } = "DB4.DBX140.0";
}
