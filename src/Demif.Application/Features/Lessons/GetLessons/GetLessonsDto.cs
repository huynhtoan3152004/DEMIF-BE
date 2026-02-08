using Demif.Domain.Enums;

namespace Demif.Application.Features.Lessons.GetLessons;

/// <summary>
/// Request cho lấy danh sách lessons
/// </summary>
public class GetLessonsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Level? Level { get; set; }
    public LessonType? Type { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// DTO cho lesson
/// </summary>
public class LessonDto
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
