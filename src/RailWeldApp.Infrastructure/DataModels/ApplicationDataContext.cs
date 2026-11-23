using Microsoft.EntityFrameworkCore;

namespace RailWeldApp.DataModels
{
    public class ApplicationDataContext(DbContextOptions<ApplicationDataContext> options) : DbContext(options)
    {
        public DbSet<ParameterSettingRecord> ParameterSettings => Set<ParameterSettingRecord>();

        public DbSet<UserRecord> Users => Set<UserRecord>();

        public DbSet<WorkRecord> WorkRecords => Set<WorkRecord>();

        public DbSet<WorkSampleRecord> WorkSamples => Set<WorkSampleRecord>();

        public DbSet<WorkMetricRecord> WorkMetrics => Set<WorkMetricRecord>();

        public DbSet<WorkStageRecord> WorkStages => Set<WorkStageRecord>();

        public DbSet<AlarmRecord> Alarms => Set<AlarmRecord>();

        public DbSet<CommunicationErrorRecord> CommunicationErrors => Set<CommunicationErrorRecord>();

        public DbSet<LoginRecord> LoginRecords => Set<LoginRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ParameterSettingRecord>(entity =>
            {
                entity.ToTable("ParameterSetting");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasMaxLength(32);
                entity.Property(x => x.ParameterName).HasMaxLength(128).IsRequired();
            });

            modelBuilder.Entity<UserRecord>(entity =>
            {
                entity.ToTable("UserAccount");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Account).IsUnique();
                entity.Property(x => x.Id).HasMaxLength(32);
                entity.Property(x => x.Account).HasMaxLength(64).IsRequired();
                entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
                entity.Property(x => x.PasswordSalt).HasMaxLength(128).IsRequired();
                entity.Property(x => x.DisplayName).HasMaxLength(64);
                entity.Property(x => x.Role).HasConversion<int>();
            });

            modelBuilder.Entity<WorkRecord>(entity =>
            {
                entity.ToTable("WorkRecord");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.AccountId);
                entity.HasIndex(x => x.StartedAt);
                entity.HasIndex(x => x.ReportNo);
                entity.Property(x => x.Id).HasMaxLength(32);
                entity.Property(x => x.AccountId).HasMaxLength(32).IsRequired();
                entity.Property(x => x.AccountName).HasMaxLength(64).IsRequired();
                entity.Property(x => x.RecipeId).HasMaxLength(32);
                entity.Property(x => x.RecipeName).HasMaxLength(128);
                entity.Property(x => x.JobNumber).HasMaxLength(64);
                entity.Property(x => x.OperatorName).HasMaxLength(64);
                entity.Property(x => x.ShiftName).HasMaxLength(64);
                entity.Property(x => x.EquipmentName).HasMaxLength(128);
                entity.Property(x => x.ReportNo).HasMaxLength(64);
                entity.Property(x => x.Result).HasConversion<int>();
                entity.Property(x => x.ResultDetail).HasMaxLength(512);
                entity.Property(x => x.Remark).HasMaxLength(512);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.WorkRecords)
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Recipe)
                    .WithMany()
                    .HasForeignKey(x => x.RecipeId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(x => x.Samples)
                    .WithOne(x => x.WorkRecord)
                    .HasForeignKey(x => x.WorkRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Metrics)
                    .WithOne(x => x.WorkRecord)
                    .HasForeignKey(x => x.WorkRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Stages)
                    .WithOne(x => x.WorkRecord)
                    .HasForeignKey(x => x.WorkRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Alarms)
                    .WithOne(x => x.WorkRecord)
                    .HasForeignKey(x => x.WorkRecordId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<WorkSampleRecord>(entity =>
            {
                entity.ToTable("WorkSample");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.WorkRecordId, x.Timestamp });
                entity.Property(x => x.WorkRecordId).HasMaxLength(32).IsRequired();
            });

            modelBuilder.Entity<WorkMetricRecord>(entity =>
            {
                entity.ToTable("WorkMetric");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.WorkRecordId, x.SortOrder });
                entity.Property(x => x.WorkRecordId).HasMaxLength(32).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Unit).HasMaxLength(16);
                entity.Property(x => x.Judgement).HasMaxLength(64);
            });

            modelBuilder.Entity<WorkStageRecord>(entity =>
            {
                entity.ToTable("WorkStage");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.WorkRecordId, x.SortOrder });
                entity.Property(x => x.WorkRecordId).HasMaxLength(32).IsRequired();
                entity.Property(x => x.Stage).HasConversion<int>();
                entity.Property(x => x.CoolingMode).HasConversion<int>();
                entity.Property(x => x.CoolingParameter).HasMaxLength(64);
            });

            modelBuilder.Entity<AlarmRecord>(entity =>
            {
                entity.ToTable("Alarm");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.OccurredAt);
                entity.HasIndex(x => x.Code);
                entity.HasIndex(x => x.IsActive);
                entity.Property(x => x.WorkRecordId).HasMaxLength(32);
                entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Message).HasMaxLength(512).IsRequired();
                entity.Property(x => x.Level).HasConversion<int>();
                entity.Property(x => x.AcknowledgedByAccount).HasMaxLength(64);
                entity.Property(x => x.AcknowledgedByUserId).HasMaxLength(32);
            });

            modelBuilder.Entity<CommunicationErrorRecord>(entity =>
            {
                entity.ToTable("CommunicationError");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.OccurredAt);
                entity.Property(x => x.CommunicationType).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Message).HasMaxLength(512).IsRequired();
                entity.Property(x => x.Detail).HasMaxLength(2048);
            });

            modelBuilder.Entity<LoginRecord>(entity =>
            {
                entity.ToTable("LoginRecord");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.OccurredAt);
                entity.HasIndex(x => x.Account);
                entity.Property(x => x.UserId).HasMaxLength(32);
                entity.Property(x => x.Account).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Message).HasMaxLength(256);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.LoginRecords)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
