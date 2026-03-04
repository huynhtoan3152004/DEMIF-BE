namespace Demif.Application.Features.Lessons.GetLessonById;

/// <summary>
/// Lesson detail response.
/// MediaType determines the content source: "audio" (MP3), "video" (hosted), or "youtube" (YouTube embed).
/// </summary>
public class GetLessonByIdResponse
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
    public string FullTranscript { get; set; } = string.Empty;
    public bool HasDictationExercise { get; set; }
    public List<string>? AvailableDictationLevels { get; set; }
    public bool IsPremiumOnly { get; set; }
    public int CompletionsCount { get; set; }
    public decimal AvgScore { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
}
