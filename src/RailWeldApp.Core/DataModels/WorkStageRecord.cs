using RailWeldApp.Enums;

namespace RailWeldApp.DataModels;

/// <summary>
/// 作业阶段参数快照。配方后续修改不会影响历史报告。
/// </summary>
public sealed class WorkStageRecord
{
    public long Id { get; set; }

    public string WorkRecordId { get; set; } = string.Empty;

    public WeldStage Stage { get; set; }

    public int SortOrder { get; set; }

    public double Temperature { get; set; }

    public double Power { get; set; }

    public CoolingModes CoolingMode { get; set; }

    public string CoolingParameter { get; set; } = string.Empty;

    public WorkRecord? WorkRecord { get; set; }
}
