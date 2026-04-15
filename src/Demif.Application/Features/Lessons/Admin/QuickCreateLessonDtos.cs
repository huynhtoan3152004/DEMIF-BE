using Demif.Domain.Enums;
using System.Text.Json.Serialization;

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

    /// <summary>"auto" | "srt" | "vtt" | "plain" — mặc định "auto"</summary>
    public string Format { get; set; } = "auto";

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
    [JsonConverter(typeof(LessonLevelJsonConverter))]
    public string Level { get; set; } = "Beginner";

    /// <summary>Loại bài: Dictation, Shadowing. Mặc định Dictation.</summary>
    [JsonConverter(typeof(LessonTypeJsonConverter))]
    public string LessonType { get; set; } = "Dictation";

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
/// Segment transcript đã chuẩn hóa để FE render ngay mà không phải tự parse file gốc.
/// </summary>
public class TranscriptSegmentDto
{
    public int Index { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
    public int WordCount { get; set; }
}

/// <summary>
/// Payload transcript đã parse xong.
/// FE chỉ cần đọc FullTranscript + Segments để render text và sync thời gian.
/// </summary>
public class ParsedTranscriptDto
{
    /// <summary>Giá trị request ban đầu: auto, srt, vtt, plain.</summary>
    public string RequestedFormat { get; set; } = string.Empty;

    /// <summary>Kết quả backend suy luận sau khi parse: timed hoặc plain.</summary>
    public string DetectedFormat { get; set; } = string.Empty;

    public string FullTranscript { get; set; } = string.Empty;
    public int SegmentCount { get; set; }
    public int WordCount { get; set; }
    public List<TranscriptSegmentDto> Segments { get; set; } = new();
}

/// <summary>
/// Internal parse result used by backend services.
/// Keeps the raw timed segments for persistence and the UI-friendly payload for responses.
/// </summary>
public class TranscriptParseResult
{
    public string RequestedFormat { get; set; } = string.Empty;
    public string DetectedFormat { get; set; } = string.Empty;
    public List<TimedSegment> Segments { get; set; } = new();
    public ParsedTranscriptDto Transcript { get; set; } = new();

    public string FullTranscript => Transcript.FullTranscript;
    public int SegmentCount => Transcript.SegmentCount;
    public int WordCount => Transcript.WordCount;
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
    public ParsedTranscriptDto Transcript { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
