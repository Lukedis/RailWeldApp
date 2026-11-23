using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailWeldApp.DataModels;
using RailWeldApp.DependencyInjection;
using RailWeldApp.Interfaces;
using RailWeldApp.Options;
using RailWeldApp.Services;
using RailWeldApp.Views;

namespace RailWeldApp
{
    /// <summary>
    /// 程序入口和组合根。
    /// 负责创建依赖注入容器，并按顺序初始化数据库、账户和后台服务。
    /// </summary>
    public partial class App : Application
    {
        private ILogger<App>? _logger;

        public IServiceProvider Services { get; }

        public App()
        {
            // 添加依赖注入
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCommandService();

            Services = serviceCollection.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _logger = Services.GetService<ILogger<App>>();

            // 三个入口分别覆盖：UI 线程、进程级、后台 Task。
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            try
            {
                // 1. 数据库迁移只做一次；旧版 EnsureCreated 库会在服务内部备份并升级。
                DatabaseService databaseService = Services.GetRequiredService<DatabaseService>();
                databaseService.ApplyMigrations();

                // 2. 首次启动没有账户时创建配置中的管理员。
                EnsureBootstrapAdmin(databaseService);

                // 3. 单例监听 PlcDataCache，作业开始/结束时自动写 WorkRecord。
                _ = Services.GetRequiredService<WorkRecordRecorder>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"数据库初始化失败：{ex.Message}",
                    "RailWeld",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);

                return;
            }

            // 绑定诊断：WPF 默认会把每条绑定失败格式化后写到 Trace / VS 输出。
            // 当前页面大量使用占位 VM，一次切页会有上百条失败；在 Debug + VS 下，
            // 这些字符串格式化的开销很可观，而且输出窗口会被刷满。
            // 日常运行直接关掉；排查绑定问题时把下面一行改成 SourceLevels.Warning 即可。
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;

            // 音效预加载放后台：解码 WAV 不该占启动关键路径
            ISoundService? soundService = Services.GetService<ISoundService>();

            if (soundService is not null)
            {
                _ = Task.Run(() =>
                {
                    soundService.Preload("Popup");
                    soundService.Preload("Success");
                    soundService.Preload("Failure");
                });
            }

            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();

            mainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "WPF UI 未处理异常。");

            MessageBox.Show(
                $"程序发生异常：\n{e.Exception.Message}",
                "RailWeld",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // HMI 保持运行，避免单个界面异常直接退出整个采集程序。
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger?.LogCritical(e.ExceptionObject as Exception, "应用域未处理异常。");
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "未观测的任务异常。");
            e.SetObserved();
        }

        /// <summary>
        /// 只在数据库没有任何账户时创建管理员，避免覆盖现场已有账户。
        /// </summary>
        private void EnsureBootstrapAdmin(DatabaseService databaseService)
        {
            BootstrapAdminOptions options = Services
                .GetRequiredService<IOptions<BootstrapAdminOptions>>()
                .Value;

            if (!options.Enabled || string.IsNullOrWhiteSpace(options.Account) || string.IsNullOrWhiteSpace(options.Password))
                return;

            List<UserRecord> users = databaseService.GetUsersAsync().GetAwaiter().GetResult();

            if (users.Count > 0)
                return;

            IPasswordService passwordService = Services.GetRequiredService<IPasswordService>();

            string salt = passwordService.CreateSalt();
            string hash = passwordService.HashPassword(options.Password, salt);

            var admin = new UserRecord
            {
                Account = options.Account.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? options.Account.Trim() : options.DisplayName.Trim(),
                Role = RailWeldApp.Enums.OperatorRoles.Admin,
                IsEnabled = true,
                CreatedAt = DateTime.Now,
            };

            databaseService.AddUserAsync(admin).GetAwaiter().GetResult();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DispatcherUnhandledException -= OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            try
            {
                if (Services is IAsyncDisposable asyncDisposable)
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                else if (Services is IDisposable disposable)
                    disposable.Dispose();
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}
