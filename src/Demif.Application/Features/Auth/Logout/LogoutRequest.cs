using System.ComponentModel.DataAnnotations;

namespace Demif.Application.Features.Auth.Logout;

/// <summary>
/// Request đăng xuất - revoke refresh token
/// </summary>
public class LogoutRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
