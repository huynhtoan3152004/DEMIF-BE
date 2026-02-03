namespace Demif.Application.Features.Profile.GetMyProfile;

/// <summary>
/// Response lấy profile của user hiện tại
/// </summary>
public class GetMyProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    // Profile info
    public string? Country { get; set; }
    public string NativeLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public int DailyGoalMinutes { get; set; }

    // Roles
    public List<string> Roles { get; set; } = new();

    // Account info
    public string AuthProvider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
