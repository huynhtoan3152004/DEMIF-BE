using Demif.Domain.Common;
using Demif.Domain.Enums;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity UserProgress - tiến độ học tập tổng thể
/// </summary>
public class UserProgress : BaseEntity
{
    public Guid UserId { get; set; }

    // Tổng quan
    public int TotalPoints { get; set; }
    public int TotalMinutes { get; set; }
    public int LessonsCompleted { get; set; }
    public int DictationCompleted { get; set; }
    public int ShadowingCompleted { get; set; }

    // Accuracy
    public decimal AvgDictationScore { get; set; }
    public decimal AvgShadowingScore { get; set; }

    /// <summary>
    /// JSON skills: {"listening": 75, "speaking": 60, "vocabulary": 80, "grammar": 70}
    /// </summary>
    public string? Skills { get; set; }

    // Level progression
    public Level CurrentLevel { get; set; } = Level.Beginner;
    public int LevelProgress { get; set; } // 0-100 (% to next level)

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual User? User { get; set; }
}
