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
    public string? DictationTemplate { get; set; }
    public bool IsPremiumOnly { get; set; }
    public int DisplayOrder { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = "published";
}

/// <summary>
/// Response cho admin lesson với full info
/// </summary>
public class AdminLessonDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string FullTranscript { get; set; } = string.Empty;
    public string? DictationTemplate { get; set; }
    public bool IsPremiumOnly { get; set; }
    public int DisplayOrder { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CompletionsCount { get; set; }
    public decimal AvgScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
