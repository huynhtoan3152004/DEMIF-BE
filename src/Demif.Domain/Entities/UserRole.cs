using Demif.Domain.Common;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity UserRole - bảng trung gian cho quan hệ nhiều-nhiều User-Role
/// Cho phép 1 user có nhiều role và 1 role có nhiều user
/// </summary>
public class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    /// <summary>
    /// Ngày được gán role
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Người gán role (null nếu là hệ thống tự gán)
    /// </summary>
    public Guid? AssignedBy { get; set; }

    /// <summary>
    /// Ngày hết hạn role (null = vĩnh viễn)
    /// Hữu ích cho Premium subscription
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
