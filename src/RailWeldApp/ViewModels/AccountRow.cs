using CommunityToolkit.Mvvm.ComponentModel;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;

namespace RailWeldApp.ViewModels;

/// <summary>
/// 账户管理列表的一行。
/// </summary>
public partial class AccountRow : ObservableObject
{
    public AccountRow(UserRecord record)
    {
        Record = record;
        Account = record.Account;
        _displayName = record.DisplayName;
        _role = record.Role;
        _isEnabled = record.IsEnabled;
    }

    public UserRecord Record { get; }

    public string Account { get; }

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoleText))]
    private OperatorRoles _role;

    [ObservableProperty]
    private bool _isEnabled;

    public string RoleText => Role == OperatorRoles.Admin ? "管理员" : "操作员";

    public string StatusText => IsEnabled ? "启用" : "已禁用";

    partial void OnIsEnabledChanged(bool value) => OnPropertyChanged(nameof(StatusText));
}
