namespace RailWeldApp.Interfaces;

// 登录后的当前账户。页面只读，记录按账户区分。
public interface IUserSession
{
    string Account { get; }
    string DisplayName { get; }
    bool IsSignedIn { get; }

    void SignIn(string account, string displayName = "");

    void SignOut();
}
