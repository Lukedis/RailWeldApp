using CommunityToolkit.Mvvm.Input;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;

namespace RailWeldApp.ViewModels
{
    /// <summary>
    /// 首页（总览）：产品介绍页，只负责把用户带到具体功能页。
    /// </summary>
    public partial class AdminDashboardViewModel(IAdminNavigationService adminNavigationService)
        : PageViewModel(PageName.AdminDashboard)
    {
        [RelayCommand]
        private void OpenRealTimeData() => adminNavigationService.NavigateTo<AdminRealTimeDataViewModel>();

        [RelayCommand]
        private void OpenTempMonitor() => adminNavigationService.NavigateTo<AdminTempMonitorViewModel>();
    }
}
