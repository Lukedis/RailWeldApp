using System.ComponentModel;
using RailWeldApp.Enums;
using RailWeldApp.Models;

namespace RailWeldApp.ViewModels;

// 实时数据页：只做 PlcSnapshot -> UI 属性的映射，不直接读 PLC。
// 数据链路：PlcPollingBackgroundService -> PlcDataCache -> 本 VM -> XAML。
// 页面是 Transient，用弱事件订阅缓存，离开后可以被回收。
public partial class AdminRealTimeDataViewModel : PageViewModel
{
    private readonly PlcDataCache _plcDataCache;

    private PlcSnapshot? Snapshot => _plcDataCache.LastSnapshot;

    public AdminRealTimeDataViewModel(PlcDataCache plcDataCache) : base(PageName.AdminRealTimeData)
    {
        _plcDataCache = plcDataCache;

        // 不能直接 +=，否则单例 PlcDataCache 会强引用这个 Transient 页面 VM。
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
            // 一次刷新整页。5 Hz 下约 50 个绑定，WPF 可以承受。
            OnPropertyChanged(string.Empty);
        }
    }

    // 三轴
    public double XAxisActualPosition => Snapshot?.XActPos ?? 0;

    public double XAxisActualSpeed => Snapshot?.XActSpeed ?? 0;
    public double XAxisTargetPosition => Snapshot?.XSetPos ?? 0;
    public double XAxisTargetSpeed => Snapshot?.XSetSpeed ?? 0;
    public double XAxisCurrent => Snapshot?.XActCurrent ?? 0;

    public double YAxisActualPosition => Snapshot?.YActPos ?? 0;
    public double YAxisActualSpeed => Snapshot?.YActSpeed ?? 0;
    public double YAxisTargetPosition => Snapshot?.YSetPos ?? 0;
    public double YAxisTargetSpeed => Snapshot?.YSetSpeed ?? 0;
    public double YAxisCurrent => Snapshot?.YActCurrent ?? 0;

    public double ZAxisActualPosition => Snapshot?.ZActPos ?? 0;
    public double ZAxisActualSpeed => Snapshot?.ZActSpeed ?? 0;
    public double ZAxisTargetPosition => Snapshot?.ZSetPos ?? 0;
    public double ZAxisTargetSpeed => Snapshot?.ZSetSpeed ?? 0;
    public double ZAxisCurrent => Snapshot?.ZActCurrent ?? 0;

    // 12 路模拟量
    public double TempRailTop => Snapshot?.ActTemp1 ?? 0;

    public double TempRailFoot => Snapshot?.ActTemp2 ?? 0;
    public double TempRailInnerFoot => Snapshot?.ActTemp3 ?? 0;
    public double TempCooling => Snapshot?.ActTemp4 ?? 0;

    public double FlowRailTop => Snapshot?.ActFlow1 ?? 0;
    public double FlowRailWeb => Snapshot?.ActFlow2 ?? 0;
    public double FlowRailBottom => Snapshot?.ActFlow3 ?? 0;
    public double FlowSpare => 0;

    public double TrackY => Snapshot?.YActPos ?? 0;
    public double TrackZ => Snapshot?.ZActPos ?? 0;
    public double AirPressure => Snapshot?.ActPressure ?? 0;
    public double TrackSpare => 0;

    // 8 路开关量。状态位按当前 DB4 字段假设，接真实 PLC 时按现场点表核对。
    public bool IgbtPowerOn => Snapshot?.DY_ZT ?? false;

    public bool YAxisInPosition => Snapshot?.Y_CDJ_QGQW ?? false;
    public bool ZAxisInPosition => Snapshot?.Z_CDJ_QGQW ?? false;

    public bool JackSafePosition => Snapshot is
    {
        JLG_1_SW: true,
        JLG_2_SW: true,
        JLG_3_SW: true,
        JLG_4_SW: true
    };

    public bool AllowProcessing => Snapshot?.ProcessOk ?? false;
    public bool FeedComplete => Snapshot?.JLG_4_SW ?? false;
    public bool AutoRunning => Snapshot is { HeatRun: true } or { CoolingRun: true };
    public bool SpareActuator => false;

    // 电参数
    public double Voltage => Snapshot?.Voltage ?? 0;

    public double Current => Snapshot?.Current ?? 0;
    public double Frequency => Snapshot?.Frequency ?? 0;
    public double Power => Snapshot?.Power ?? 0;
    public double Energy => Snapshot?.Energy ?? 0;

    // 页脚
    public string AppVersion => "v1.0.0";

    public bool IsCommunicationOk => _plcDataCache.IsConnected;
    public int CommunicationDelayMs => 0;
    public string ClockText => DateTime.Now.ToString("HH:mm:ss");
    public string WeldId => "—";
}
