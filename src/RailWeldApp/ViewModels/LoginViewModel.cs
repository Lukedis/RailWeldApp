using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Services;

namespace RailWeldApp.ViewModels;

public partial class LoginViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IUserSession _userSession;
    private readonly IDatabaseService _databaseService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _showPassword;

    [ObservableProperty]
    private bool _rememberUserName;

    [ObservableProperty]
    private bool _isSigningIn;

    [ObservableProperty]
    private string _noticeMessage = string.Empty;

    public LoginViewModel(
        INavigationService navigationService,
        IUserSession userSession,
        IDatabaseService databaseService,
        IPasswordService passwordService,
        ILogger<LoginViewModel> logger)
        : base(PageName.Login)
    {
        _navigationService = navigationService;
        _userSession = userSession;
        _databaseService = databaseService;
        _passwordService = passwordService;
        _logger = logger;
    }

    private bool CanSignIn() => !IsSigningIn;

    partial void OnIsSigningInChanged(bool value) => SignInCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync()
    {
        if (!IsUserNameAndPasswordNull())
            return;

        IsSigningIn = true;
        NoticeMessage = string.Empty;

        string account = UserName.Trim();

        try
        {
            UserRecord? user = await _databaseService.GetUserByAccountAsync(account);

            // 不区分“账号不存在 / 密码错误 / 已禁用”，避免泄露账户信息。
            if (user is null
                || !user.IsEnabled
                || !_passwordService.VerifyPassword(Password, user.PasswordHash, user.PasswordSalt))
            {
                await AddLoginRecordAsync(user?.Id, account, succeeded: false, "账号或密码错误");
                HasError = true;
                ErrorMessage = "账号或密码错误。";
                return;
            }

            _userSession.SignIn(user.Account, user.DisplayName);

            await _databaseService.UpdateUserLastLoginAsync(user.Id, DateTime.Now);
            await AddLoginRecordAsync(user.Id, user.Account, succeeded: true, "登录成功");

            _navigationService.NavigateTo<AdminViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败，账号：{Account}", account);

            HasError = true;
            ErrorMessage = "登录失败，请检查数据库或稍后重试。";
        }
        finally
        {
            IsSigningIn = false;
        }
    }

    private async Task AddLoginRecordAsync(string? userId, string account, bool succeeded, string message)
    {
        try
        {
            await _databaseService.AddLoginRecordAsync(new LoginRecord
            {
                UserId = userId,
                Account = account,
                Succeeded = succeeded,
                OccurredAt = DateTime.Now,
                Message = message,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写入登录审计失败，账号：{Account}", account);
        }
    }

    [RelayCommand]
    private void ForgotPassword()
    {
        HasError = true;
        ErrorMessage = "请联系系统管理员重置密码。";
    }

    [RelayCommand]
    private void SignInWithCode()
    {
        HasError = false;
        NoticeMessage = "验证码登录暂未开放，请使用账号密码登录。";
    }

    private bool IsUserNameAndPasswordNull()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(Password))
        {
            HasError = true;
            ErrorMessage = "请输入账号与密码！";
            return false;
        }

        if (string.IsNullOrEmpty(UserName))
        {
            HasError = true;
            ErrorMessage = "请输入账号！";
            return false;
        }

        if (string.IsNullOrEmpty(Password))
        {
            HasError = true;
            ErrorMessage = "请输入密码！";
            return false;
        }

        return true;
    }
}
