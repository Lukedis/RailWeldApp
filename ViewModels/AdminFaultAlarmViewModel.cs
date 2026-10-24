using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Models;

namespace RailWeldApp.ViewModels;

// 故障报警：监听 PlcDataCache 的报警字，按位做边沿检测。
// 报警出现插入新行；恢复只把旧行标记为已恢复，不删行。
// 页面没有定时器，只有快照变化时才处理。
public partial class AdminFaultAlarmViewModel : PageViewModel
{
    private const int _maxRows = 500;
    private const string _resetAddress = "DB5.200.0";
    private static readonly TimeSpan _resetPulse = TimeSpan.FromMilliseconds(80);

    private readonly PlcDataCache _plcDataCache;
    private readonly IPlcService _plcService;
    private readonly ILogger<AdminFaultAlarmViewModel> _logger;

    private readonly Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

    // 当前仍在活动的报警：Code -> 行
    private readonly Dictionary<string, AlarmRow> _activeRows = [];

    private ushort _lastAlarm1;
    private ushort _lastAlarm2;

    public AdminFaultAlarmViewModel(PlcDataCache plcDataCache, IPlcService plcService, ILogger<AdminFaultAlarmViewModel> logger)
        : base(PageName.AdminFaultAlarm)
    {
        _plcDataCache = plcDataCache;
        _plcService = plcService;
        _logger = logger;

        AlarmView = CollectionViewSource.GetDefaultView(AlarmRows);
        AlarmView.Filter = FilterAlarm;

        SubscribeWeak<INotifyPropertyChanged, PropertyChangedEventArgs>(
            _plcDataCache,
            nameof(INotifyPropertyChanged.PropertyChanged),
            OnPlcDataChanged);

        if (_plcDataCache.LastSnapshot is { } snapshot)
        {
            ProcessAlarms(snapshot);
        }

        RefreshSummary();
    }

    public ObservableCollection<AlarmRow> AlarmRows { get; } = [];

    public ICollectionView AlarmView { get; }

    public IReadOnlyList<string> LevelOptions { get; } = ["全部", "故障", "警告", "提示"];

    [ObservableProperty]
    private string _selectedLevel = "全部";

    [ObservableProperty]
    private bool _hasActiveAlarm;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _todayCount;

    [ObservableProperty]
    private string _bannerText = "系统正常 · 暂无活动报警";

    [ObservableProperty]
    private string _latestText = "—";

    public bool IsPlcConnected => _plcDataCache.IsConnected;

    public string AppVersion => "v1.0.0";
    public bool IsCommunicationOk => _plcDataCache.IsConnected;
    public int CommunicationDelayMs => 0;
    public string ClockText => DateTime.Now.ToString("HH:mm:ss");

    partial void OnSelectedLevelChanged(string value) => AlarmView.Refresh();

    private void OnPlcDataChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 报警集合/ICollectionView 必须在 UI 线程改。
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(new Action(() => OnPlcDataChanged(sender, e)));
            return;
        }

        if (e.PropertyName == nameof(PlcDataCache.LastSnapshot) && _plcDataCache.LastSnapshot is { } snapshot)
        {
            ProcessAlarms(snapshot);
            RefreshSummary();
            return;
        }

        if (e.PropertyName is nameof(PlcDataCache.IsConnected)
            or nameof(PlcDataCache.LastError))
        {
            OnPropertyChanged(nameof(IsPlcConnected));
            OnPropertyChanged(nameof(IsCommunicationOk));
            ResetCommand.NotifyCanExecuteChanged();
            RefreshSummary();
        }
    }

    private void ProcessAlarms(PlcSnapshot snapshot)
    {
        ushort alarm1 = snapshot.ActAlarm1;
        ushort alarm2 = snapshot.ActAlarm2;

        if (alarm1 == _lastAlarm1 && alarm2 == _lastAlarm2)
        {
            return;
        }

        ProcessWord(1, _lastAlarm1, alarm1, snapshot.Timestamp);
        ProcessWord(2, _lastAlarm2, alarm2, snapshot.Timestamp);

        _lastAlarm1 = alarm1;
        _lastAlarm2 = alarm2;
    }

    private void ProcessWord(int word, ushort oldValue, ushort newValue, DateTime timestamp)
    {
        foreach (AlarmDefinition definition in _definitions)
        {
            if (definition.Word != word)
            {
                continue;
            }

            bool wasActive = (oldValue & (1 << definition.Bit)) != 0;
            bool isActive = (newValue & (1 << definition.Bit)) != 0;

            if (isActive && !wasActive)
            {
                AddAlarm(definition, timestamp);
            }
            else if (!isActive && wasActive)
            {
                RecoverAlarm(definition.Code, timestamp);
            }
        }
    }

    private void AddAlarm(AlarmDefinition definition, DateTime occurredAt)
    {
        // 同一位还在活动时不重复插入。
        if (_activeRows.ContainsKey(definition.Code))
        {
            return;
        }

        var row = new AlarmRow
        {
            Code = definition.Code,
            LevelText = definition.LevelText,
            Message = definition.Message,
            OccurredAt = occurredAt,
        };

        AlarmRows.Insert(0, row);
        _activeRows[definition.Code] = row;

        // 只裁掉已恢复的旧行，活动报警保留。
        while (AlarmRows.Count > _maxRows)
        {
            AlarmRow last = AlarmRows[^1];

            if (last.IsActive)
            {
                break;
            }

            AlarmRows.RemoveAt(AlarmRows.Count - 1);
        }
    }

    private void RecoverAlarm(string code, DateTime recoveredAt)
    {
        if (!_activeRows.Remove(code, out AlarmRow? row))
        {
            return;
        }

        row.IsActive = false;
        row.DurationText = FormatDuration(recoveredAt - row.OccurredAt);
    }

    private void RefreshSummary()
    {
        ActiveCount = _activeRows.Count;
        HasActiveAlarm = ActiveCount > 0;
        TodayCount = AlarmRows.Count(x => x.OccurredAt.Date == DateTime.Today);

        if (AlarmRows.Count > 0)
        {
            AlarmRow latest = AlarmRows[0];
            LatestText = $"{latest.Time} · {latest.Message}（{latest.Code}）";
        }
        else
        {
            LatestText = "—";
        }

        if (!IsPlcConnected)
        {
            BannerText = ActiveCount > 0
                ? $"采集中断 · {ActiveCount} 条未复位报警"
                : "采集中断 · 无法获取报警状态";
        }
        else if (ActiveCount == 0)
        {
            BannerText = "系统正常 · 暂无活动报警";
        }
        else
        {
            AlarmRow latestActive = _activeRows.Values
                .OrderByDescending(x => x.OccurredAt)
                .First();

            string duration = FormatDuration(DateTime.Now - latestActive.OccurredAt);

            bool allAcknowledged = _activeRows.Values.All(x => x.IsAcknowledged);

            BannerText = $"{ActiveCount} 条未复位报警 · 最近 {latestActive.Time} {latestActive.Message} · 已持续 {duration}"
                + (allAcknowledged ? " · 已确认" : string.Empty);
        }

        OnPropertyChanged(nameof(ClockText));
        OnPropertyChanged(nameof(IsCommunicationOk));
        ResetCommand.NotifyCanExecuteChanged();
    }

    private bool FilterAlarm(object item)
        => SelectedLevel == "全部" || (item is AlarmRow row && row.LevelText == SelectedLevel);

    [RelayCommand]
    private void AcknowledgeAll()
    {
        foreach (AlarmRow row in _activeRows.Values)
        {
            row.IsAcknowledged = true;
        }

        // TODO: 报警确认记录写入本地数据库。
        RefreshSummary();
    }

    [RelayCommand(CanExecute = nameof(IsPlcConnected))]
    private async Task ResetAsync()
    {
        if (!IsPlcConnected)
        {
            return;
        }

        try
        {
            await _plcService.WriteAsync(_resetAddress, true);
            await Task.Delay(_resetPulse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复位 PLC 报警失败。");
        }
        finally
        {
            try
            {
                await _plcService.WriteAsync(_resetAddress, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复位 PLC 报警位释放失败。");
            }
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        if (duration.TotalSeconds < 60)
        {
            return $"{duration.Seconds} 秒";
        }

        if (duration.TotalMinutes < 60)
        {
            return $"{duration.Minutes} 分 {duration.Seconds} 秒";
        }

        return $"{(int)duration.TotalHours} 小时 {duration.Minutes} 分";
    }

    private readonly record struct AlarmDefinition(int Word, int Bit, string Code, string LevelText, string Message);

    // 报警位定义来自旧系统说明 4.7，接现场 PLC 时按点表核对。
    private static readonly AlarmDefinition[] _definitions =
    [
        new(1, 0, "F1000", "故障", "急停按下"),
        new(1, 1, "F1001", "故障", "系统未上电"),
        new(1, 2, "F1002", "故障", "IGBT 电源故障"),
        new(1, 3, "F1003", "警告", "气压不足"),
        new(1, 4, "F1004", "警告", "能量报警"),
        new(1, 5, "F1005", "提示", "喷风模式未选择"),
        new(1, 8, "F1008", "故障", "X 轴伺服故障"),
        new(1, 9, "F1009", "故障", "Y 轴伺服故障"),
        new(1, 10, "F1010", "故障", "Z 轴伺服故障"),
        new(1, 11, "F1011", "警告", "冷却水温过高"),
        new(1, 12, "F1012", "警告", "冷却水温过低"),
        new(1, 13, "F1013", "故障", "Y 跟踪气缸前进故障"),
        new(1, 14, "F1014", "故障", "Y 跟踪气缸后退故障"),
        new(1, 15, "F1015", "故障", "Z 跟踪气缸上升故障"),
        new(2, 0, "F2000", "故障", "Z 轴跟踪气缸下降故障"),
        new(2, 1, "F2001", "故障", "1# 顶料缸上升故障"),
        new(2, 2, "F2002", "故障", "1# 顶料缸下降故障"),
        new(2, 3, "F2003", "故障", "2# 顶料缸上升故障"),
        new(2, 4, "F2004", "故障", "2# 顶料缸下降故障"),
        new(2, 5, "F2005", "故障", "3# 顶料缸上升故障"),
        new(2, 6, "F2006", "故障", "3# 顶料缸下降故障"),
        new(2, 7, "F2007", "故障", "4# 顶料缸上升故障"),
        new(2, 8, "F2008", "故障", "4# 顶料缸下降故障"),
    ];
}
