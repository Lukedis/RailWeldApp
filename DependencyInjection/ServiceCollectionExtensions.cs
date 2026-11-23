using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Interfaces;
using RailWeldApp.Models;
using RailWeldApp.Options;
using RailWeldApp.Services;
using RailWeldApp.Views;
using RailWeldApp.ViewModels;
using RailWeldApp.Extensions;

namespace RailWeldApp.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddCommandService(this IServiceCollection services)
    {
        // 1. 配置：读取输出目录下的 appsettings.json
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        services.AddSingleton(configuration);

        // 2. 数据库：SQLite 文件，路径来自配置
        // 用 DbContextFactory 而不是 AddDbContext：每次用时 new 一个短生命周期上下文，
        var databasePath = configuration["Database:Path"] ?? throw new InvalidOperationException("appsettings.json 里缺少 Database:Path");

        databasePath = Environment.ExpandEnvironmentVariables(databasePath);

        var databaseDirectory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrEmpty(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        // 数据库上下文
        services.AddDbContextFactory<ApplicationDataContext>(options => options.UseSqlite($"Data Source={databasePath}"));

        // 3. 日志：Serilog 写文件（配置见 appsettings.json 的 Serilog 节）
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddFileLogger(configuration);
        });

        // 4. 音效：界面提示音，单例缓存已解码的播放器
        services.AddSingleton<ISoundService, SoundService>();

        // 5. PLC（S7）：连接服务 + 采集缓存 + 后台轮询任务
        services.AddSingleton<PlcDataCache>();
        services.AddSingleton<IPlcService, PlcService>();
        services.AddSingleton<IProcessParameterService, ProcessParameterService>();
        services.AddSingleton<IPlcPollingBackgroundService, PlcPollingBackgroundService>();
        services.AddSingleton<WorkRecordRecorder>();

        // 选项
        services.AddOptions<PlcConfigurationOptions>().BindConfiguration("PLC");
        services.AddOptions<ModbusRtuConfigurationOptions>().BindConfiguration("ModbusRTU");
        services.AddOptions<RealtimeDataOptions>().BindConfiguration(RealtimeDataOptions.SectionName);
        services.AddOptions<ManualControlPointConfigurationOptions>().BindConfiguration(ManualControlPointConfigurationOptions.SectionName);
        services.AddOptions<BootstrapAdminOptions>().BindConfiguration(BootstrapAdminOptions.SectionName);
        services.AddOptions<ProcessParametersConfigurationOptions>().BindConfiguration("ProcessParametersAddress");

        // 6. Modbus RTU（串口）
        services.AddSingleton<IModbusRtuService, ModbusRtuService>();

        // 对话框服务
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IPasswordService, PasswordService>();

        // 7. 导航：窗口级 + 管理台内部级
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAdminNavigationService, AdminNavigationService>();

        // 8. 窗口
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<AdminViewModel>();

        // 9. 页面 VM
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AdminDashboardViewModel>();
        services.AddTransient<AdminSettingsViewModel>();
        services.AddTransient<AdminRealTimeDataViewModel>();
        services.AddTransient<AdminManualControlViewModel>();
        services.AddTransient<AdminAutoControlViewModel>();
        services.AddTransient<AdminTempMonitorViewModel>();
        services.AddTransient<AdminHistoryQueryViewModel>();
        services.AddTransient<AdminFaultAlarmViewModel>();
        services.AddTransient<AdminReportExportViewModel>();

        // 页面工厂：从 DI 解析 Transient 页面 VM，导航服务只依赖它
        services.AddSingleton<PageFactory>();

        // 数据库服务
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IDatabaseService>(provider => provider.GetRequiredService<DatabaseService>());
    }
}