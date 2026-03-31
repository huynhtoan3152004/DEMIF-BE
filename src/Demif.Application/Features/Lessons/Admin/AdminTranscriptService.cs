using System.Text.Json;
using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Lessons.Admin;

// ─── Request / Response DTOs ───────────────────────────────────────────────

public class UpdateTranscriptRequest
{
    /// <summary>Raw SRT / VTT / plain-text transcript do admin paste vào.</summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// "vtt" | "srt" | "plain"
    /// plain = chỉ text, không có timestamp → dùng auto-generate timing (kém chính xác hơn)
    /// </summary>
    public string Format { get; set; } = "vtt";
}

public class UpdateTranscriptResponse
{
    public Guid LessonId { get; set; }
    public int SegmentCount { get; set; }
    public int WordCount { get; set; }
    public bool DictationTemplatesRegenerated { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UpdateLessonStatusRequest
{
    /// <summary>"draft" | "published" | "archived"</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Admin xem preview bài dictation kèm ĐÁP ÁN — chưa xóa answers.</summary>
public class AdminDictationPreviewSegment
{
    public int Index { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; } = string.Empty; // Full transcript text
    public int WordCount { get; set; }
}

public class AdminDictationPreviewResponse
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalSegments { get; set; }
    public int TotalWords { get; set; }
    public bool ReadyToPublish { get; set; }
    public List<string> PublishBlockers { get; set; } = new();
    public List<AdminDictationPreviewSegment> Segments { get; set; } = new();
}

// ─── Service ────────────────────────────────────────────────────────────────

/// <summary>
/// AdminTranscriptService — quản lý transcript + status workflow.
///
/// Quy trình gọn nhất:
/// 1. POST /admin/lessons/from-youtube → hasCaptions? → tự động xong
/// 2. GET  /admin/lessons/{id}/dictation-preview → admin review segment + đáp án
/// 3. PATCH /admin/lessons/{id}/transcript  → (nếu cần sửa / video không có caption)
/// 4. PATCH /admin/lessons/{id}/status      → published
/// </summary>
public class AdminTranscriptService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly Demif.Application.Abstractions.Services.ICacheService _cacheService;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "draft", "published", "archived" };

    public AdminTranscriptService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext,
        Demif.Application.Abstractions.Services.ICacheService cacheService)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    private async Task InvalidateLessonCacheAsync(Guid? lessonId = null)
    {
        await _cacheService.RemoveByPrefixAsync("lessons:");
        if (lessonId.HasValue)
        {
            await _cacheService.RemoveByPrefixAsync($"lesson:{lessonId.Value}");
        }
    }

    // ── 1. Preview (admin xem tất cả segments + đáp án) ───────────────────

    public async Task<Result<AdminDictationPreviewResponse>> GetDictationPreviewAsync(
        Guid lessonId, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null)
            return Result.Failure<AdminDictationPreviewResponse>(Error.NotFound("Không tìm thấy bài học."));

        var blockers = new List<string>();

        if (string.IsNullOrWhiteSpace(lesson.TimedTranscript))
            blockers.Add("Chưa có TimedTranscript — cần upload SRT/VTT hoặc import YouTube có caption.");

        if (string.IsNullOrWhiteSpace(lesson.FullTranscript))
            blockers.Add("Chưa có FullTranscript.");

        List<TimedSegment> segments = new();
        if (!string.IsNullOrWhiteSpace(lesson.TimedTranscript))
        {
            try
            {
                segments = JsonSerializer.Deserialize<List<TimedSegment>>(lesson.TimedTranscript, JsonOpts)
                           ?? new List<TimedSegment>();
            }
            catch
            {
                blockers.Add("TimedTranscript bị lỗi format — cần upload lại.");
            }
        }

        var segmentDtos = segments.Select((s, i) => new AdminDictationPreviewSegment
        {
            Index = i,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Text = s.Text,              // FULL text + đáp án — admin thấy hết
            WordCount = s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
        }).ToList();

        return Result.Success(new AdminDictationPreviewResponse
        {
            LessonId = lesson.Id,
            Title = lesson.Title,
            Status = lesson.Status,
            TotalSegments = segmentDtos.Count,
            TotalWords = segmentDtos.Sum(s => s.WordCount),
            ReadyToPublish = blockers.Count == 0,
            PublishBlockers = blockers,
            Segments = segmentDtos
        });
    }

    // ── 2. Update transcript (SRT / VTT / plain) ──────────────────────────

    public async Task<Result<UpdateTranscriptResponse>> UpdateTranscriptAsync(
        Guid lessonId, UpdateTranscriptRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawContent))
            return Result.Failure<UpdateTranscriptResponse>(
                Error.Validation("Transcript content không được để trống."));

        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null)
            return Result.Failure<UpdateTranscriptResponse>(Error.NotFound("Không tìm thấy bài học."));

        // Parse theo format
        List<TimedSegment> segments;
        try
        {
            segments = request.Format.ToLowerInvariant() switch
            {
                "vtt" => ParseVtt(request.RawContent),
                "srt" => ParseSrt(request.RawContent),
                "plain" => GenerateFromPlain(request.RawContent, lesson.DurationSeconds),
                _ => throw new ArgumentException($"Format '{request.Format}' không hợp lệ. Dùng: vtt, srt, plain")
            };
        }
        catch (Exception ex)
        {
            return Result.Failure<UpdateTranscriptResponse>(
                Error.Validation($"Không thể parse transcript: {ex.Message}"));
        }

        if (segments.Count == 0)
            return Result.Failure<UpdateTranscriptResponse>(
                Error.Validation("Không tìm thấy segment nào trong nội dung. Kiểm tra lại format."));

        // Cập nhật lesson
        lesson.TimedTranscript = JsonSerializer.Serialize(segments, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        lesson.FullTranscript = string.Join(" ", segments.Select(s => s.Text));

        // Regenerate DictationTemplates
        var templatesRegenerated = false;
        try
        {
            lesson.DictationTemplates = DictationTemplateGenerator.GenerateAllTemplates(lesson.TimedTranscript);
            templatesRegenerated = true;
        }
        catch { /* non-critical */ }

        await _lessonRepository.UpdateAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await InvalidateLessonCacheAsync(lessonId);

        var wordCount = segments.Sum(s =>
            s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

        return Result.Success(new UpdateTranscriptResponse
        {
            LessonId = lesson.Id,
            SegmentCount = segments.Count,
            WordCount = wordCount,
            DictationTemplatesRegenerated = templatesRegenerated,
            Message = $"✅ Cập nhật transcript thành công: {segments.Count} segments, {wordCount} từ."
                      + (templatesRegenerated ? " Đã regenerate DictationTemplates." : "")
        });
    }

    // ── 3. Update status ──────────────────────────────────────────────────

    public async Task<Result<object>> UpdateStatusAsync(
        Guid lessonId, UpdateLessonStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!AllowedStatuses.Contains(request.Status))
            return Result.Failure<object>(
                Error.Validation($"Status '{request.Status}' không hợp lệ. Dùng: draft, published, archived."));

        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null)
            return Result.Failure<object>(Error.NotFound("Không tìm thấy bài học."));

        // Guard: không publish nếu thiếu transcript
        if (request.Status.Equals("published", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(lesson.TimedTranscript))
                return Result.Failure<object>(Error.Validation(
                    "Không thể publish: bài học chưa có TimedTranscript. " +
                    "Hãy import YouTube có caption hoặc PATCH /transcript trước."));
        }

        var oldStatus = lesson.Status;
        lesson.Status = request.Status.ToLowerInvariant();

        await _lessonRepository.UpdateAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await InvalidateLessonCacheAsync(lessonId);

        return Result.Success<object>(new
        {
            lessonId = lesson.Id,
            title = lesson.Title,
            previousStatus = oldStatus,
            newStatus = lesson.Status,
            message = $"Đã chuyển trạng thái: {oldStatus} → {lesson.Status}"
        });
    }

    // ── Parsers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Parse WebVTT format:
    ///   WEBVTT
    ///   00:00:02.500 --> 00:00:04.800
    ///   Hello everyone
    /// </summary>
    public static List<TimedSegment> ParseVtt(string content)
    {
        var segments = new List<TimedSegment>();
        var lines = content.Split('\n', StringSplitOptions.None);
        double? start = null, end = null;
        var textLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.Contains("-->"))
            {
                // Flush previous segment
                if (start.HasValue && textLines.Count > 0)
                    segments.Add(MakeSegment(start.Value, end ?? start.Value, textLines));

                var parts = line.Split("-->", 2);
                start = ParseTimestamp(parts[0].Trim());
                end = ParseTimestamp(parts[1].Trim().Split(' ', 2)[0]); // Trim trước để tránh split ra ""
                textLines = new List<string>();
            }
            else if (!string.IsNullOrWhiteSpace(line)
                     && !line.StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase)
                     && !IsNumericCueId(line))
            {
                textLines.Add(line);
            }
            else if (string.IsNullOrWhiteSpace(line) && start.HasValue && textLines.Count > 0)
            {
                segments.Add(MakeSegment(start.Value, end ?? start.Value, textLines));
                start = null; end = null; textLines = new List<string>();
            }
        }

        // Flush last
        if (start.HasValue && textLines.Count > 0)
            segments.Add(MakeSegment(start.Value, end ?? start.Value, textLines));

        return segments;
    }

    /// <summary>
    /// Parse SRT format:
    ///   1
    ///   00:00:02,500 --> 00:00:04,800
    ///   Hello everyone
    /// </summary>
    public static List<TimedSegment> ParseSrt(string content)
        => ParseVtt(content.Replace(",", ".")); // SRT dùng dấu phẩy, VTT dùng dấu chấm

    /// <summary>Plain text không có timestamp → dùng auto-generate (kém chính xác).</summary>
    public static List<TimedSegment> GenerateFromPlain(string content, int durationSeconds)
    {
        var json = DictationTemplateGenerator.GenerateTimedTranscript(content, durationSeconds);
        return JsonSerializer.Deserialize<List<TimedSegment>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<TimedSegment>();
    }

    private static TimedSegment MakeSegment(double start, double end, List<string> textLines)
    {
        // Strip HTML tags (<c>, <b>, positioning cues)
        var text = string.Join(" ", textLines);
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "").Trim();
        return new TimedSegment
        {
            StartTime = Math.Round(start, 3),
            EndTime = Math.Round(end, 3),
            Text = text
        };
    }

    private static double ParseTimestamp(string ts)
    {
        // HH:MM:SS.mmm hoặc MM:SS.mmm — luôn dùng InvariantCulture tránh lỗi locale
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        var parts = ts.Split(':');
        return parts.Length switch
        {
            3 => double.Parse(parts[0], ic) * 3600
               + double.Parse(parts[1], ic) * 60
               + double.Parse(parts[2], ic),
            2 => double.Parse(parts[0], ic) * 60
               + double.Parse(parts[1], ic),
            _ => 0
        };
    }

    private static bool IsNumericCueId(string line)
        => int.TryParse(line, out _);
}
