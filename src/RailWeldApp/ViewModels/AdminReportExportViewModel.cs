using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Services;

namespace RailWeldApp.ViewModels;

// 报告导出：左侧报告列表，右侧只显示选中的 Report。
public partial class AdminReportExportViewModel : PageViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IUserSession _session;
    private readonly ILogger<AdminReportExportViewModel> _logger;

    // 保存当天全部报告；搜索框只过滤这个列表，不重复访问数据库。
    private readonly List<Report> _allReports = [];

    public AdminReportExportViewModel(IDatabaseService databaseService, IUserSession session, ILogger<AdminReportExportViewModel> logger)
        : base(PageName.AdminReportExport)
    {
        _databaseService = databaseService;
        _session = session;
        _logger = logger;

        _ = FetchReportsAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultCount))]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private ObservableCollection<Report> _reportItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRecord))]
    [NotifyPropertyChangedFor(nameof(Report))]
    [NotifyPropertyChangedFor(nameof(HasReport))]
    private string _selectedRecordId = string.Empty;

    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public Report? SelectedRecord => ReportItems.FirstOrDefault(x => x.RecordId == SelectedRecordId);

    public Report? Report => SelectedRecord;

    public int ResultCount => ReportItems.Count;
    public bool HasResults => ReportItems.Count > 0;
    public bool HasReport => SelectedRecord is not null;
    public string CurrentAccount => _session.Account;

    partial void OnSelectedDateChanged(DateTime? value) => _ = FetchReportsAsync();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private async Task FetchReportsAsync()
    {
        IsLoading = true;

        try
        {
            DateTime day = SelectedDate?.Date ?? DateTime.Today;
            DateTime from = day;
            DateTime to = day.AddDays(1);

            // 报告只需要指标和工艺阶段，不需要逐秒采样点。
            List<WorkRecord> records = await _databaseService.GetWorkRecordsAsync(
                from,
                to,
                CurrentAccount);

            _allReports.Clear();
            _allReports.AddRange(records.Select(MapReport));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取报告记录失败。");
            _allReports.Clear();
        }
        finally
        {
            ApplyFilter();
            IsLoading = false;
        }
    }

    private static Report MapReport(WorkRecord record)
    {
        // 把数据库实体转换成只读的 Report，界面和 PDF 服务都只依赖这个模型。
        var report = new Report
        {
            RecordId = record.Id,
            AccountName = record.AccountName,
            TimeText = record.StartedAt.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture),
            IsAbnormal = record.Result is WorkResult.Unqualified or WorkResult.Abnormal,
            OperatorName = string.IsNullOrWhiteSpace(record.OperatorName) ? record.AccountName : record.OperatorName,
            JobNumber = string.IsNullOrWhiteSpace(record.JobNumber) ? "—" : record.JobNumber,
            ProducedAt = record.StartedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            EquipmentName = string.IsNullOrWhiteSpace(record.EquipmentName) ? "—" : record.EquipmentName,
            RecipeName = string.IsNullOrWhiteSpace(record.RecipeName) ? "—" : record.RecipeName,
            ShiftName = string.IsNullOrWhiteSpace(record.ShiftName) ? "—" : record.ShiftName,
            ReportNo = string.IsNullOrWhiteSpace(record.ReportNo) ? record.Id : record.ReportNo,
            IsQualified = record.Result == WorkResult.Qualified,
            ResultText = ResultText(record.Result),
            ResultDetail = string.IsNullOrWhiteSpace(record.ResultDetail) ? "—" : record.ResultDetail,
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        };

        foreach (WorkStageRecord stage in record.Stages.OrderBy(x => x.SortOrder))
        {
            report.ProcessRows.Add(new Report.ProcessRow
            {
                Section = StageText(stage.Stage),
                TempText = Format(stage.Temperature),
                PowerText = Format(stage.Power),
                CoolingMode = stage.CoolingMode == CoolingModes.Temperature ? "温度" : "时间",
                CoolingParam = string.IsNullOrWhiteSpace(stage.CoolingParameter) ? "—" : stage.CoolingParameter,
            });
        }

        foreach (WorkMetricRecord metric in record.Metrics.OrderBy(x => x.SortOrder))
        {
            report.MetricRows.Add(new Report.MetricRow
            {
                Name = string.IsNullOrWhiteSpace(metric.Unit)
                    ? metric.Name
                    : $"{metric.Name} ({metric.Unit})",
                Start = Format(metric.StartValue),
                Peak = Format(metric.PeakValue),
                End = Format(metric.EndValue),
                Average = Format(metric.AverageValue),
                MinText = Format(metric.MinValue),
                MaxText = Format(metric.MaxValue),
                OverCount = metric.OverCount,
                Judgement = string.IsNullOrWhiteSpace(metric.Judgement) ? "—" : metric.Judgement,
            });
        }

        return report;
    }

    private static string ResultText(WorkResult result) => result switch
    {
        WorkResult.Qualified => "合格",
        WorkResult.Unqualified => "不合格",
        WorkResult.Abnormal => "异常",
        _ => "待判定",
    };

    private static string StageText(WeldStage stage) => stage switch
    {
        WeldStage.Start => "起始段",
        WeldStage.Transition => "转变段",
        WeldStage.Final => "最终段",
        _ => "—",
    };

    private static string Format(double value)
        => value.ToString("0.##", CultureInfo.InvariantCulture);

    private void ApplyFilter()
    {
        string keyword = SearchText.Trim();

        IEnumerable<Report> result = _allReports;

        if (keyword.Length > 0)
        {
            result = result.Where(x => x.AccountName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        SetReports(result);

        SelectedRecordId = ReportItems.FirstOrDefault()?.RecordId ?? string.Empty;
    }

    /// <summary>
    ///  设置集合，并监听集合变化。
    /// </summary>
    private void SetReports(IEnumerable<Report> reports)
    {
        ReportItems.CollectionChanged -= OnReportItemsCollectionChanged;
        ReportItems = [.. reports];
        ReportItems.CollectionChanged += OnReportItemsCollectionChanged;
    }

    private void OnReportItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ResultCount));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(SelectedRecord));
        OnPropertyChanged(nameof(Report));
        OnPropertyChanged(nameof(HasReport));
    }
}
