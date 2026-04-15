namespace Demif.Application.Features.Lessons.GetLessons;

/// <summary>
/// Request cho lấy danh sách lessons
/// </summary>
public class GetLessonsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Level { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? MediaType { get; set; }
    public string? Tag { get; set; }
    public string? Search { get; set; }
}

/// <summary>
/// Lesson summary DTO for list view.
/// MediaType determines the content source: "audio" (MP3), "video" (hosted), or "youtube" (YouTube embed).
/// </summary>
public class LessonDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Lesson type: Dictation, Listening, etc.</summary>
    public string LessonType { get; set; } = string.Empty;

    /// <summary>Difficulty: Beginner, Intermediate, Advanced, Expert</summary>
    public string Level { get; set; } = string.Empty;

    public string? Category { get; set; }

    /// <summary>
    /// Primary media URL.
    /// - "audio": direct MP3/audio file URL
    /// - "youtube": YouTube embed URL (https://www.youtube.com/embed/{videoId})
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>Legacy audio URL (use MediaUrl instead). Kept for backward compatibility.</summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Content type: "audio" | "video" | "youtube".
    /// Frontend should render the appropriate player based on this value.
    /// </summary>
    public string MediaType { get; set; } = "audio";

    /// <summary>
    /// YouTube Video ID (only present when MediaType == "youtube").
    /// Use to build custom embed URLs or thumbnail URLs.
    /// </summary>
    public string? VideoId { get; set; }

    /// <summary>
    /// YouTube embed URL (only present when MediaType == "youtube").
    /// Ready-to-use in an iframe src attribute.
    /// </summary>
    public string? EmbedUrl { get; set; }

    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPremiumOnly { get; set; }
    public int CompletionsCount { get; set; }
    public decimal AvgScore { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Response cho danh sách lessons với pagination
/// </summary>
public class GetLessonsResponse
{
    public List<LessonDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
