using CommunityToolkit.Mvvm.ComponentModel;

namespace RailWeldApp.ViewModels;

// 报警列表的一行：界面只读，状态变化由 VM 在报警字边沿时更新。
public partial class AlarmRow : ObservableObject
{
    public long RecordId { get; set; }

    public string Code { get; init; } = string.Empty;

    public string LevelText { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    /// <summary>
    ///  报警发生于
    /// </summary>
    public DateTime OccurredAt { get; init; }

    public string Time => OccurredAt.ToString("HH:mm:ss");

    /// <summary>
    ///  是否处于激活状态
    /// </summary>
    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string _durationText = "刚刚";

    /// <summary>
    /// 是否已确认
    /// </summary>
    [ObservableProperty]
    private bool _isConfirmed;
}