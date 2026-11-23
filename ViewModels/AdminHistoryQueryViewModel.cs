using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Services;

namespace RailWeldApp.ViewModels;

// 历史查询：左侧记录列表，右侧只显示选中的 HistoryRecord。
public partial class AdminHistoryQueryViewModel : PageViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IUserSession _session;
    private readonly ILogger<AdminHistoryQueryViewModel> _logger;

    private readonly List<HistoryRecord> _allRecords = [];

    // 采样点数据量大，只在选中某条记录时加载；这里记录已经加载过的记录 Id。
    private readonly HashSet<string> _loadedSampleRecords = [];

    // 日期快速切换时，旧请求可能晚返回；用 generation 丢弃过期结果。
    private int _fetchGeneration;

    public AdminHistoryQueryViewModel(IDatabaseService databaseService, IUserSession session, ILogger<AdminHistoryQueryViewModel> logger)
        : base(PageName.AdminHistoryQuery)
    {
        _databaseService = databaseService;
        _session = session;
        _logger = logger;

        _ = FetchRecordsAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultCount))]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private ObservableCollection<HistoryRecord> _records = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRecord))]
    private string _selectedRecordId = string.Empty;

    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public HistoryRecord? SelectedRecord
        => Records.FirstOrDefault(x => x.RecordId == SelectedRecordId);

    public int ResultCount => Records.Count;
    public bool HasResults => Records.Count > 0;
    public string CurrentAccount => _session.Account;

    partial void OnSelectedDateChanged(DateTime? value) => _ = FetchRecordsAsync();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedRecordIdChanged(string value) => _ = LoadSelectedSamplesAsync(value);

    private async Task FetchRecordsAsync()
    {
        // 每次查询递增；await 返回后如果 generation 已变，说明用户又换了日期。
        int generation = ++_fetchGeneration;

        IsLoading = true;

        try
        {
            DateTime day = SelectedDate?.Date ?? DateTime.Today;
            DateTime from = day;
            DateTime to = day.AddDays(1);

            List<WorkRecord> records = await _databaseService.GetWorkRecordsAsync(
                from,
                to,
                CurrentAccount);

            if (generation != _fetchGeneration)
                return;

            _loadedSampleRecords.Clear();
            _allRecords.Clear();
            _allRecords.AddRange(records.Select(MapRecord));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取历史记录失败。");

            _allRecords.Clear();
        }
        finally
        {
            if (generation == _fetchGeneration)
            {
                ApplyFilter();
                IsLoading = false;
            }
        }
    }

    private async Task LoadSelectedSamplesAsync(string recordId)
    {
        // 同一记录只加载一次；列表查询不读取 WorkSample，避免一次拉太多行。
        if (string.IsNullOrWhiteSpace(recordId)
            || _loadedSampleRecords.Contains(recordId))
        {
            return;
        }

        HistoryRecord? record = _allRecords.FirstOrDefault(x => x.RecordId == recordId);

        if (record is null)
            return;

        try
        {
            List<WorkSampleRecord> samples = await _databaseService.GetWorkSamplesAsync(recordId);

            if (record.RecordId != SelectedRecordId)
                return;

            record.Samples.Clear();

            foreach (WorkSampleRecord sample in samples)
            {
                record.Samples.Add(new HistoryRecord.Sample
                {
                    Time = sample.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                    Temp1 = Format(sample.Temperature1),
                    Temp2 = Format(sample.Temperature2),
                    Temp3 = Format(sample.Temperature3),
                    Temp4 = Format(sample.Temperature4),
                    Pressure = Format(sample.Pressure),
                    Flow1 = Format(sample.Flow1),
                    Flow2 = Format(sample.Flow2),
                    Flow3 = Format(sample.Flow3),
                });
            }

            _loadedSampleRecords.Add(recordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取历史采样点失败：{RecordId}", recordId);
        }
    }

    private static HistoryRecord MapRecord(WorkRecord record)
    {
        var result = new HistoryRecord
        {
            RecordId = record.Id,
            AccountName = record.AccountName,
            StartedAt = record.StartedAt,
            IsAbnormal = record.Result is WorkResult.Unqualified or WorkResult.Abnormal,
            MetaText = string.Join(" · ", new[]
            {
                record.RecipeName,
                record.ShiftName,
                record.ResultDetail,
            }.Where(x => !string.IsNullOrWhiteSpace(x))),
        };

        foreach (WorkMetricRecord metric in record.Metrics.OrderBy(x => x.SortOrder))
        {
            result.Metrics.Add(new HistoryRecord.Metric
            {
                Name = string.IsNullOrWhiteSpace(metric.Unit)
                    ? metric.Name
                    : $"{metric.Name} ({metric.Unit})",
                Start = Format(metric.StartValue),
                Peak = Format(metric.PeakValue),
                End = Format(metric.EndValue),
                Average = Format(metric.AverageValue),
                OverCount = metric.OverCount,
                PeakRatio = metric.PeakRatio,
            });
        }

        return result;
    }

    private static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private void ApplyFilter()
    {
        string keyword = SearchText.Trim();

        IEnumerable<HistoryRecord> result = _allRecords;

        if (keyword.Length > 0)
        {
            result = result.Where(x => x.AccountName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        SetRecords(result);

        SelectedRecordId = Records.FirstOrDefault()?.RecordId ?? string.Empty;
    }

    /// <summary>
    ///  设置集合，并监听集合变化。
    /// </summary>
    private void SetRecords(IEnumerable<HistoryRecord> records)
    {
        Records.CollectionChanged -= OnRecordsCollectionChanged;
        Records = [.. records];
        Records.CollectionChanged += OnRecordsCollectionChanged;
    }

    private void OnRecordsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ResultCount));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(SelectedRecord));
    }
}