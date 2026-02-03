namespace Demif.Application.Features.Users.GetUserById;

/// <summary>
/// Response chi tiết user
/// </summary>
public class GetUserByIdResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty;

    // Profile info
    public string? Country { get; set; }
    public string NativeLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public int DailyGoalMinutes { get; set; }

    // Auth info
    public string AuthProvider { get; set; } = string.Empty;

    // Roles
    public List<UserRoleDto> Roles { get; set; } = new();

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserRoleDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
