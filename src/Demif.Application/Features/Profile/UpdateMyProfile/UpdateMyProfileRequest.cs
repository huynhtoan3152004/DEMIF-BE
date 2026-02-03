namespace Demif.Application.Features.Profile.UpdateMyProfile;

/// <summary>
/// Request cập nhật profile của user hiện tại
/// </summary>
public class UpdateMyProfileRequest
{
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Country { get; set; }
    public string? NativeLanguage { get; set; }
    public string? TargetLanguage { get; set; }
    public string? CurrentLevel { get; set; }
    public int? DailyGoalMinutes { get; set; }
}
