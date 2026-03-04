using Demif.Domain.Enums;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Request cho tạo/cập nhật lesson
/// </summary>
public class CreateUpdateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LessonType LessonType { get; set; }
    public Level Level { get; set; }
    public string? Category { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string FullTranscript { get; set; } = string.Empty;

    /// <summary>
    /// JSON array timed segments (optional).
    /// Nếu admin không cung cấp, backend sẽ auto-generate từ FullTranscript + DurationSeconds.
    /// Format: [{"startTime": 0.0, "endTime": 2.5, "text": "Hello everyone"}]
    /// </summary>
    public string? TimedTranscript { get; set; }

    public bool IsPremiumOnly { get; set; }
    public int DisplayOrder { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = "published";
}

/// <summary>
/// Admin lesson detail DTO with full info.
/// </summary>
public class AdminLessonDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? Category { get; set; }

    /// <summary>Legacy audio URL.</summary>
    public string AudioUrl { get; set; } = string.Empty;

    /// <summary>Primary media URL (MP3 path or YouTube embed URL).</summary>
    public string? MediaUrl { get; set; }

    /// <summary>"audio" | "video" | "youtube"</summary>
    public string? MediaType { get; set; }

    /// <summary>YouTube Video ID (populated when MediaType == "youtube").</summary>
    public string? VideoId { get; set; }

    /// <summary>YouTube embed URL (populated when MediaType == "youtube").</summary>
    public string? EmbedUrl { get; set; }

    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string FullTranscript { get; set; } = string.Empty;
    public string? TimedTranscript { get; set; }
    public bool HasDictationTemplates { get; set; }
    public bool IsPremiumOnly { get; set; }
    public int DisplayOrder { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CompletionsCount { get; set; }
    public decimal AvgScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
