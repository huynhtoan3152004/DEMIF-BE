namespace Demif.Application.Features.Lessons.GetLessonById;

/// <summary>
/// Response cho lesson detail
/// </summary>
public class GetLessonByIdResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? MediaUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? MediaType { get; set; }
    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string FullTranscript { get; set; } = string.Empty;
    public string? DictationTemplate { get; set; }
    public bool IsPremiumOnly { get; set; }
    public int CompletionsCount { get; set; }
    public decimal AvgScore { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
}
