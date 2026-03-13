using Demif.Domain.Enums;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Quick-create lesson request — minimal fields, admin paste SRT/VTT/plain transcript.
/// Backend auto-generates: TimedTranscript, FullTranscript, DictationTemplates, DurationSeconds.
/// </summary>
public class QuickCreateLessonRequest
{
    /// <summary>Tiêu đề bài học (bắt buộc)</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Mô tả ngắn (tùy chọn)</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Nội dung transcript — hỗ trợ 3 format:
    /// - SRT: "1\n00:00:00,400 --> 00:00:02,320\nHello everyone\n\n2\n..."
    /// - VTT: "WEBVTT\n\n00:00:00.400 --> 00:00:02.320\nHello everyone\n\n..."
    /// - Plain: text thuần (kém chính xác, cần DurationSeconds)
    /// </summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>"srt" | "vtt" | "plain" — mặc định "srt"</summary>
    public string Format { get; set; } = "srt";

    /// <summary>
    /// URL media (YouTube embed hoặc audio file).
    /// Nếu null → lesson tạo nhưng không có media player.
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>"audio" | "video" | "youtube" — auto-detect từ MediaUrl nếu null</summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Thời lượng (giây). Bắt buộc nếu format = "plain".
    /// Với SRT/VTT, backend tự extract từ timestamps.
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>Cấp độ: Beginner, Intermediate, Advanced, Expert. Mặc định Beginner.</summary>
    public Level Level { get; set; } = Level.Beginner;

    /// <summary>Loại bài: Dictation, Shadowing. Mặc định Dictation.</summary>
    public LessonType LessonType { get; set; } = LessonType.Dictation;

    /// <summary>Danh mục: conversation, business, travel, academic, news, entertainment</summary>
    public string? Category { get; set; }

    /// <summary>Chỉ Premium mới xem được. Mặc định false.</summary>
    public bool IsPremiumOnly { get; set; }

    /// <summary>Thứ tự hiển thị. Mặc định 0.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Tags, ví dụ: "business,daily"</summary>
    public string? Tags { get; set; }

    /// <summary>URL thumbnail (tùy chọn)</summary>
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// Response sau khi quick-create lesson thành công.
/// </summary>
public class QuickCreateLessonResponse
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int SegmentCount { get; set; }
    public int WordCount { get; set; }
    public int DurationSeconds { get; set; }
    public bool HasDictationTemplates { get; set; }
    public string Message { get; set; } = string.Empty;
}
