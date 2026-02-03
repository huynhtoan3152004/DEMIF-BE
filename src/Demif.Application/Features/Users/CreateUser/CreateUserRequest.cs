using System.ComponentModel.DataAnnotations;

namespace Demif.Application.Features.Users.CreateUser;

/// <summary>
/// Request tạo user mới (Admin)
/// </summary>
public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    // Optional profile
    public string? Country { get; set; }
    public string NativeLanguage { get; set; } = "Vietnamese";
    public string TargetLanguage { get; set; } = "English";
    public string? AvatarUrl { get; set; }

    // Roles to assign (by name)
    public List<string> Roles { get; set; } = new() { "User" };
}
