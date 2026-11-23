using CommunityToolkit.Mvvm.ComponentModel;
using RailWeldApp.Interfaces;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Services;

public partial class DialogService : ObservableObject, IDialogService
{
    [ObservableProperty]
    private DialogViewModel? _currentDialog;

    public async Task ShowDialogAsync(DialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        // 同一时刻只允许一个对话框
        if (CurrentDialog is not null)
            return;

        CurrentDialog = viewModel;

        try
        {
            await viewModel.ShowAsync();
        }
        finally
        {
            if (ReferenceEquals(CurrentDialog, viewModel))
                CurrentDialog = null;
        }
    }
}