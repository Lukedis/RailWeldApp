using System.ComponentModel;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Interfaces
{
    // 窗口级导航（登录页 ↔ 管理台）。CurrentView 只读，由 NavigateTo 统一切换。
    public interface INavigationService : INotifyPropertyChanged
    {
        ViewModelBase? CurrentView { get; }

        // 创建并切换到目标页面。init 在页面成为 CurrentView 之前执行（用于传查询条件等）。
        void NavigateTo<TViewModel>(Action<TViewModel>? init = null) where TViewModel : ViewModelBase;
    }
}
