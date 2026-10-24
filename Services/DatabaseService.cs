using Microsoft.EntityFrameworkCore;
using RailWeldApp.DataModels;

namespace RailWeldApp.Services
{
    public sealed class DatabaseService(IDbContextFactory<ApplicationDataContext> contextFactory)
    {
        public void ApplyMigrations()
        {
            using ApplicationDataContext context = contextFactory.CreateDbContext();

            // 注意：EnsureCreated 只在数据库不存在时建表。
            // 已有库新增 DbSet/表时它不会补表，正式版应改用 EF Migrate。
            context.Database.EnsureCreated();
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
    }
}
