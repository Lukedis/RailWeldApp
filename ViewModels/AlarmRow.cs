using CommunityToolkit.Mvvm.ComponentModel;

namespace RailWeldApp.ViewModels;

// 报警列表的一行：界面只读，状态变化由 VM 在报警字边沿时更新。
public partial class AlarmRow : ObservableObject
{
    public required string Code { get; init; }

    public required string LevelText { get; init; }

    public required string Message { get; init; }

    public DateTime OccurredAt { get; init; }

    public string Time => OccurredAt.ToString("HH:mm:ss");

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string _durationText = "刚刚";

    [ObservableProperty]
    private bool _isAcknowledged;
}
