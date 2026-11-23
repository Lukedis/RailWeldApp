using Microsoft.Extensions.Logging;
using RailWeldApp.Interfaces;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Services;

// 导航生命周期的统一调用点。页面回调失败不应该阻断页面切换，所以这里只记录日志。
internal static class NavigationLifecycle
{
    // 调用旧页面的 OnNavigatedFrom；异常只记日志，不阻断导航。
    public static void Leave(ViewModelBase? view, ILogger logger)
    {
        // 判断是否实现了 INavigationAware 接口，如果没有实现则直接返回
        if (view is not INavigationAware aware)
        {
            return;
        }

        try
        {
            // 调用旧页面的OnNavigatedFrom离开页面的方法
            aware.OnNavigatedFrom();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "页面离开回调失败：{Page}", view.GetType().Name);
        }
    }

    // 调用新页面的 OnNavigatedTo；异常只记日志，不阻断导航。
    public static void Enter(ViewModelBase? view, ILogger logger)
    {
        // 判断是否实现了 INavigationAware 接口，如果没有实现则直接返回
        if (view is not INavigationAware aware)
        {
            return;
        }

        try
        {
            // 调用新的页面的OnNavigatedTo进入方法
            aware.OnNavigatedTo();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "页面进入回调失败：{Page}", view.GetType().Name);
        }
    }
}