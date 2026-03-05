namespace Demif.Application.Features.Me.RecordActivity;

/// <summary>
/// Kết quả sau khi ghi nhận hoạt động
/// </summary>
public class RecordActivityResponse
{
    public int TotalPoints { get; set; }
    public int PointsEarned { get; set; }
    public int CurrentStreak { get; set; }
    public bool StreakIncreased { get; set; }
    public string CurrentLevel { get; set; } = "Beginner";
    public int LevelProgress { get; set; }
}
