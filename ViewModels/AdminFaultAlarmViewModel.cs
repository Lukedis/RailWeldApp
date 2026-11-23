using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Models;
using RailWeldApp.Services;

namespace RailWeldApp.ViewModels;

// 故障报警：报警字边沿 -> 记录行；恢复只改状态不删行；确认只改本地，复位才写 PLC。
public partial class AdminFaultAlarmViewModel : PageViewModel
{
    private const int _maxRows = 500;

    /// <summary>
    ///  重置地址
    /// </summary>
    private const string _resetAddress = "DB5.200.0";

    /// <summary>
    ///  重置脉冲时间戳
    /// </summary>
    private static readonly TimeSpan _resetPulse = TimeSpan.FromMilliseconds(80);

    private readonly PlcDataCache _cache;
    private readonly IPlcService _plc;
    private readonly IDatabaseService _databaseService;
    private readonly IUserSession _session;
    private readonly ILogger<AdminFaultAlarmViewModel> _logger;

    /// <summary>
    ///  上一次警报1
    /// </summary>
    private ushort _lastAlarm1;

    /// <summary>
    /// 上一次警报2
    /// </summary>
    private ushort _lastAlarm2;

    public AdminFaultAlarmViewModel(
        PlcDataCache cache,
        IPlcService plc,
        IDatabaseService databaseService,
        IUserSession session,
        ILogger<AdminFaultAlarmViewModel> logger)
        : base(PageName.AdminFaultAlarm)
    {
        _cache = cache;
        _plc = plc;
        _databaseService = databaseService;
        _session = session;
        _logger = logger;

        // 获取对应的默认视图，方便排序、筛选、分组。
        AlarmView = CollectionViewSource.GetDefaultView(AlarmRows);

        // 按照选择的报警等级筛选，全部/故障/警告/提示。
        AlarmView.Filter = o => SelectedLevel == "全部" || (o is AlarmRow row && row.LevelText == SelectedLevel);

        SubscribeWeak<INotifyPropertyChanged, PropertyChangedEventArgs>(
            _cache, nameof(INotifyPropertyChanged.PropertyChanged), OnPlcDataChanged);

        if (_cache.LastSnapshot is { } snapshot)
        {
            ProcessAlarms(snapshot);
        }

        _ = LoadAlarmHistoryAsync();
    }

    /// <summary>
    /// 报警行
    /// </summary>
    public ObservableCollection<AlarmRow> AlarmRows { get; } = [];

    /// <summary>
    /// ICollectionView、排序、筛选、分组，以及管理当前选中项
    /// 报警视图
    /// </summary>
    public ICollectionView AlarmView { get; }

    public IReadOnlyList<string> LevelOptions { get; } = ["全部", "故障", "警告", "提示"];

    [ObservableProperty]
    private string _selectedLevel = "全部";

    partial void OnSelectedLevelChanged(string value) => AlarmView.Refresh();

    public int ActiveCount => AlarmRows.Count(x => x.IsActive);
    public bool HasActiveAlarm => ActiveCount > 0;
    public int TodayCount => AlarmRows.Count(x => x.OccurredAt.Date == DateTime.Today);
    public bool IsPlcConnected => _cache.IsConnected;

    public string LatestText => AlarmRows.Count == 0
        ? "—"
        : $"{AlarmRows[0].Time} · {AlarmRows[0].Message}（{AlarmRows[0].Code}）";

    public string BannerText
    {
        get
        {
            if (!IsPlcConnected)
            {
                return ActiveCount > 0 ? $"采集中断 · {ActiveCount} 条未复位报警" : "采集中断 · 无法获取报警状态";
            }

            if (ActiveCount == 0)
            {
                return "系统正常 · 暂无活动报警";
            }

            AlarmRow latest = AlarmRows.First(x => x.IsActive);

            string duration = FormatDuration(DateTime.Now - latest.OccurredAt);

            bool allAcknowledged = AlarmRows.Where(x => x.IsActive).All(x => x.IsConfirmed);

            return $"{ActiveCount} 条未复位报警 · 最近 {latest.Time} {latest.Message} · 已持续 {duration}"
                   + (allAcknowledged ? " · 已确认" : string.Empty);
        }
    }

    private void OnPlcDataChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => OnPlcDataChanged(sender, e)));

            return;
        }

        if (e.PropertyName == nameof(PlcDataCache.LastSnapshot) && _cache.LastSnapshot is { } snapshot)
        {
            ProcessAlarms(snapshot);
        }

        if (e.PropertyName is nameof(PlcDataCache.LastSnapshot)
            or nameof(PlcDataCache.IsConnected)
            or nameof(PlcDataCache.LastError))
        {
            // 所有统计/Banner 都是计算属性，统一刷新。
            OnPropertyChanged(string.Empty);
            ResetCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    ///  处理报警
    /// </summary>
    private void ProcessAlarms(PlcSnapshot snapshot)
    {
        if (snapshot.ActAlarm1 == _lastAlarm1 && snapshot.ActAlarm2 == _lastAlarm2)
        {
            return;
        }

        foreach ((int word, int bit, string code, string level, string message) in _alarmMap)
        {
            ushort oldValue = word == 1 ? _lastAlarm1 : _lastAlarm2;
            ushort newValue = word == 1 ? snapshot.ActAlarm1 : snapshot.ActAlarm2;

            bool wasActive = (oldValue & (1 << bit)) != 0;
            bool isActive = (newValue & (1 << bit)) != 0;

            if (isActive && !wasActive)
            {
                AddAlarm(code, level, message, snapshot.Timestamp);
            }
            else if (!isActive && wasActive)
            {
                RecoverAlarm(code, snapshot.Timestamp);
            }
        }

        _lastAlarm1 = snapshot.ActAlarm1;
        _lastAlarm2 = snapshot.ActAlarm2;
    }

    private void AddAlarm(string code, string level, string message, DateTime occurredAt)
    {
        if (AlarmRows.Any(x => x.IsActive && x.Code == code))
        {
            return;
        }

        var row = new AlarmRow
        {
            Code = code,
            LevelText = level,
            Message = message,
            OccurredAt = occurredAt,
        };

        AlarmRows.Insert(0, row);

        // 只裁已恢复的旧行，活动报警保留。
        while (AlarmRows.Count > _maxRows && !AlarmRows[^1].IsActive)
        {
            AlarmRows.RemoveAt(AlarmRows.Count - 1);
        }

        _ = PersistAlarmAsync(row);
    }

    private async Task PersistAlarmAsync(AlarmRow row)
    {
        try
        {
            var record = new AlarmRecord
            {
                Code = row.Code,
                Level = ParseLevel(row.LevelText),
                Message = row.Message,
                OccurredAt = row.OccurredAt,
                IsActive = true,
                IsAcknowledged = row.IsConfirmed,
            };

            await _databaseService.AddAlarmAsync(record);

            row.RecordId = record.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "报警记录写入数据库失败：{Code}", row.Code);
        }
    }

    private async Task LoadAlarmHistoryAsync()
    {
        try
        {
            DateTime from = DateTime.Today;
            DateTime to = from.AddDays(1);

            List<AlarmRecord> records = await _databaseService.GetAlarmsAsync(from, to, _maxRows);

            foreach (AlarmRecord record in records)
            {
                if (AlarmRows.Any(x => x.RecordId == record.Id
                    || (x.IsActive && x.Code == record.Code)))
                {
                    continue;
                }

                var row = new AlarmRow
                {
                    RecordId = record.Id,
                    Code = record.Code,
                    LevelText = LevelText(record.Level),
                    Message = record.Message,
                    OccurredAt = record.OccurredAt,
                    IsActive = record.IsActive,
                    IsConfirmed = record.IsAcknowledged,
                    DurationText = record.RecoveredAt is { } recoveredAt
                        ? FormatDuration(recoveredAt - record.OccurredAt)
                        : "进行中",
                };

                AlarmRows.Add(row);
            }

            OnPropertyChanged(string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取报警历史失败。");
        }
    }

    private static AlarmLevel ParseLevel(string level) => level switch
    {
        "故障" => AlarmLevel.Fault,
        "警告" => AlarmLevel.Warning,
        _ => AlarmLevel.Info,
    };

    private static string LevelText(AlarmLevel level) => level switch
    {
        AlarmLevel.Fault => "故障",
        AlarmLevel.Warning => "警告",
        _ => "提示",
    };

    /// <summary>
    ///  恢复报警
    /// </summary>

    private void RecoverAlarm(string code, DateTime recoveredAt)
    {
        AlarmRow? row = AlarmRows.FirstOrDefault(x => x.IsActive && x.Code == code);

        if (row is null)
        {
            return;
        }

        row.IsActive = false;
        row.DurationText = FormatDuration(recoveredAt - row.OccurredAt);

        _ = PersistRecoveryAsync(row, recoveredAt);
    }

    private async Task PersistRecoveryAsync(AlarmRow row, DateTime recoveredAt)
    {
        if (row.RecordId == 0)
            return;

        try
        {
            await _databaseService.RecoverAlarmAsync(row.RecordId, recoveredAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "报警恢复状态写入数据库失败：{Code}", row.Code);
        }
    }

    [RelayCommand]
    private async Task AcknowledgeAllAsync()
    {
        foreach (AlarmRow row in AlarmRows.Where(x => x.IsActive))
        {
            row.IsConfirmed = true;
        }

        try
        {
            await _databaseService.AcknowledgeAllActiveAlarmsAsync(
                _session.Account,
                userId: null,
                acknowledgedAt: DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "报警确认写入数据库失败。");
        }

        OnPropertyChanged(string.Empty);
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
            await _plc.WriteAsync(_resetAddress, true);
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
                await _plc.WriteAsync(_resetAddress, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放 PLC 复位位失败。");
            }
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        return duration.TotalSeconds < 60
            ? $"{duration.Seconds} 秒"
            : duration.TotalMinutes < 60
                ? $"{duration.Minutes} 分 {duration.Seconds} 秒"
                : $"{(int)duration.TotalHours} 小时 {duration.Minutes} 分";
    }

    // 报警位定义（旧系统说明 4.7），接现场 PLC 时按点表核对。
    private static readonly (int Word, int Bit, string Code, string Level, string Message)[] _alarmMap =
    [
        (1, 0, "F1000", "故障", "急停按下"),
        (1, 1, "F1001", "故障", "系统未上电"),
        (1, 2, "F1002", "故障", "IGBT 电源故障"),
        (1, 3, "F1003", "警告", "气压不足"),
        (1, 4, "F1004", "警告", "能量报警"),
        (1, 5, "F1005", "提示", "喷风模式未选择"),
        (1, 8, "F1008", "故障", "X 轴伺服故障"),
        (1, 9, "F1009", "故障", "Y 轴伺服故障"),
        (1, 10, "F1010", "故障", "Z 轴伺服故障"),
        (1, 11, "F1011", "警告", "冷却水温过高"),
        (1, 12, "F1012", "警告", "冷却水温过低"),
        (1, 13, "F1013", "故障", "Y 跟踪气缸前进故障"),
        (1, 14, "F1014", "故障", "Y 跟踪气缸后退故障"),
        (1, 15, "F1015", "故障", "Z 跟踪气缸上升故障"),
        (2, 0, "F2000", "故障", "Z 轴跟踪气缸下降故障"),
        (2, 1, "F2001", "故障", "1# 顶料缸上升故障"),
        (2, 2, "F2002", "故障", "1# 顶料缸下降故障"),
        (2, 3, "F2003", "故障", "2# 顶料缸上升故障"),
        (2, 4, "F2004", "故障", "2# 顶料缸下降故障"),
        (2, 5, "F2005", "故障", "3# 顶料缸上升故障"),
        (2, 6, "F2006", "故障", "3# 顶料缸下降故障"),
        (2, 7, "F2007", "故障", "4# 顶料缸上升故障"),
        (2, 8, "F2008", "故障", "4# 顶料缸下降故障"),
    ];
}