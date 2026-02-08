using Demif.Domain.Common;
using Demif.Domain.Enums;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity Lesson - bài học Dictation/Shadowing
/// </summary>
public class Lesson : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Phân loại
    public LessonType LessonType { get; set; }
    public Level Level { get; set; }
    public string? Category { get; set; } // conversation, business, travel, academic

    // Media (linh hoạt cho cả audio và video)
    public string AudioUrl { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// URL media thay thế (hỗ trợ cả MP3/MP4). Ưu tiên dùng field này.
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Loại media: "audio" | "video"
    /// </summary>
    public string? MediaType { get; set; }

    // Content
    public string FullTranscript { get; set; } = string.Empty;

    /// <summary>
    /// JSON template cho Dictation với chỗ trống
    /// {"segments": [{"text": "I", "isBlank": false}, {"text": "___", "isBlank": true, "answer": "went", "hint": "w___"}]}
    /// </summary>
    public string? DictationTemplate { get; set; }

    // Stats (denormalized)
    public int CompletionsCount { get; set; }
    public decimal AvgScore { get; set; }

    public string Status { get; set; } = "published"; // draft, published, archived

    /// <summary>
    /// Chỉ user Premium mới xem được
    /// </summary>
    public bool IsPremiumOnly { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong danh sách
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// JSON array tags: ["business", "daily", "travel"]
    /// </summary>
    public string? Tags { get; set; }

    // Navigation
    public virtual ICollection<UserExercise> Exercises { get; set; } = new List<UserExercise>();
}
