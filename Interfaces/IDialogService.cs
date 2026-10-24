using System.ComponentModel;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Interfaces;

/// <summary>
/// 对话框服务：同一时刻只显示一个对话框，界面监听 <see cref="INotifyPropertyChanged"/> 来换内容。
/// </summary>
public interface IDialogService : INotifyPropertyChanged
{
    /// <summary>
    /// 当前显示的对话框；null 表示没有。
    /// </summary>
    DialogViewModel? CurrentDialog { get; set; }

    /// <summary>
    /// 显示一个对话框，等它关掉再返回。
    /// </summary>
    Task ShowDialogAsync(DialogViewModel viewModel);
}
