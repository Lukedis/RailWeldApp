using CommunityToolkit.Mvvm.ComponentModel;
using RailWeldApp.Interfaces;

namespace RailWeldApp.Services;

public partial class UserSession : ObservableObject, IUserSession
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSignedIn))]
    private string _account = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    public bool IsSignedIn => !string.IsNullOrWhiteSpace(Account);

    public void SignIn(string account, string displayName = "")
    {
        Account = account;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? account : displayName;
    }

    public void SignOut()
    {
        Account = string.Empty;
        DisplayName = string.Empty;
    }
}