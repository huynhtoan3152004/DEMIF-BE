using Demif.Domain.Common;

namespace Demif.Domain.Entities;

/// <summary>
/// Thông báo hệ thống dành cho từng user.
/// </summary>
public class UserNotification : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = "system_announcement";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string Channel { get; set; } = "email";
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public virtual User? User { get; set; }
}