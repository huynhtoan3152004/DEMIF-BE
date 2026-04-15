using Demif.Application.Features.Admin.Analytics;

namespace Demif.Application.Features.Admin.Analytics.Lessons.Access;

public class LessonAccessAnalyticsResponse
{
    public DateTime GeneratedAt { get; set; }
    public int TotalAccessEvents { get; set; }
    public int TotalTrackedLessons { get; set; }
    public int TotalTrackedUsers { get; set; }
    public int CompletedTrackers { get; set; }
    public int InProgressTrackers { get; set; }
    public int StartedTrackers { get; set; }
    public List<LessonAccessItem> TopAccessedLessons { get; set; } = new();
    public List<LessonAccessItem> RecentAccessedLessons { get; set; } = new();
    public List<StatCountItem> ByStatus { get; set; } = new();
    public List<StatCountItem> ByAccessType { get; set; } = new();
}

public class LessonAccessItem
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int AccessCount { get; set; }
    public int UniqueUsers { get; set; }
    public int CompletedCount { get; set; }
    public int InProgressCount { get; set; }
    public int StartedCount { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime? FirstAccessedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
