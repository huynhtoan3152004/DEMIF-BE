using Demif.Domain.Common;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity Role - quản lý vai trò người dùng
/// Được thiết kế để dễ dàng mở rộng thêm role mới
/// </summary>
public class Role : AuditableEntity
{
    /// <summary>
    /// Tên role (VD: Admin, User, Premium, Moderator)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về role
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Có phải role mặc định cho user mới không
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Role có đang active không
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Permissions dạng JSON để linh hoạt
    /// VD: {"canManageUsers": true, "canViewReports": false}
    /// </summary>
    public string? Permissions { get; set; }

    // Navigation property - Many-to-Many với User thông qua UserRole
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
