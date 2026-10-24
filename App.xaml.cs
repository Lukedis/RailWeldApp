using System.Diagnostics;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RailWeldApp.DependencyInjection;
using RailWeldApp.Interfaces;
using RailWeldApp.Services;
using RailWeldApp.Views;

namespace RailWeldApp
{
    public partial class App : Application
    {
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

            try
            {
                // 只初始化一次：建库/建表/迁移
                Services.GetRequiredService<DatabaseService>().ApplyMigrations();
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

        protected override void OnExit(ExitEventArgs e)
        {
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
