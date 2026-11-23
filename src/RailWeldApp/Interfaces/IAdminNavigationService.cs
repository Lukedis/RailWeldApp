using System.ComponentModel;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Interfaces
{
    // 管理台内部导航（总览 / 实时 / 手动 / 自动 / ...）。CurrentView 只读。
    public interface IAdminNavigationService : INotifyPropertyChanged
    {
        ViewModelBase? CurrentView { get; }

        // 创建并切换到目标子页面。init 在页面成为 CurrentView 之前执行（用于传账户/查询条件）。
        void NavigateTo<TViewModel>(Action<TViewModel>? init = null) where TViewModel : ViewModelBase;
    }
}
