using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;

namespace RailWeldApp.ViewModels;

public partial class AdminViewModel : PageViewModel, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IAdminNavigationService _adminNavigationService;

    public AdminViewModel(INavigationService navigationService, IAdminNavigationService adminNavigationService)
        : base(PageName.Admin)
    {
        _navigationService = navigationService;
        _adminNavigationService = adminNavigationService;

        // 用弱事件：AdminViewModel 是单例，即便以后改成 Transient 也不会被服务反向留住。
        SubscribeWeak<INotifyPropertyChanged, PropertyChangedEventArgs>(
            _adminNavigationService,
            nameof(INotifyPropertyChanged.PropertyChanged),
            OnAdminNavigationChanged);
    }

    /// <summary>
    ///  进入AdminViewModel页面时，要做的事情
    /// </summary>
    public void OnNavigatedTo()
    {
        IsDrawerOpen = false;
        _adminNavigationService.NavigateTo<AdminDashboardViewModel>();
    }

    /// <summary>
    ///  离开AdminViewModel页面时，要做到事情
    /// </summary>
    public void OnNavigatedFrom()
    {
        IsDrawerOpen = false;
    }

    public ViewModelBase? CurrentPage => _adminNavigationService.CurrentView;

    public bool IsActiveAdminDashboard => IsActivePage(PageName.AdminDashboard);

    public bool IsActiveAdminSettings => IsActivePage(PageName.AdminSettings);

    public bool IsActiveAdminRealTimeData => IsActivePage(PageName.AdminRealTimeData);

    public bool IsActiveAdminManualControl => IsActivePage(PageName.AdminManualControl);

    public bool IsActiveAdminAutoControl => IsActivePage(PageName.AdminAutoControl);

    public bool IsActiveAdminTempMonitor => IsActivePage(PageName.AdminTempMonitor);

    public bool IsActiveAdminHistoryQuery => IsActivePage(PageName.AdminHistoryQuery);

    public bool IsActiveAdminFaultAlarm => IsActivePage(PageName.AdminFaultAlarm);

    public bool IsActiveAdminReportExport => IsActivePage(PageName.AdminReportExport);

    private bool IsActivePage(PageName page)
    {
        if (_adminNavigationService.CurrentView is not PageViewModel current)
            return false;

        return current.PageName == page;
    }

    private void OnAdminNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IAdminNavigationService.CurrentView))
            return;

        // 通知属性发生改变
        OnPropertyChanged(nameof(CurrentPage));

        OnPropertyChanged(nameof(IsActiveAdminDashboard));
        OnPropertyChanged(nameof(IsActiveAdminSettings));
        OnPropertyChanged(nameof(IsActiveAdminRealTimeData));
        OnPropertyChanged(nameof(IsActiveAdminManualControl));
        OnPropertyChanged(nameof(IsActiveAdminAutoControl));
        OnPropertyChanged(nameof(IsActiveAdminTempMonitor));
        OnPropertyChanged(nameof(IsActiveAdminHistoryQuery));
        OnPropertyChanged(nameof(IsActiveAdminFaultAlarm));
        OnPropertyChanged(nameof(IsActiveAdminReportExport));
    }

    [ObservableProperty]
    private bool _isDrawerOpen;

    [ObservableProperty]
    private int _numberCount = 3;

    [ObservableProperty]
    private int _alarmCount;

    /// <summary>
    /// 通知抽屉（铃铛）。
    /// </summary>
    [RelayCommand]
    private void OpenDrawer()
    {
        IsDrawerOpen = true;
        NumberCount = 0;
    }

    [RelayCommand]
    private void CloseDrawer() => IsDrawerOpen = false;

    // 开工信息（生产线 / 班组 / 起始接头号）现在在登录页选，管理台不再弹"开工设置"。
    // 需要重新弹窗时，用 Dialog 属性调 ShowDialogAsync。

    [RelayCommand]
    private void NavigationToAdminDashboard() => _adminNavigationService.NavigateTo<AdminDashboardViewModel>();

    [RelayCommand]
    private void NavigationToAdminSettings() => _adminNavigationService.NavigateTo<AdminSettingsViewModel>();

    [RelayCommand]
    private void NavigationToAdminRealTimeData() => _adminNavigationService.NavigateTo<AdminRealTimeDataViewModel>();

    [RelayCommand]
    private void NavigationToAdminManualControl() => _adminNavigationService.NavigateTo<AdminManualControlViewModel>();

    [RelayCommand]
    private void NavigationToAdminAutoControl() => _adminNavigationService.NavigateTo<AdminAutoControlViewModel>();

    [RelayCommand]
    private void NavigationToAdminTempMonitor() => _adminNavigationService.NavigateTo<AdminTempMonitorViewModel>();

    [RelayCommand]
    private void NavigationToAdminHistoryQuery() => _adminNavigationService.NavigateTo<AdminHistoryQueryViewModel>();

    [RelayCommand]
    private void NavigationToAdminFaultAlarm() => _adminNavigationService.NavigateTo<AdminFaultAlarmViewModel>();

    [RelayCommand]
    private void NavigationToAdminReportExport() => _adminNavigationService.NavigateTo<AdminReportExportViewModel>();

    [RelayCommand]
    private void BackToLogin() => _navigationService.NavigateTo<LoginViewModel>();
}
