using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Models;

namespace RailWeldApp.Services;

/// <summary>
/// 监听 PLC 的加热运行状态，自动形成 WorkRecord。
///
/// 状态变化：
/// HeatRun=true  → 创建 WorkRecord，并先保存主记录和 WorkStage；
/// 运行中        → 每秒追加一个 WorkSample；
/// HeatRun=false → 计算 WorkMetric，补写采样、指标和结束时间。
///
/// 先保存主记录是为了在运行中断电时，至少保留作业和工艺阶段。
/// </summary>
public sealed class WorkRecordRecorder : IDisposable
{
    private readonly PlcDataCache _cache;
    private readonly IDatabaseService _databaseService;
    private readonly IUserSession _session;
    private readonly ILogger<WorkRecordRecorder> _logger;
    private readonly object _gate = new();

    private WorkRecord? _activeRecord;
    private List<WorkSampleRecord> _samples = [];
    private DateTime _lastSampleAt;

    public WorkRecordRecorder(
        PlcDataCache cache,
        IDatabaseService databaseService,
        IUserSession session,
        ILogger<WorkRecordRecorder> logger)
    {
        _cache = cache;
        _databaseService = databaseService;
        _session = session;
        _logger = logger;

        _cache.PropertyChanged += OnCacheChanged;

        if (_session is INotifyPropertyChanged sessionNotify)
            sessionNotify.PropertyChanged += OnSessionChanged;
    }

    private void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName is nameof(IUserSession.Account) or nameof(IUserSession.IsSignedIn))
            && _cache.LastSnapshot is { } snapshot)
        {
            Evaluate(snapshot);
        }
    }

    private void OnCacheChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(PlcDataCache.LastSnapshot) or nameof(PlcDataCache.IsConnected)))
            return;

        if (_cache.LastSnapshot is { } snapshot)
            Evaluate(snapshot);
    }

    private void Evaluate(PlcSnapshot snapshot)
    {
        // 采集线程和 UI 线程都会触发这里，用锁保证记录状态只被处理一次。
        lock (_gate)
        {
            // 断开 PLC 也算一次作业结束，避免记录一直停留在“进行中”。
            if (!_cache.IsConnected)
            {
                if (_activeRecord is not null)
                    FinishRecord(snapshot);

                return;
            }

            if (!_session.IsSignedIn)
            {
                if (_activeRecord is not null)
                    FinishRecord(snapshot);

                return;
            }

            if (snapshot.HeatRun)
            {
                if (_activeRecord is null)
                    StartRecord(snapshot);

                if (_activeRecord is not null
                    && snapshot.Timestamp - _lastSampleAt >= TimeSpan.FromSeconds(1))
                {
                    AddSample(snapshot);
                }

                return;
            }

            if (_activeRecord is not null)
            {
                AddSample(snapshot);
                FinishRecord(snapshot);
            }
        }
    }

    private void StartRecord(PlcSnapshot snapshot)
    {
        try
        {
            UserRecord? user = _databaseService
                .GetUserByAccountAsync(_session.Account)
                .GetAwaiter()
                .GetResult();

            if (user is null)
            {
                _logger.LogWarning("无法开始作业记录：账户 {Account} 不存在。", _session.Account);
                return;
            }

            _activeRecord = new WorkRecord
            {
                AccountId = user.Id,
                AccountName = user.Account,
                OperatorName = user.DisplayName,
                EquipmentName = "RailWeld",
                RecipeName = string.Empty,
                StartedAt = snapshot.Timestamp,
                ReportNo = $"R{snapshot.Timestamp:yyyyMMddHHmmss}",
                Result = WorkResult.Unknown,
                ResultDetail = "自动记录，待判定。",
                CreatedAt = DateTime.Now,
            };

            ApplyRecipe(_activeRecord);

            // 先落主记录，而不是等作业结束；这样进程异常退出也不会丢掉整条作业。
            _databaseService.AddWorkRecordAsync(_activeRecord).GetAwaiter().GetResult();

            _samples = [];
            _lastSampleAt = snapshot.Timestamp.AddSeconds(-1);
            AddSample(snapshot);

            _logger.LogInformation("开始作业记录：{ReportNo}", _activeRecord.ReportNo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建作业记录失败。");
            _activeRecord = null;
        }
    }

    private void ApplyRecipe(WorkRecord record)
    {
        try
        {
            ParameterSettingRecord? recipe = _databaseService
                .GetParameterSettingRecords()
                .FirstOrDefault();

            if (recipe is null)
                return;

            record.RecipeId = recipe.Id;
            record.RecipeName = recipe.ParameterName;

            AddStage(record, WeldStage.Start, 1, recipe.StartTemp, recipe.StartPower, recipe.StartCoolingMode, recipe.StartCoolingParam);
            AddStage(record, WeldStage.Transition, 2, recipe.TransitionTemp, recipe.TransitionPower, recipe.TransitionCoolingMode, recipe.TransitionCoolingParam);
            AddStage(record, WeldStage.Final, 3, recipe.FinalTemp, recipe.FinalPower, recipe.FinalCoolingMode, recipe.FinalCoolingParam);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取配方快照失败，作业记录将不包含工艺阶段。");
        }
    }

    private static void AddStage(
        WorkRecord record,
        WeldStage stage,
        int sortOrder,
        string temperature,
        string power,
        CoolingModes coolingMode,
        string coolingParameter)
    {
        record.Stages.Add(new WorkStageRecord
        {
            Stage = stage,
            SortOrder = sortOrder,
            Temperature = ParseDouble(temperature),
            Power = ParseDouble(power),
            CoolingMode = coolingMode,
            CoolingParameter = coolingParameter ?? string.Empty,
        });
    }

    private static double ParseDouble(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? result
            : 0;
    }

    private void AddSample(PlcSnapshot snapshot)
    {
        if (_activeRecord is null)
            return;

        _lastSampleAt = snapshot.Timestamp;

        _samples.Add(new WorkSampleRecord
        {
            Timestamp = snapshot.Timestamp,
            ElapsedMs = (int)Math.Max(0, (snapshot.Timestamp - _activeRecord.StartedAt).TotalMilliseconds),
            Temperature1 = snapshot.ActTemp1,
            Temperature2 = snapshot.ActTemp2,
            Temperature3 = snapshot.ActTemp3,
            Temperature4 = snapshot.ActTemp4,
            Pressure = snapshot.ActPressure,
            Flow1 = snapshot.ActFlow1,
            Flow2 = snapshot.ActFlow2,
            Flow3 = snapshot.ActFlow3,
            Voltage = snapshot.Voltage,
            Current = snapshot.Current,
            Power = snapshot.Power,
            Energy = snapshot.Energy,
        });
    }

    private void FinishRecord(PlcSnapshot snapshot)
    {
        WorkRecord? record = _activeRecord;
        List<WorkSampleRecord> samples = _samples;

        _activeRecord = null;
        _samples = [];

        if (record is null)
            return;

        record.FinishedAt = snapshot.Timestamp;

        List<WorkMetricRecord> metrics = [.. BuildMetrics(samples)];

        _ = PersistFinishAsync(record, samples, metrics);
    }

    private async Task PersistFinishAsync(
        WorkRecord record,
        IReadOnlyList<WorkSampleRecord> samples,
        IReadOnlyList<WorkMetricRecord> metrics)
    {
        try
        {
            await _databaseService.AddWorkSamplesAsync(record.Id, samples);
            await _databaseService.AddWorkMetricsAsync(record.Id, metrics);
            await _databaseService.UpdateWorkRecordResultAsync(
                record.Id,
                record.Result,
                record.ResultDetail,
                record.FinishedAt);

            _logger.LogInformation(
                "作业记录完成：{ReportNo}，采样 {Count} 条。",
                record.ReportNo,
                samples.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存作业记录失败：{ReportNo}", record.ReportNo);
        }
    }

    private static IEnumerable<WorkMetricRecord> BuildMetrics(IReadOnlyList<WorkSampleRecord> samples)
    {
        if (samples.Count == 0)
            return [];

        var metrics = new List<WorkMetricRecord>();

        AddMetric(metrics, "温度1", "℃", 1, samples.Select(x => x.Temperature1));
        AddMetric(metrics, "温度2", "℃", 2, samples.Select(x => x.Temperature2));
        AddMetric(metrics, "温度3", "℃", 3, samples.Select(x => x.Temperature3));
        AddMetric(metrics, "温度4", "℃", 4, samples.Select(x => x.Temperature4));
        AddMetric(metrics, "压力", "MPa", 5, samples.Select(x => x.Pressure));
        AddMetric(metrics, "流量1", "L/min", 6, samples.Select(x => x.Flow1));
        AddMetric(metrics, "流量2", "L/min", 7, samples.Select(x => x.Flow2));
        AddMetric(metrics, "流量3", "L/min", 8, samples.Select(x => x.Flow3));
        AddMetric(metrics, "电压", "V", 9, samples.Select(x => x.Voltage));
        AddMetric(metrics, "电流", "A", 10, samples.Select(x => x.Current));
        AddMetric(metrics, "功率", "kW", 11, samples.Select(x => x.Power));

        return metrics;
    }

    private static void AddMetric(
        List<WorkMetricRecord> metrics,
        string name,
        string unit,
        int sortOrder,
        IEnumerable<double> values)
    {
        double[] data = [.. values];

        if (data.Length == 0)
            return;

        metrics.Add(new WorkMetricRecord
        {
            Name = name,
            Unit = unit,
            StartValue = data[0],
            EndValue = data[^1],
            PeakValue = data.Max(),
            MinValue = data.Min(),
            MaxValue = data.Max(),
            AverageValue = data.Average(),
            OverCount = 0,
            PeakRatio = 0,
            Judgement = "—",
            SortOrder = sortOrder,
        });
    }

    public void Dispose()
    {
        _cache.PropertyChanged -= OnCacheChanged;

        if (_session is INotifyPropertyChanged sessionNotify)
            sessionNotify.PropertyChanged -= OnSessionChanged;
    }
}
