using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RailWeldApp.Interfaces;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Services
{
    public partial class NavigationService(PageFactory pageFactory, ILogger<NavigationService> logger)
        : ObservableObject, INavigationService
    {
        [ObservableProperty]
        private ViewModelBase? _currentView;

        public void NavigateTo<TViewModel>(Action<TViewModel>? init = null) where TViewModel : ViewModelBase
        {
            TViewModel next = pageFactory.GetViewModel(init);

            if (ReferenceEquals(CurrentView, next))
                return;

            NavigationLifecycle.Leave(CurrentView, logger);

            CurrentView = next;

            NavigationLifecycle.Enter(next, logger);
        }
    }
}
