namespace RailWeldApp.DataModels;

/// <summary>
/// PLC / Modbus 通信异常。系统设置页面的连接错误最终应写入这张表。
/// </summary>
public sealed class CommunicationErrorRecord
{
    public long Id { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.Now;

    public string CommunicationType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public bool IsRecovered { get; set; }

    public DateTime? RecoveredAt { get; set; }
}
