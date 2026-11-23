namespace RailWeldApp.Options;

/// <summary>
/// 首次启动时创建的管理员账户。创建后请修改密码或关闭此配置。
/// </summary>
public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public bool Enabled { get; set; } = true;

    public string Account { get; set; } = "admin";

    public string Password { get; set; } = "admin123";

    public string DisplayName { get; set; } = "管理员";
}
