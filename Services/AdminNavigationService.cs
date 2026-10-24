using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RailWeldApp.Interfaces;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Services
{
    public partial class AdminNavigationService(PageFactory pageFactory, ILogger<AdminNavigationService> logger)
        : ObservableObject, IAdminNavigationService
    {
        [ObservableProperty]
        private ViewModelBase? _currentView;

        public void NavigateTo<TViewModel>(Action<TViewModel>? init = null) where TViewModel : ViewModelBase
        {
            TViewModel nextView = pageFactory.GetViewModel(init);

            if (ReferenceEquals(CurrentView, nextView))
                return;

            // 旧页面离开
            NavigationLifecycle.Leave(CurrentView, logger);

            CurrentView = nextView;

            // 页面进入
            NavigationLifecycle.Enter(nextView, logger);
        }
    }
}
