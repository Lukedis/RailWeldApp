using Microsoft.EntityFrameworkCore;

namespace RailWeldApp.DataModels
{
    public class ApplicationDataContext(DbContextOptions<ApplicationDataContext> options) : DbContext(options)
    {
        public DbSet<ParameterSettingRecord> ParameterSettings => Set<ParameterSettingRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ParameterSettingRecord>()
                .ToTable("ParameterSetting")
                .HasKey(x => x.Id);
        }
    }
}
