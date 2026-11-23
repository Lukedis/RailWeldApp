using System.ComponentModel;
using RailWeldApp.Enums;
using RailWeldApp.Models;

namespace RailWeldApp.ViewModels;

// 温度监视：8 路采集量直接从 PlcDataCache 映射，量程/阈值由 XAML 决定。
// 页面 Transient，使用弱事件订阅，离开后可回收。
public partial class AdminTempMonitorViewModel : PageViewModel
{
    private readonly PlcDataCache _plcDataCache;
    private PlcSnapshot? Snapshot => _plcDataCache.LastSnapshot;

    public AdminTempMonitorViewModel(PlcDataCache plcDataCache) : base(PageName.AdminTempMonitor)
    {
        _plcDataCache = plcDataCache;

        SubscribeWeak<INotifyPropertyChanged, PropertyChangedEventArgs>(
            _plcDataCache,
            nameof(INotifyPropertyChanged.PropertyChanged),
            OnPlcDataChanged);
    }

    private void OnPlcDataChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PlcDataCache.LastSnapshot)
            or nameof(PlcDataCache.IsConnected)
            or nameof(PlcDataCache.LastError))
        {
            OnPropertyChanged(string.Empty);
        }
    }

    public double RailTopTemp => Snapshot?.ActTemp1 ?? 0;
    public double RailFootTemp => Snapshot?.ActTemp2 ?? 0;
    public double RailInnerFootTemp => Snapshot?.ActTemp3 ?? 0;
    public double CoolingTemp => Snapshot?.ActTemp4 ?? 0;

    public double AirPressure => Snapshot?.ActPressure ?? 0;

    public double FlowTop => Snapshot?.ActFlow1 ?? 0;
    public double FlowWeb => Snapshot?.ActFlow2 ?? 0;
    public double FlowBottom => Snapshot?.ActFlow3 ?? 0;
}
