namespace RailWeldApp.DataModels;

/// <summary>
/// 登录审计。成功和失败都记录；失败时 UserId 可以为空。
/// </summary>
public sealed class LoginRecord
{
    public long Id { get; set; }

    public string? UserId { get; set; }

    public string Account { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.Now;

    public string Message { get; set; } = string.Empty;

    public UserRecord? User { get; set; }
}
