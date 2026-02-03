using System.ComponentModel.DataAnnotations;

namespace Demif.Application.Features.Auth.RefreshToken;

/// <summary>
/// Request để refresh access token
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
