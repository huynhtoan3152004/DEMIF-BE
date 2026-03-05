namespace Demif.Application.Features.Me.GetProgress;

/// <summary>
/// Tiến độ học tập tổng quan của user
/// </summary>
public class GetProgressResponse
{
    public int TotalPoints { get; set; }
    public int TotalMinutes { get; set; }
    public int LessonsCompleted { get; set; }
    public int DictationCompleted { get; set; }
    public int ShadowingCompleted { get; set; }
    public decimal AvgDictationScore { get; set; }
    public decimal AvgShadowingScore { get; set; }
    public string CurrentLevel { get; set; } = "Beginner";

    /// <summary>0–100: % tiến độ đến level tiếp theo</summary>
    public int LevelProgress { get; set; }
}
