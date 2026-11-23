using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using RailWeldApp.Enums;

namespace RailWeldApp.ViewModels
{
    public partial class PageViewModel(PageName pageName) : ViewModelBase
    {
        [ObservableProperty]
        private PageName _pageName = pageName;

        // 弱事件订阅：页面 VM 是 Transient，导航走后会被 GC；
        // 如果直接 source.PropertyChanged += ...，单例的 PlcDataCache/导航服务
        // 会一直强引用旧页面 VM，切页越多内存越高、回调也越多。
        //
        // 用法（不需要手动退订）：
        // SubscribeWeak<INotifyPropertyChanged, PropertyChangedEventArgs>(
        //     _plcDataCache,
        //     nameof(INotifyPropertyChanged.PropertyChanged),
        //     OnPlcDataChanged);
        //
        // 如果页面 VM 还持有 Timer、CancellationTokenSource、串口回调等非事件资源，
        // 请额外实现 IDisposable，并在导航切换时释放。
        protected static void SubscribeWeak<TSource, TEventArgs>(
            TSource source,
            string eventName,
            EventHandler<TEventArgs> handler)
            where TSource : class
            where TEventArgs : EventArgs
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
            ArgumentNullException.ThrowIfNull(handler);

            WeakEventManager<TSource, TEventArgs>.AddHandler(source, eventName, handler);
        }
    }
}
