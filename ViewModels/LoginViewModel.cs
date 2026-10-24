using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;

namespace RailWeldApp.ViewModels;

public partial class LoginViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;

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

    public LoginViewModel(INavigationService navigationService) : base(PageName.Login)
    {
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void SignIn()
    {
        // TODO:判断用户输入是否为空
        if (!IsUserNameAndPasswordNull())
            return;

        // TODO:从数据库中查询对应账号的信息

        // TODO:跳转到Admin页面
        _navigationService.NavigateTo<AdminViewModel>();
    }

    //partial void OnUserNameChanged(string value) => VerifyUserNameAndPassword();

    //partial void OnPasswordChanged(string value) => VerifyUserNameAndPassword();

    private bool IsUserNameAndPasswordNull()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(Password))
        {
            HasError = true;
            ErrorMessage = "请输入账号与密码!";
            return false;
        }
        if (string.IsNullOrEmpty(UserName))
        {
            HasError = true;
            ErrorMessage = "请输入账号!";
            return false;
        }
        if (string.IsNullOrEmpty(Password))
        {
            HasError = true;
            ErrorMessage = "请输入密码!";
            return false;
        }

        return true;
    }
}
