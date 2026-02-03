using System.ComponentModel.DataAnnotations;

namespace Demif.Application.Features.Users.UpdateUserStatus;

/// <summary>
/// Request thay đổi status user (Activate/Deactivate)
/// </summary>
public class UpdateUserStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;  // Active, Inactive, Suspended, Banned
}
