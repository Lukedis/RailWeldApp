using RailWeldApp.Enums;

namespace RailWeldApp.DataModels;

/// <summary>
/// 报警记录。开始报警时插入，恢复时更新 RecoveredAt，确认时写入确认人和时间。
/// </summary>
public sealed class AlarmRecord
{
    public long Id { get; set; }

    public string? WorkRecordId { get; set; }

    public string Code { get; set; } = string.Empty;

    public AlarmLevel Level { get; set; } = AlarmLevel.Info;

    public string Message { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.Now;

    public DateTime? RecoveredAt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsAcknowledged { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public string? AcknowledgedByAccount { get; set; }

    public string? AcknowledgedByUserId { get; set; }

    public WorkRecord? WorkRecord { get; set; }
}
