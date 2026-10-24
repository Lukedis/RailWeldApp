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
        // 桌面 HMI 的参数只在启动时读一次；关掉文件监听，省掉一个 FileSystemWatcher 和配置重载路径。
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        services.AddSingleton(configuration);

        // 2. 数据库：SQLite 文件，路径来自配置
        // 用 DbContextFactory 而不是 AddDbContext：每次用时 new 一个短生命周期上下文，
        // 适合 WPF 这种"操作零散、不常驻"的界面程序。
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
        services.AddOptions<PlcOptions>().BindConfiguration("PLC");
        services.AddSingleton<PlcDataCache>();
        services.AddSingleton<IPlcService, PlcService>();
        services.AddSingleton<IPlcPollingBackgroundService, PlcPollingBackgroundService>();

        // 6. Modbus RTU（串口）
        services.AddOptions<ModbusRtuOptions>().BindConfiguration("ModbusRTU");
        services.AddSingleton<IModbusRtuService, ModbusRtuService>();

        // 对话框服务
        services.AddSingleton<IDialogService, DialogService>();

        // 7. 导航：窗口级 + 管理台内部级
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAdminNavigationService, AdminNavigationService>();

        // 8. 窗口（一个进程只有一份）
        // 视图不注册进 DI：MainWindow/AdminView/各页面 View 都由 ViewTemplates.xaml 的 DataTemplate 创建。
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<AdminViewModel>();

        // 9. 页面 VM（Transient：每次进入都是干净的新状态）
        // 新增页面时：在 ViewModels 加 VM、在 Views 加 View，并在 Themes/ViewTemplates.xaml 补一行 DataTemplate。
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
    }
}
