using Demif.Domain.Common;
using Demif.Domain.Enums;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity UserExercise - kết quả làm bài của user
/// </summary>
public class UserExercise : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }

    public ExerciseType ExerciseType { get; set; }

    /// <summary>
    /// Dictation: câu trả lời | Shadowing: transcript
    /// </summary>
    public string? UserInput { get; set; }

    /// <summary>
    /// URL file ghi âm (Cloudflare R2)
    /// </summary>
    public string? RecordingUrl { get; set; }

    /// <summary>
    /// JSON kết quả chi tiết
    /// Dictation: {"totalBlanks": 5, "correctBlanks": 4, "answers": [{...}]}
    /// Shadowing: {"wordAccuracy": 85, "pronunciation": 78, "fluency": 80}
    /// </summary>
    public string? ResultDetails { get; set; }

    public int Score { get; set; } // 0-100
    public int? TimeSpentSeconds { get; set; }
    public int Attempts { get; set; } = 1;
    public int PlaysUsed { get; set; } = 1;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual User? User { get; set; }
    public virtual Lesson? Lesson { get; set; }
}
