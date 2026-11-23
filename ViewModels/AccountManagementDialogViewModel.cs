using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;

namespace RailWeldApp.ViewModels;

/// <summary>
/// 账户管理：创建操作员、启用/禁用、角色、密码重置。
/// </summary>
public partial class AccountManagementDialogViewModel : DialogViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IPasswordService _passwordService;
    private readonly IUserSession _session;

    public AccountManagementDialogViewModel(
        IDatabaseService databaseService,
        IPasswordService passwordService,
        IUserSession session)
    {
        _databaseService = databaseService;
        _passwordService = passwordService;
        _session = session;

        _ = LoadUsersAsync();
    }

    public ObservableCollection<AccountRow> Users { get; } = [];

    public IReadOnlyList<RoleOption> RoleOptions { get; } =
    [
        new(OperatorRoles.Operator, "操作员"),
        new(OperatorRoles.Admin, "管理员"),
    ];

    [ObservableProperty]
    private AccountRow? _selectedUser;

    [ObservableProperty]
    private bool _canManage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _newAccount = string.Empty;

    [ObservableProperty]
    private string _newDisplayName = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private OperatorRoles _newRole = OperatorRoles.Operator;

    [ObservableProperty]
    private string _resetPassword = string.Empty;

    public string CurrentAccount => _session.Account;

    public bool IsSelfSelected => SelectedUser?.Account == CurrentAccount;

    private async Task LoadUsersAsync()
    {
        IsBusy = true;

        try
        {
            List<UserRecord> records = await _databaseService.GetUsersAsync();
            UserRecord? current = await _databaseService.GetUserByAccountAsync(CurrentAccount);

            // 只有管理员才能修改账户；普通操作员仍可查看列表。
            CanManage = current?.Role == OperatorRoles.Admin;

            string selectedAccount = SelectedUser?.Account ?? CurrentAccount;

            Users.Clear();

            foreach (UserRecord record in records.OrderBy(x => x.Account))
                Users.Add(new AccountRow(record));

            SelectedUser = Users.FirstOrDefault(x => x.Account == selectedAccount)
                ?? Users.FirstOrDefault();

            if (!CanManage)
                SetError("当前账户不是管理员，不能管理其他账户。");
        }
        catch (Exception)
        {
            SetError("读取账户列表失败。");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedUserChanged(AccountRow? value)
    {
        OnPropertyChanged(nameof(IsSelfSelected));
        ResetPassword = string.Empty;
        StatusMessage = string.Empty;
        HasError = false;
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (!CanManage)
            return;

        // 账号、初始密码和长度是创建账户的最低校验。
        if (string.IsNullOrWhiteSpace(NewAccount) || string.IsNullOrWhiteSpace(NewPassword))
        {
            SetError("账号和初始密码不能为空。");
            return;
        }

        if (NewPassword.Length < 6)
        {
            SetError("初始密码至少 6 位。");
            return;
        }

        if (Users.Any(x => string.Equals(x.Account, NewAccount.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            SetError("账号已存在。");
            return;
        }

        try
        {
            string salt = _passwordService.CreateSalt();

            var user = new UserRecord
            {
                Account = NewAccount.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(NewDisplayName) ? NewAccount.Trim() : NewDisplayName.Trim(),
                Role = NewRole,
                PasswordSalt = salt,
                PasswordHash = _passwordService.HashPassword(NewPassword, salt),
                IsEnabled = true,
                CreatedAt = DateTime.Now,
            };

            await _databaseService.AddUserAsync(user);

            NewAccount = string.Empty;
            NewDisplayName = string.Empty;
            NewPassword = string.Empty;
            NewRole = OperatorRoles.Operator;

            await LoadUsersAsync();
            SelectedUser = Users.FirstOrDefault(x => x.Account == user.Account);
            SetStatus($"已创建账户 {user.Account}。");
        }
        catch (Exception)
        {
            SetError("创建账户失败，账号可能已存在。");
        }
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        AccountRow? row = SelectedUser;

        if (!CanManage || row is null)
            return;

        if (row.Account == CurrentAccount && row.Role != OperatorRoles.Admin)
        {
            // 防止当前管理员把自己降级后无法再管理账户。
            SetError("不能取消当前登录账户的管理员角色。");
            return;
        }

        bool removingLastAdmin = row.Record.Role == OperatorRoles.Admin
            && row.Role != OperatorRoles.Admin
            && Users.Count(x => x.Role == OperatorRoles.Admin) <= 1;

        if (removingLastAdmin)
        {
            SetError("至少需要保留一个管理员账户。");
            return;
        }

        try
        {
            row.Record.DisplayName = row.DisplayName.Trim();
            row.Record.Role = row.Role;
            row.Record.IsEnabled = row.IsEnabled;

            if (!await _databaseService.UpdateUserAsync(row.Record))
            {
                SetError("账户不存在，保存失败。");
                return;
            }

            await LoadUsersAsync();
            SetStatus("账户信息已保存。");
        }
        catch (Exception)
        {
            SetError("保存账户失败。");
        }
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync()
    {
        AccountRow? row = SelectedUser;

        if (!CanManage || row is null)
            return;

        if (row.Account == CurrentAccount)
        {
            // 当前账户必须保持可用，否则用户退出后无法再次登录。
            SetError("不能禁用当前登录账户。");
            return;
        }

        if (row.IsEnabled
            && row.Role == OperatorRoles.Admin
            && Users.Count(x => x.Role == OperatorRoles.Admin && x.IsEnabled) <= 1)
        {
            SetError("至少需要保留一个启用状态的管理员。");
            return;
        }

        row.IsEnabled = !row.IsEnabled;

        try
        {
            row.Record.IsEnabled = row.IsEnabled;

            await _databaseService.UpdateUserAsync(row.Record);

            SetStatus(row.IsEnabled ? "账户已启用。" : "账户已禁用。");
        }
        catch (Exception)
        {
            row.IsEnabled = !row.IsEnabled;
            SetError("更新账户状态失败。");
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        AccountRow? row = SelectedUser;

        if (!CanManage || row is null)
            return;

        // 管理员重置密码不需要知道旧密码，但必须满足最小长度。
        if (ResetPassword.Length < 6)
        {
            SetError("重置密码至少 6 位。");
            return;
        }

        try
        {
            string salt = _passwordService.CreateSalt();
            row.Record.PasswordSalt = salt;
            row.Record.PasswordHash = _passwordService.HashPassword(ResetPassword, salt);

            if (!await _databaseService.UpdateUserAsync(row.Record))
            {
                SetError("账户不存在，重置失败。");
                return;
            }

            ResetPassword = string.Empty;
            SetStatus("密码已重置。");
        }
        catch (Exception)
        {
            SetError("重置密码失败。");
        }
    }

    [RelayCommand]
    private async Task CloseDialogAsync()
    {
        await CloseAsync();
    }

    private void SetStatus(string message)
    {
        HasError = false;
        StatusMessage = message;
    }

    private void SetError(string message)
    {
        HasError = true;
        StatusMessage = message;
    }
}

public sealed record RoleOption(OperatorRoles Value, string Text)
{
    public override string ToString() => Text;
}
