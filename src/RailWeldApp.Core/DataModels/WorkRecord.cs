using RailWeldApp.Enums;

namespace RailWeldApp.DataModels;

/// <summary>
/// 一次完整的焊接作业记录。按账户区分，不存焊缝编号。
/// </summary>
public sealed class WorkRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string AccountId { get; set; } = string.Empty;

    /// <summary>账户名快照，账户改名后历史记录仍保持原样。</summary>
    public string AccountName { get; set; } = string.Empty;

    public string? RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public string JobNumber { get; set; } = string.Empty;

    public string OperatorName { get; set; } = string.Empty;

    public string ShiftName { get; set; } = string.Empty;

    public string EquipmentName { get; set; } = string.Empty;

    public string ReportNo { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.Now;

    public DateTime? FinishedAt { get; set; }

    public WorkResult Result { get; set; } = WorkResult.Unknown;

    public string ResultDetail { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public UserRecord? User { get; set; }

    public ParameterSettingRecord? Recipe { get; set; }

    public ICollection<WorkSampleRecord> Samples { get; set; } = [];

    public ICollection<WorkMetricRecord> Metrics { get; set; } = [];

    public ICollection<WorkStageRecord> Stages { get; set; } = [];

    public ICollection<AlarmRecord> Alarms { get; set; } = [];
}
