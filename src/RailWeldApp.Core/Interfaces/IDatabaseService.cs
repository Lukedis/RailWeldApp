using RailWeldApp.DataModels;
using RailWeldApp.Enums;

namespace RailWeldApp.Interfaces;

/// <summary>
/// 数据访问口。ViewModel/后台服务只依赖此接口，具体实现位于 Services/DatabaseService。
/// </summary>
public interface IDatabaseService
{
    List<ParameterSettingRecord> GetParameterSettingRecords();

    void AddParameterSettingRecord(ParameterSettingRecord parameterSetting);

    void DeleteParameterSettingRecord(string id);

    void UpdateParameterSettingRecord(ParameterSettingRecord parameterSetting);

    Task<List<UserRecord>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserRecord?> GetUserByAccountAsync(string account, CancellationToken cancellationToken = default);

    Task AddUserAsync(UserRecord user, CancellationToken cancellationToken = default);

    Task<bool> UpdateUserAsync(UserRecord user, CancellationToken cancellationToken = default);

    Task<bool> UpdateUserLastLoginAsync(string userId, DateTime loginAt, CancellationToken cancellationToken = default);

    Task AddLoginRecordAsync(LoginRecord record, CancellationToken cancellationToken = default);

    Task<List<LoginRecord>> GetLoginRecordsAsync(
        DateTime from,
        DateTime to,
        int take = 500,
        CancellationToken cancellationToken = default);

    Task AddWorkRecordAsync(WorkRecord record, CancellationToken cancellationToken = default);

    Task<int> AddWorkMetricsAsync(
        string workRecordId,
        IEnumerable<WorkMetricRecord> metrics,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateWorkRecordResultAsync(
        string workRecordId,
        WorkResult result,
        string resultDetail,
        DateTime? finishedAt = null,
        CancellationToken cancellationToken = default);

    Task<List<WorkRecord>> GetWorkRecordsAsync(
        DateTime from,
        DateTime to,
        string? account = null,
        int take = 500,
        CancellationToken cancellationToken = default);

    Task<WorkRecord?> GetWorkRecordAsync(
        string workRecordId,
        bool includeSamples = true,
        CancellationToken cancellationToken = default);

    Task<List<WorkSampleRecord>> GetWorkSamplesAsync(
        string workRecordId,
        CancellationToken cancellationToken = default);

    Task<int> AddWorkSamplesAsync(
        string workRecordId,
        IEnumerable<WorkSampleRecord> samples,
        CancellationToken cancellationToken = default);

    Task AddAlarmAsync(AlarmRecord alarm, CancellationToken cancellationToken = default);

    Task<List<AlarmRecord>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);

    Task<List<AlarmRecord>> GetAlarmsAsync(
        DateTime from,
        DateTime to,
        int take = 500,
        CancellationToken cancellationToken = default);

    Task<bool> RecoverAlarmAsync(
        long alarmId,
        DateTime recoveredAt,
        CancellationToken cancellationToken = default);

    Task<int> AcknowledgeAlarmsAsync(
        IEnumerable<long> alarmIds,
        string? account,
        string? userId,
        DateTime acknowledgedAt,
        CancellationToken cancellationToken = default);

    Task<int> AcknowledgeAllActiveAlarmsAsync(
        string? account,
        string? userId,
        DateTime acknowledgedAt,
        CancellationToken cancellationToken = default);

    Task AddCommunicationErrorAsync(
        CommunicationErrorRecord error,
        CancellationToken cancellationToken = default);

    Task<List<CommunicationErrorRecord>> GetCommunicationErrorsAsync(
        int take = 200,
        CancellationToken cancellationToken = default);

    Task<bool> RecoverCommunicationErrorAsync(
        long errorId,
        DateTime recoveredAt,
        CancellationToken cancellationToken = default);
}
