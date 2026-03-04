namespace Demif.Application.Features.Lessons.GetLessonSegments;

/// <summary>
/// Request lấy segments của lesson.
/// Level quyết định LevelConfig → FE biết ẩn/hiện transcript.
/// </summary>
public class GetLessonSegmentsRequest
{
    public string Level { get; set; } = "Intermediate";
}

/// <summary>
/// Config hành vi theo level — FE dùng để điều khiển UI.
/// </summary>
public class LevelConfig
{
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Hiện transcript trước khi user gõ (Beginner: true, còn lại: false)
    /// </summary>
    public bool ShowTranscriptBefore { get; set; }

    /// <summary>
    /// Hiện transcript sau khi submit (Beginner + Intermediate: true, Advanced + Expert: false)
    /// FE vẫn nhận transcript từ /check — đây chỉ là hint cho FE biết có nên auto-show không
    /// </summary>
    public bool ShowTranscriptAfter { get; set; }

    /// <summary>
    /// Số lần replay mỗi segment. -1 = unlimited
    /// </summary>
    public int MaxReplays { get; set; }

    /// <summary>
    /// Hiện gợi ý số chữ cái (e.g. "_ _ _ _ _") cho segment text
    /// </summary>
    public bool ShowWordCount { get; set; }
}

/// <summary>
/// Một segment của bài — có startTime/endTime để FE sync với YouTube player.
/// Text chỉ có mặt nếu level == Beginner (ShowTranscriptBefore = true).
/// </summary>
public class LessonSegmentDto
{
    public int Index { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }

    /// <summary>
    /// Số từ — FE dùng để hiện gợi ý "_ _ _ _" khi ShowWordCount = true
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Transcript text — CHỈ có mặt khi ShowTranscriptBefore = true (Beginner level).
    /// Null với tất cả level khác.
    /// </summary>
    public string? Text { get; set; }
}

/// <summary>
/// Response đầy đủ cho FE: lesson info + levelConfig + danh sách segments.
/// </summary>
public class LessonSegmentsResponse
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string? MediaType { get; set; }
    public int DurationSeconds { get; set; }
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Config cho level đã chọn — FE dùng để render UI phù hợp
    /// </summary>
    public LevelConfig LevelConfig { get; set; } = null!;

    public List<LessonSegmentDto> Segments { get; set; } = new();
    public int TotalSegments { get; set; }
}
