namespace RailWeldApp.DataModels;

/// <summary>
/// 作业结束后按采样数据汇总出的指标，供历史查询和报告直接读取。
/// </summary>
public sealed class WorkMetricRecord
{
    public long Id { get; set; }

    public string WorkRecordId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public double StartValue { get; set; }

    public double PeakValue { get; set; }

    public double EndValue { get; set; }

    public double AverageValue { get; set; }

    public double MinValue { get; set; }

    public double MaxValue { get; set; }

    public int OverCount { get; set; }

    public double PeakRatio { get; set; }

    public string Judgement { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public WorkRecord? WorkRecord { get; set; }
}
