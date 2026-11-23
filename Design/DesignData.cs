using Microsoft.Extensions.DependencyInjection;
using RailWeldApp.DependencyInjection;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Design
{
    public static class DesignData
    {
        private static readonly IServiceProvider _serviceProvider;

        static DesignData()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCommandService();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public static MainWindowViewModel MainWindowViewModel => _serviceProvider.GetRequiredService<MainWindowViewModel>();
        public static LoginViewModel LoginViewModel => _serviceProvider.GetRequiredService<LoginViewModel>();
        public static AdminViewModel AdminViewModel => _serviceProvider.GetRequiredService<AdminViewModel>();
        public static AdminDashboardViewModel AdminDashboardViewModel => _serviceProvider.GetRequiredService<AdminDashboardViewModel>();
        public static AdminSettingsViewModel AdminSettingsViewModel => _serviceProvider.GetRequiredService<AdminSettingsViewModel>();
        public static AdminRealTimeDataViewModel AdminRealTimeDataViewModel => _serviceProvider.GetRequiredService<AdminRealTimeDataViewModel>();
        public static AdminManualControlViewModel AdminManualControlViewModel => _serviceProvider.GetRequiredService<AdminManualControlViewModel>();
        public static AdminAutoControlViewModel AdminAutoControlViewModel => _serviceProvider.GetRequiredService<AdminAutoControlViewModel>();
        public static AdminTempMonitorViewModel AdminTempMonitorViewModel => _serviceProvider.GetRequiredService<AdminTempMonitorViewModel>();
        public static AdminHistoryQueryViewModel AdminHistoryQueryViewModel => _serviceProvider.GetRequiredService<AdminHistoryQueryViewModel>();
        public static AdminFaultAlarmViewModel AdminFaultAlarmViewModel => _serviceProvider.GetRequiredService<AdminFaultAlarmViewModel>();
        public static AdminReportExportViewModel AdminReportExportViewModel => _serviceProvider.GetRequiredService<AdminReportExportViewModel>();
    }
}