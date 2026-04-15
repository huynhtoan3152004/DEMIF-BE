using Demif.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// DTO để cập nhật thông tin chung của Lesson (không bao gồm Transcript hay Status)
/// </summary>
public class UpdateLessonMetadataRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [JsonConverter(typeof(LessonTypeJsonConverter))]
    public string LessonType { get; set; } = "Dictation";

    [JsonConverter(typeof(LessonLevelJsonConverter))]
    public string Level { get; set; } = "Beginner";
    public string? Category { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPremiumOnly { get; set; }
    public int DisplayOrder { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Request upload audio MP3 riêng cho lesson.
/// Endpoint này chỉ upload file và trả về URL public.
/// FE sẽ dùng URL này cho quick-create hoặc update metadata.
/// </summary>
public class UploadLessonAudioRequest
{
    public IFormFile AudioFile { get; set; } = null!;
    public IFormFile? File { get; set; }
    public string FolderName { get; set; } = "demif-lessons/audio";
}

/// <summary>
/// Response của endpoint upload audio.
/// </summary>
public class UploadLessonAudioResponse
{
    public string MediaUrl { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "audio";
    public string FolderName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
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

/// <summary>
/// Request for updating dictation templates manually.
/// </summary>
public class UpdateDictationTemplatesRequest
{
    /// <summary>
    /// JSON array or object of custom dictation templates from the frontend.
    /// This will overwrite the existing auto-generated templates.
    /// Required shape per level:
    /// [{"level":"Beginner","blankPercentage":15,"segments":[{"startTime":0,"endTime":2.5,"words":[{"text":"Hello","isBlank":false,"position":0},{"text":"","isBlank":true,"position":1,"answer":"everyone"}]}]}]
    /// Each template must include `level` and each segment must include `words`.
    /// </summary>
    public string DictationTemplatesJson { get; set; } = string.Empty;
}
