using System.ComponentModel.DataAnnotations;

namespace Demif.Application.Features.Users.AssignRole;

/// <summary>
/// Request gán role cho user
/// </summary>
public class AssignRoleRequest
{
    [Required]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Ngày hết hạn role (optional, null = vĩnh viễn)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
