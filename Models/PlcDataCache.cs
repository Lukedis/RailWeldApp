using CommunityToolkit.Mvvm.ComponentModel;

namespace RailWeldApp.Models;

/// <summary>
///  PLC数据缓存
/// </summary>
public partial class PlcDataCache : ObservableObject
{
    // 最近一次成功采集的快照
    [ObservableProperty]
    private PlcSnapshot? _lastSnapshot;

    // PLC 当前连接状态
    [ObservableProperty]
    private bool _isConnected;

    // 最近一次通信异常信息
    [ObservableProperty]
    private string? _lastError;

    // 最近一次成功采集时间
    [ObservableProperty]
    private DateTime? _lastUpdateTime;

    // 更新最新采集结果
    public void Update(PlcSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        LastSnapshot = snapshot;

        LastUpdateTime = snapshot.Timestamp;
    }
}
