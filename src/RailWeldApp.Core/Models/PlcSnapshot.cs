namespace RailWeldApp.Models
{
    /// <summary>
    ///  PLC数据快照
    /// </summary>
    public sealed class PlcSnapshot
    {
        // 本次成功采集的时间
        public DateTime Timestamp { get; init; }

        // 本次快照是否有效
        public bool IsValid { get; init; }

        public float Voltage { get; init; }
        public float Current { get; init; }
        public float Frequency { get; init; }
        public float Power { get; init; }
        public float Energy { get; init; }
        public float XActPos { get; init; }
        public float XActSpeed { get; init; }
        public float XSetPos { get; init; }
        public float XSetSpeed { get; init; }
        public float XActCurrent { get; init; }
        public float YActPos { get; init; }
        public float YActSpeed { get; init; }
        public float YSetPos { get; init; }
        public float YSetSpeed { get; init; }
        public float YActCurrent { get; init; }
        public float ZActPos { get; init; }
        public float ZActSpeed { get; init; }
        public float ZSetPos { get; init; }
        public float ZSetSpeed { get; init; }
        public float ZActCurrent { get; init; }
        public float ActTemp1 { get; init; }
        public float ActTemp2 { get; init; }
        public float ActTemp3 { get; init; }
        public float ActTemp4 { get; init; }
        public float ActFlow1 { get; init; }
        public float ActFlow2 { get; init; }
        public float ActFlow3 { get; init; }
        public float ActPressure { get; init; }
        public bool HeatRun { get; init; }
        public bool CoolingRun { get; init; }
        public ushort ActAlarm1 { get; init; }
        public ushort ActAlarm2 { get; init; }
        public float ActLenght1 { get; init; }
        public float ActLenght2 { get; init; }
        public bool Y_CDJ_QGQW { get; init; }
        public bool Y_CDJ_QGHW { get; init; }
        public bool Z_CDJ_QGQW { get; init; }
        public bool Z_CDJ_QGHW { get; init; }
        public bool JLG_1_SW { get; init; }
        public bool JLG_1_XW { get; init; }
        public bool JLG_2_SW { get; init; }
        public bool JLG_2_XW { get; init; }
        public bool JLG_3_SW { get; init; }
        public bool JLG_3_XW { get; init; }
        public bool JLG_4_SW { get; init; }
        public bool JLG_4_XW { get; init; }
        public bool DY_ZT { get; init; }
        public bool PQ_ZT { get; init; }
        public bool ProcessOk { get; init; }
    }
}
