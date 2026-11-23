using System.Collections.ObjectModel;

namespace RailWeldApp.ViewModels;

// 一条历史记录，右侧只绑这个对象。
// Metric / Sample 是行类型，不是独立的记录模型。
public sealed class HistoryRecord
{
    public string RecordId { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public string StartedAtText => StartedAt.ToString("HH:mm:ss");
    public string MetaText { get; init; } = string.Empty;
    public bool IsAbnormal { get; init; }

    public ObservableCollection<Metric> Metrics { get; } = [];
    public ObservableCollection<Sample> Samples { get; } = [];

    public sealed class Metric
    {
        public string Name { get; init; } = string.Empty;
        public string Start { get; init; } = "—";
        public string Peak { get; init; } = "—";
        public string End { get; init; } = "—";
        public string Average { get; init; } = "—";
        public int OverCount { get; init; }
        public double PeakRatio { get; init; }
        public bool HasOverLimit => OverCount > 0;
    }

    public sealed class Sample
    {
        public string Time { get; init; } = string.Empty;
        public string Temp1 { get; init; } = "—";
        public string Temp2 { get; init; } = "—";
        public string Temp3 { get; init; } = "—";
        public string Temp4 { get; init; } = "—";
        public string Pressure { get; init; } = "—";
        public string Flow1 { get; init; } = "—";
        public string Flow2 { get; init; } = "—";
        public string Flow3 { get; init; } = "—";
    }
}
