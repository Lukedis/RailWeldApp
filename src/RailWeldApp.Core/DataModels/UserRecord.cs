using RailWeldApp.Enums;

namespace RailWeldApp.DataModels;

/// <summary>
/// 登录账户。记录按账户区分，不使用焊缝编号。
/// </summary>
public sealed class UserRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Account { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public OperatorRoles Role { get; set; } = OperatorRoles.Operator;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLoginAt { get; set; }

    public ICollection<WorkRecord> WorkRecords { get; set; } = [];

    public ICollection<LoginRecord> LoginRecords { get; set; } = [];
}
