using System.Collections.ObjectModel;

namespace RailWeldApp.ViewModels;

// 一条报告记录，右侧只绑这个对象。
// ProcessRow / MetricRow 是行类型，不是独立的记录模型。
public sealed class Report
{
    public string RecordId { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public bool IsAbnormal { get; init; }

    public string OperatorName { get; init; } = "—";
    public string JobNumber { get; init; } = "—";
    public string ProducedAt { get; init; } = "—";
    public string EquipmentName { get; init; } = "—";
    public string RecipeName { get; init; } = "—";
    public string ShiftName { get; init; } = "—";
    public string ReportNo { get; init; } = "—";
    public bool IsQualified { get; init; }
    public string ResultText { get; init; } = "待判定";
    public string ResultDetail { get; init; } = "—";
    public string GeneratedAt { get; init; } = "—";

    public ObservableCollection<ProcessRow> ProcessRows { get; } = [];
    public ObservableCollection<MetricRow> MetricRows { get; } = [];

    public sealed class ProcessRow
    {
        public string Section { get; init; } = string.Empty;
        public string TempText { get; init; } = "—";
        public string PowerText { get; init; } = "—";
        public string CoolingMode { get; init; } = "—";
        public string CoolingParam { get; init; } = "—";
    }

    public sealed class MetricRow
    {
        public string Name { get; init; } = string.Empty;
        public string Start { get; init; } = "—";
        public string Peak { get; init; } = "—";
        public string End { get; init; } = "—";
        public string Average { get; init; } = "—";
        public string MinText { get; init; } = "—";
        public string MaxText { get; init; } = "—";
        public int OverCount { get; init; }
        public bool HasOverLimit => OverCount > 0;
        public string Judgement { get; init; } = "—";
    }
}
