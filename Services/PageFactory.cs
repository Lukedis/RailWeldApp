using Microsoft.Extensions.DependencyInjection;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Services;

// 页面 VM 工厂：统一从 DI 解析 Transient 页面 VM，并允许在返回前做初始化。
// 导航服务用它创建页面，业务代码不需要直接碰 IServiceProvider。
public sealed class PageFactory(IServiceProvider provider)
{
    public T GetViewModel<T>(Action<T>? init = null) where T : ViewModelBase
    {
        T viewModel = provider.GetRequiredService<T>();

        init?.Invoke(viewModel);

        return viewModel;
    }
}
