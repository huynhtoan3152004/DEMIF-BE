using System.Text.Json;
using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Application.Features.Lessons;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Lessons.Admin;

// ─── Request / Response DTOs ───────────────────────────────────────────────

public class UpdateTranscriptRequest
{
    /// <summary>Raw SRT / VTT / plain-text transcript do admin paste vào.</summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// "auto" | "vtt" | "srt" | "plain"
    /// plain = chỉ text, không có timestamp → dùng auto-generate timing (kém chính xác hơn)
    /// </summary>
    public string Format { get; set; } = "auto";
}

public class UpdateTranscriptResponse
{
    public Guid LessonId { get; set; }
    public int SegmentCount { get; set; }
    public int WordCount { get; set; }
    public bool DictationTemplatesRegenerated { get; set; }
    public ParsedTranscriptDto Transcript { get; set; } = new();
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
    public Dictionary<string, DictationTemplate> DictationTemplates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
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

        if (string.IsNullOrWhiteSpace(lesson.DictationTemplates))
            blockers.Add("Chưa có DictationTemplates.");

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

        var dictationTemplates = new Dictionary<string, DictationTemplate>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(lesson.DictationTemplates))
        {
            try
            {
                dictationTemplates = AdminLessonService.ParseDictationTemplates(lesson.DictationTemplates);
            }
            catch
            {
                blockers.Add("DictationTemplates bị lỗi format — cần regenerate hoặc update lại.");
            }
        }

        return Result.Success(new AdminDictationPreviewResponse
        {
            LessonId = lesson.Id,
            Title = lesson.Title,
            Status = lesson.Status,
            TotalSegments = segmentDtos.Count,
            TotalWords = segmentDtos.Sum(s => s.WordCount),
            ReadyToPublish = blockers.Count == 0,
            PublishBlockers = blockers,
            Segments = segmentDtos,
            DictationTemplates = dictationTemplates
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
        string detectedFormat;
        try
        {
            var parseResult = ParseTranscriptPayload(request.RawContent, request.Format, lesson.DurationSeconds);
            segments = parseResult.Segments;
            detectedFormat = parseResult.DetectedFormat;
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
            Transcript = BuildTranscriptDto(request.Format, detectedFormat, segments),
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
        foreach (var block in SplitTranscriptBlocks(content))
        {
            if (block.Count == 0)
                continue;

            if (block.Any(line => line.StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase)))
                continue;

            if (block[0].StartsWith("NOTE", StringComparison.OrdinalIgnoreCase)
                || block[0].StartsWith("STYLE", StringComparison.OrdinalIgnoreCase)
                || block[0].StartsWith("REGION", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var timingLineIndex = block.FindIndex(line => line.Contains("-->", StringComparison.Ordinal));
            if (timingLineIndex < 0)
                continue;

            if (!TryParseCueTimingLine(block[timingLineIndex], out var start, out var end))
                continue;

            var textLines = block
                .Skip(timingLineIndex + 1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (textLines.Count == 0)
                continue;

            segments.Add(MakeSegment(start, end, textLines));
        }

        return segments;
    }

    /// <summary>
    /// Parse SRT format:
    ///   1
    ///   00:00:02,500 --> 00:00:04,800
    ///   Hello everyone
    /// </summary>
    public static List<TimedSegment> ParseSrt(string content)
    {
        var normalizedBlocks = SplitTranscriptBlocks(content)
            .Select(block => block
                .Select(line => line.Contains("-->", StringComparison.Ordinal)
                    ? NormalizeSrtCueLine(line)
                    : line)
                .ToList())
            .ToList();

        var segments = new List<TimedSegment>();
        foreach (var block in normalizedBlocks)
        {
            if (block.Count == 0)
                continue;

            var timingLineIndex = block.FindIndex(line => line.Contains("-->", StringComparison.Ordinal));
            if (timingLineIndex < 0)
                continue;

            if (!TryParseCueTimingLine(block[timingLineIndex], out var start, out var end))
                continue;

            var textLines = block
                .Skip(timingLineIndex + 1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (textLines.Count == 0)
                continue;

            segments.Add(MakeSegment(start, end, textLines));
        }

        return segments;
    }

    /// <summary>Plain text không có timestamp → dùng auto-generate (kém chính xác).</summary>
    public static List<TimedSegment> GenerateFromPlain(string content, int durationSeconds)
    {
        var normalizedContent = StripLeadingInlineTimestamps(content);
        var json = DictationTemplateGenerator.GenerateTimedTranscript(normalizedContent, durationSeconds);
        return JsonSerializer.Deserialize<List<TimedSegment>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<TimedSegment>();
    }

    /// <summary>
    /// Parse transcript cho FE/backend dùng chung.
    /// Auto-detect mặc định ưu tiên file có timestamps; nếu không có timestamps thì xem như plain text.
    /// </summary>
    public static TranscriptParseResult ParseTranscriptPayload(
        string rawContent,
        string? format,
        int? durationSeconds = null)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            throw new ArgumentException("Transcript content không được để trống.");

        var normalizedFormat = NormalizeFormat(format);
        var parseMode = normalizedFormat == "auto"
            ? DetectParseMode(rawContent)
            : normalizedFormat;

        var detectedFormat = parseMode == "plain" ? "plain" : "timed";

        List<TimedSegment> segments = parseMode switch
        {
            "vtt" => ParseVtt(rawContent),
            "srt" => ParseSrt(rawContent),
            "plain" => GenerateFromPlain(rawContent, durationSeconds ?? 60),
            _ => throw new ArgumentException($"Format '{format}' không hợp lệ. Dùng: auto, vtt, srt, plain")
        };

        if (segments.Count == 0)
            throw new ArgumentException("Không tìm thấy segment nào trong nội dung transcript.");

        return new TranscriptParseResult
        {
            RequestedFormat = normalizedFormat,
            DetectedFormat = detectedFormat,
            Segments = segments,
            Transcript = BuildTranscriptDto(normalizedFormat, detectedFormat, segments)
        };
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

    private static List<List<string>> SplitTranscriptBlocks(string content)
    {
        var blocks = new List<List<string>>();
        var currentBlock = new List<string>();

        foreach (var rawLine in content.Split('\n', StringSplitOptions.None))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentBlock.Count > 0)
                {
                    blocks.Add(currentBlock);
                    currentBlock = new List<string>();
                }

                continue;
            }

            currentBlock.Add(line.Trim());
        }

        if (currentBlock.Count > 0)
            blocks.Add(currentBlock);

        return blocks;
    }

    private static string NormalizeSrtCueLine(string cueLine)
    {
        var parts = cueLine.Split("-->", 2, StringSplitOptions.None);
        if (parts.Length != 2)
            return cueLine;

        var start = parts[0].Trim().Replace(',', '.');
        var endAndSettings = parts[1].Trim();
        var endParts = endAndSettings.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var end = endParts.Length > 0 ? endParts[0].Replace(',', '.') : endAndSettings.Replace(',', '.');
        var settings = endParts.Length > 1 ? " " + endParts[1] : string.Empty;
        return $"{start} --> {end}{settings}";
    }

    private static bool TryParseCueTimingLine(string timingLine, out double start, out double end)
    {
        start = 0;
        end = 0;

        var parts = timingLine.Split("-->", 2, StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        var startText = parts[0].Trim();
        var endText = parts[1].Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];

        try
        {
            start = ParseTimestamp(startText);
            end = ParseTimestamp(endText);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ParsedTranscriptDto BuildTranscriptDto(
        string requestedFormat,
        string detectedFormat,
        List<TimedSegment> segments)
    {
        var transcriptSegments = segments.Select((segment, index) => new TranscriptSegmentDto
        {
            Index = index,
            StartTime = segment.StartTime,
            EndTime = segment.EndTime,
            Text = segment.Text,
            WordCount = CountWords(segment.Text)
        }).ToList();

        return new ParsedTranscriptDto
        {
            RequestedFormat = requestedFormat,
            DetectedFormat = detectedFormat,
            FullTranscript = string.Join(" ", segments.Select(s => s.Text)),
            SegmentCount = transcriptSegments.Count,
            WordCount = transcriptSegments.Sum(s => s.WordCount),
            Segments = transcriptSegments
        };
    }

    private static string NormalizeFormat(string? format)
    {
        var normalized = (format ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "auto" : normalized;
    }

    private static string DetectParseMode(string rawContent)
    {
        if (LooksLikeTimedTranscript(rawContent))
            return rawContent.Contains(',', StringComparison.Ordinal) ? "srt" : "vtt";

        return "plain";
    }

    private static bool LooksLikeTimedTranscript(string rawContent)
    {
        return rawContent.Contains("-->", StringComparison.Ordinal)
               || rawContent.Contains("WEBVTT", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripLeadingInlineTimestamps(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var timestampPrefix = new System.Text.RegularExpressions.Regex(
            @"^(?<timestamp>(?:\d{1,2}:)?\d{1,2}:\d{2}(?:[\.,]\d{1,3})?)\s+",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        var lines = content.Split(['\r', '\n'], StringSplitOptions.None);
        var cleanedLines = new List<string>(lines.Length);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                cleanedLines.Add(string.Empty);
                continue;
            }

            var cleaned = timestampPrefix.Replace(line, string.Empty, 1).Trim();
            cleanedLines.Add(cleaned);
        }

        return string.Join(Environment.NewLine, cleanedLines);
    }

    private static int CountWords(string text)
        => string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
