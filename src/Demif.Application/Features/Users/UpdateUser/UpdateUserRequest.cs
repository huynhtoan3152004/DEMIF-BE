namespace Demif.Application.Features.Users.UpdateUser;

/// <summary>
/// Request cập nhật thông tin user (Admin)
/// </summary>
public class UpdateUserRequest
{
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }

    // Profile info
    public string? Country { get; set; }
    public string? NativeLanguage { get; set; }
    public string? TargetLanguage { get; set; }
    public string? CurrentLevel { get; set; }
    public int? DailyGoalMinutes { get; set; }
}
