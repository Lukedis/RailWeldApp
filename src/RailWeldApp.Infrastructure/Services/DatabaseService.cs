using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;

namespace RailWeldApp.Services
{
    /// <summary>
    /// SQLite 数据访问实现（IDatabaseService）。
    ///
    /// 每次操作创建一个短生命周期 DbContext，适合桌面程序“点一下查一次”的使用方式。
    /// 查询统一使用 AsNoTracking；写操作使用当前上下文跟踪后 SaveChanges。
    /// </summary>
    public sealed class DatabaseService(
        IDbContextFactory<ApplicationDataContext> contextFactory,
        ILogger<DatabaseService> logger)
        : IDatabaseService
    {
        /// <summary>
        /// 启动时执行数据库迁移。
        /// 旧版 EnsureCreated 数据库没有迁移历史，需要先备份并保留旧配方，再重建。
        /// </summary>
        public void ApplyMigrations()
        {
            using ApplicationDataContext context = contextFactory.CreateDbContext();

            string? databasePath = context.Database.GetDbConnection().DataSource;

            bool hasHistory = context.Database.GetAppliedMigrations().Any();

            // 旧版本用 EnsureCreated 建的库没有迁移历史，直接 Migrate 会重复建表。
            // 当前旧库只有 ParameterSetting，这张表的数据需要保留。
            if (!hasHistory && !string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath))
            {
                MigrateLegacyDatabase(context, databasePath);
                return;
            }

            context.Database.Migrate();
        }

        /// <summary>
        /// 旧数据库没有 __EFMigrationsHistory：
        /// 先备份文件，再把 ParameterSetting 读入内存，重建数据库后恢复配方。
        /// </summary>
        private void MigrateLegacyDatabase(ApplicationDataContext context, string databasePath)
        {
            List<ParameterSettingRecord> recipes = [];

            try
            {
                recipes = [.. context.ParameterSettings.AsNoTracking()];
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "读取旧数据库配方失败，将只重建数据库。");
            }

            string backupPath = $"{databasePath}.bak-{DateTime.Now:yyyyMMddHHmmss}";

            File.Copy(databasePath, backupPath, overwrite: false);

            logger.LogWarning("检测到旧数据库，已备份到 {BackupPath}，准备重建并迁移。", backupPath);

            context.Database.EnsureDeleted();

            using ApplicationDataContext migratedContext = contextFactory.CreateDbContext();

            migratedContext.Database.Migrate();

            if (recipes.Count > 0)
            {
                migratedContext.ParameterSettings.AddRange(recipes);
                migratedContext.SaveChanges();
            }

            logger.LogInformation("数据库迁移完成，保留配方 {RecipeCount} 条。", recipes.Count);
        }

        public List<ParameterSettingRecord> GetParameterSettingRecords()
        {
            using ApplicationDataContext dataContext = contextFactory.CreateDbContext();

            return [.. dataContext.ParameterSettings
                .AsNoTracking()
                .OrderBy(x => x.ParameterName)
                .ThenBy(x => x.Id)];
        }

        public void AddParameterSettingRecord(ParameterSettingRecord parameterSetting)
        {
            ArgumentNullException.ThrowIfNull(parameterSetting);

            if (string.IsNullOrWhiteSpace(parameterSetting.Id))
            {
                parameterSetting.Id = Guid.NewGuid().ToString("N");
            }

            using ApplicationDataContext dataContext = contextFactory.CreateDbContext();

            dataContext.ParameterSettings.Add(parameterSetting);

            dataContext.SaveChanges();
        }

        public void DeleteParameterSettingRecord(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            using ApplicationDataContext dataContext = contextFactory.CreateDbContext();

            ParameterSettingRecord? parameterSetting = dataContext.ParameterSettings.Find(id);

            if (parameterSetting is null)
                return;

            dataContext.ParameterSettings.Remove(parameterSetting);

            dataContext.SaveChanges();
        }

        public void UpdateParameterSettingRecord(ParameterSettingRecord parameterSetting)
        {
            ArgumentNullException.ThrowIfNull(parameterSetting);

            using ApplicationDataContext dataContext = contextFactory.CreateDbContext();

            ParameterSettingRecord? existing = dataContext.ParameterSettings.Find(parameterSetting.Id);

            if (existing is null)
            {
                dataContext.ParameterSettings.Add(parameterSetting);
            }
            else
            {
                // 同一条记录一次 SaveChanges 完成更新，不再“先删后增”。
                dataContext.Entry(existing).CurrentValues.SetValues(parameterSetting);
            }

            dataContext.SaveChanges();
        }

        #region 账户

        public async Task<List<UserRecord>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            return await dataContext.Users
                .AsNoTracking()
                .OrderBy(x => x.Account)
                .ToListAsync(cancellationToken);
        }

        public async Task<UserRecord?> GetUserByAccountAsync(string account, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(account);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            string normalized = account.Trim();

            return await dataContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Account == normalized, cancellationToken);
        }

        public async Task AddUserAsync(UserRecord user, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(user.Account);
            ArgumentException.ThrowIfNullOrWhiteSpace(user.PasswordHash);

            if (string.IsNullOrWhiteSpace(user.Id))
                user.Id = Guid.NewGuid().ToString("N");

            if (user.CreatedAt == default)
                user.CreatedAt = DateTime.Now;

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            dataContext.Users.Add(user);

            await dataContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> UpdateUserAsync(UserRecord user, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(user.Id);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            UserRecord? existing = await dataContext.Users
                .FirstOrDefaultAsync(x => x.Id == user.Id, cancellationToken);

            if (existing is null)
                return false;

            dataContext.Entry(existing).CurrentValues.SetValues(user);

            await dataContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> UpdateUserLastLoginAsync(string userId, DateTime loginAt, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            UserRecord? user = await dataContext.Users
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
                return false;

            user.LastLoginAt = loginAt;

            await dataContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task AddLoginRecordAsync(LoginRecord record, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentException.ThrowIfNullOrWhiteSpace(record.Account);

            if (record.OccurredAt == default)
                record.OccurredAt = DateTime.Now;

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            dataContext.LoginRecords.Add(record);

            await dataContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<LoginRecord>> GetLoginRecordsAsync(DateTime from, DateTime to, int take = 500, CancellationToken cancellationToken = default)
        {
            EnsureRange(from, to);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            return await dataContext.LoginRecords
                .AsNoTracking()
                .Where(x => x.OccurredAt >= from && x.OccurredAt < to)
                .OrderByDescending(x => x.OccurredAt)
                .Take(ClampTake(take))
                .ToListAsync(cancellationToken);
        }

        #endregion 账户

        #region 作业记录

        public async Task AddWorkRecordAsync(WorkRecord record, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentException.ThrowIfNullOrWhiteSpace(record.AccountId);

            if (string.IsNullOrWhiteSpace(record.Id))
                record.Id = Guid.NewGuid().ToString("N");

            if (record.StartedAt == default)
                record.StartedAt = DateTime.Now;

            if (record.CreatedAt == default)
                record.CreatedAt = DateTime.Now;

            foreach (WorkSampleRecord sample in record.Samples)
                sample.WorkRecordId = record.Id;

            foreach (WorkMetricRecord metric in record.Metrics)
                metric.WorkRecordId = record.Id;

            foreach (WorkStageRecord stage in record.Stages)
                stage.WorkRecordId = record.Id;

            foreach (AlarmRecord alarm in record.Alarms)
                alarm.WorkRecordId = record.Id;

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            dataContext.WorkRecords.Add(record);

            await dataContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> AddWorkMetricsAsync(
            string workRecordId,
            IEnumerable<WorkMetricRecord> metrics,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(workRecordId);
            ArgumentNullException.ThrowIfNull(metrics);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            bool exists = await dataContext.WorkRecords
                .AnyAsync(x => x.Id == workRecordId, cancellationToken);

            if (!exists)
                throw new InvalidOperationException($"作业记录 {workRecordId} 不存在。");

            List<WorkMetricRecord> list = [.. metrics];

            foreach (WorkMetricRecord metric in list)
            {
                metric.WorkRecordId = workRecordId;
                metric.WorkRecord = null;
            }

            if (list.Count == 0)
                return 0;

            dataContext.WorkMetrics.AddRange(list);

            await dataContext.SaveChangesAsync(cancellationToken);

            return list.Count;
        }

        public async Task<bool> UpdateWorkRecordResultAsync(
            string workRecordId,
            WorkResult result,
            string resultDetail,
            DateTime? finishedAt = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(workRecordId);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            WorkRecord? record = await dataContext.WorkRecords
                .FirstOrDefaultAsync(x => x.Id == workRecordId, cancellationToken);

            if (record is null)
                return false;

            record.Result = result;
            record.ResultDetail = resultDetail ?? string.Empty;
            record.FinishedAt = finishedAt ?? DateTime.Now;

            await dataContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<List<WorkRecord>> GetWorkRecordsAsync(
            DateTime from,
            DateTime to,
            string? account = null,
            int take = 500,
            CancellationToken cancellationToken = default)
        {
            EnsureRange(from, to);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            IQueryable<WorkRecord> query = dataContext.WorkRecords
                .AsNoTracking()
                .Include(x => x.Metrics)
                .Include(x => x.Stages)
                .Where(x => x.StartedAt >= from && x.StartedAt < to);

            if (!string.IsNullOrWhiteSpace(account))
            {
                string normalized = account.Trim();
                query = query.Where(x => x.AccountName == normalized);
            }

            return await query
                .AsSplitQuery()
                .OrderByDescending(x => x.StartedAt)
                .Take(ClampTake(take))
                .ToListAsync(cancellationToken);
        }

        public async Task<WorkRecord?> GetWorkRecordAsync(
            string workRecordId,
            bool includeSamples = true,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(workRecordId);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            IQueryable<WorkRecord> query = dataContext.WorkRecords
                .AsNoTracking()
                .Include(x => x.Metrics)
                .Include(x => x.Stages)
                .Include(x => x.Alarms);

            if (includeSamples)
                query = query.Include(x => x.Samples);

            return await query
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == workRecordId, cancellationToken);
        }

        public async Task<List<WorkSampleRecord>> GetWorkSamplesAsync(
            string workRecordId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(workRecordId);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            return await dataContext.WorkSamples
                .AsNoTracking()
                .Where(x => x.WorkRecordId == workRecordId)
                .OrderBy(x => x.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> AddWorkSamplesAsync(
            string workRecordId,
            IEnumerable<WorkSampleRecord> samples,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(workRecordId);
            ArgumentNullException.ThrowIfNull(samples);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            bool exists = await dataContext.WorkRecords
                .AnyAsync(x => x.Id == workRecordId, cancellationToken);

            if (!exists)
                throw new InvalidOperationException($"作业记录 {workRecordId} 不存在。");

            List<WorkSampleRecord> list = [.. samples];

            foreach (WorkSampleRecord sample in list)
            {
                sample.WorkRecordId = workRecordId;
                sample.WorkRecord = null;
            }

            if (list.Count == 0)
                return 0;

            dataContext.WorkSamples.AddRange(list);

            await dataContext.SaveChangesAsync(cancellationToken);

            return list.Count;
        }

        #endregion 作业记录

        #region 报警

        public async Task AddAlarmAsync(AlarmRecord alarm, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(alarm);
            ArgumentException.ThrowIfNullOrWhiteSpace(alarm.Code);

            if (alarm.OccurredAt == default)
                alarm.OccurredAt = DateTime.Now;

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            dataContext.Alarms.Add(alarm);

            await dataContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<AlarmRecord>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default)
        {
            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            return await dataContext.Alarms
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.OccurredAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AlarmRecord>> GetAlarmsAsync(
            DateTime from,
            DateTime to,
            int take = 500,
            CancellationToken cancellationToken = default)
        {
            EnsureRange(from, to);

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            return await dataContext.Alarms
                .AsNoTracking()
                .Where(x => x.OccurredAt >= from && x.OccurredAt < to)
                .OrderByDescending(x => x.OccurredAt)
                .Take(ClampTake(take))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> RecoverAlarmAsync(
            long alarmId,
            DateTime recoveredAt,
            CancellationToken cancellationToken = default)
        {
            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            AlarmRecord? alarm = await dataContext.Alarms
                .FirstOrDefaultAsync(x => x.Id == alarmId, cancellationToken);

            if (alarm is null)
                return false;

            alarm.IsActive = false;
            alarm.RecoveredAt = recoveredAt;

            await dataContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<int> AcknowledgeAlarmsAsync(
            IEnumerable<long> alarmIds,
            string? account,
            string? userId,
            DateTime acknowledgedAt,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(alarmIds);

            long[] ids = [.. alarmIds.Distinct()];

            if (ids.Length == 0)
                return 0;

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            List<AlarmRecord> alarms = await dataContext.Alarms
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            foreach (AlarmRecord alarm in alarms)
            {
                alarm.IsAcknowledged = true;
                alarm.AcknowledgedAt = acknowledgedAt;
                alarm.AcknowledgedByAccount = account;
                alarm.AcknowledgedByUserId = userId;
            }

            await dataContext.SaveChangesAsync(cancellationToken);

            return alarms.Count;
        }

        public async Task<int> AcknowledgeAllActiveAlarmsAsync(
            string? account,
            string? userId,
            DateTime acknowledgedAt,
            CancellationToken cancellationToken = default)
        {
            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            List<AlarmRecord> alarms = await dataContext.Alarms
                .Where(x => x.IsActive && !x.IsAcknowledged)
                .ToListAsync(cancellationToken);

            foreach (AlarmRecord alarm in alarms)
            {
                alarm.IsAcknowledged = true;
                alarm.AcknowledgedAt = acknowledgedAt;
                alarm.AcknowledgedByAccount = account;
                alarm.AcknowledgedByUserId = userId;
            }

            await dataContext.SaveChangesAsync(cancellationToken);

            return alarms.Count;
        }

        #endregion 报警

        #region 通信错误

        public async Task AddCommunicationErrorAsync(
            CommunicationErrorRecord error,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(error);
            ArgumentException.ThrowIfNullOrWhiteSpace(error.Message);

            if (error.OccurredAt == default)
                error.OccurredAt = DateTime.Now;

            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            dataContext.CommunicationErrors.Add(error);

            await dataContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<CommunicationErrorRecord>> GetCommunicationErrorsAsync(
            int take = 200,
            CancellationToken cancellationToken = default)
        {
            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            return await dataContext.CommunicationErrors
                .AsNoTracking()
                .OrderByDescending(x => x.OccurredAt)
                .Take(ClampTake(take))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> RecoverCommunicationErrorAsync(
            long errorId,
            DateTime recoveredAt,
            CancellationToken cancellationToken = default)
        {
            await using ApplicationDataContext dataContext = await contextFactory
                .CreateDbContextAsync(cancellationToken);

            CommunicationErrorRecord? error = await dataContext.CommunicationErrors
                .FirstOrDefaultAsync(x => x.Id == errorId, cancellationToken);

            if (error is null)
                return false;

            error.IsRecovered = true;
            error.RecoveredAt = recoveredAt;

            await dataContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        #endregion 通信错误

        private static void EnsureRange(DateTime from, DateTime to)
        {
            if (from > to)
                throw new ArgumentException("开始时间不能晚于结束时间。");
        }

        private static int ClampTake(int take) => Math.Clamp(take, 1, 5000);
    }
}