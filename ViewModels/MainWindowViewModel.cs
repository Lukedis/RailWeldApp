using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RailWeldApp.Interfaces;
using RailWeldApp.Services;

namespace RailWeldApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Test

    private readonly INavigationService _navigationService;

    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        _navigationService.PropertyChanged += OnNavigationChanged;

        // 启动后先显示登录页；登录成功由 LoginViewModel 导航到管理台。
        _navigationService.NavigateTo<LoginViewModel>();

        CurrentView = _navigationService.CurrentView;
    }

    /// <summary>
    /// 窗口当前显示的页面。界面用 ContentControl 绑它，由 DataTemplate 决定长什么样。
    /// </summary>
    [ObservableProperty]
    private ViewModelBase? _currentView;

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentView))
            CurrentView = _navigationService.CurrentView;
    }

    #endregion Test
}
