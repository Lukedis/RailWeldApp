using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RailWeldApp.ViewModels;

/// <summary>
/// 通用确认对话框。ShowDialogAsync 返回后读取 Result 判断用户选择。
/// </summary>
public partial class ConfirmDialogViewModel : DialogViewModel
{
    [ObservableProperty]
    private string _title = "请确认";

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _confirmText = "确定";

    [ObservableProperty]
    private string _cancelText = "取消";

    public bool Result { get; private set; }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        Result = true;
        await CloseAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        Result = false;
        await CloseAsync();
    }
}
