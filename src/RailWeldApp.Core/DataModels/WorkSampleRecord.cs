namespace RailWeldApp.DataModels;

/// <summary>
/// 作业过程中的采样点。数据量大，单独一张表，按 WorkRecord 级联删除。
/// </summary>
public sealed class WorkSampleRecord
{
    public long Id { get; set; }

    public string WorkRecordId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public int ElapsedMs { get; set; }

    public double Temperature1 { get; set; }

    public double Temperature2 { get; set; }

    public double Temperature3 { get; set; }

    public double Temperature4 { get; set; }

    public double Pressure { get; set; }

    public double Flow1 { get; set; }

    public double Flow2 { get; set; }

    public double Flow3 { get; set; }

    public double Voltage { get; set; }

    public double Current { get; set; }

    public double Power { get; set; }

    public double Energy { get; set; }

    public WorkRecord? WorkRecord { get; set; }
}
