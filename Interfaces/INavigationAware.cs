namespace RailWeldApp.Interfaces;

// 页面 VM 可选实现：导航进入/离开时由导航服务调用。
// 回调在 UI 线程同步执行，不要在里面阻塞；需要异步加载时，
// 在 OnNavigatedTo 里启动任务并保存 CancellationTokenSource，在 OnNavigatedFrom 里取消。
public interface INavigationAware
{
    // 进入页面
    void OnNavigatedTo();

    // 离开页面
    void OnNavigatedFrom();
}
