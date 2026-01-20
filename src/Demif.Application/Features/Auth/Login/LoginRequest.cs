using System.ComponentModel.DataAnnotations;

namespace Demif.Application.Features.Auth.Login;

/// <summary>
/// Request đăng nhập
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}
