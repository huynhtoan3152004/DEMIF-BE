using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Application.Features.Lessons;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Admin Lesson Service — CRUD + Hybrid Dictation Workflow.
/// 
/// Luồng chính (Hybrid):
///   1. QuickCreateAsync / YouTubeLessonService → Tạo draft + auto-gen DictationTemplates
///   2. UpdateDictationTemplatesAsync            → Mod sửa lỗ hổng thủ công từ FE
///   3. RegenerateTemplatesAsync                 → Reset lại bản auto-gen nếu cần
///   4. AdminTranscriptService.UpdateStatusAsync  → Publish khi sẵn sàng
///   
/// [OBSOLETE] CreateAsync / UpdateAsync: Luồng cũ (15+ fields JSON), giữ backward compat.
/// </summary>
public class AdminLessonService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<AdminLessonService> _logger;
    private readonly IValidator<UpdateLessonMetadataRequest> _validator;
    private readonly ICacheService _cacheService;

    public AdminLessonService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext,
        ILogger<AdminLessonService> logger,
        IValidator<UpdateLessonMetadataRequest> validator,
        ICacheService cacheService)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
        _logger = logger;
        _validator = validator;
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

    private static readonly JsonSerializerOptions DictationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Lấy tất cả lessons với pagination (admin - không filter premium)
    /// </summary>
    public async Task<Result<AdminLessonsResponse>> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        string? status = null,
        Level? level = null,
        LessonType? type = null,
        string? category = null,
        string? mediaType = null,
        string? tag = null,
        string? search = null,
        bool? isPremiumOnly = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _lessonRepository.GetPaginatedAsync(
            page,
            pageSize,
            level: level,
            type: type,
            category: category,
            mediaType: mediaType,
            tag: tag,
            search: search,
            isPremiumOnly: isPremiumOnly,
            status: status,
            cancellationToken: cancellationToken);

        return Result.Success(new AdminLessonsResponse
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// Lấy lesson theo ID
    /// </summary>
    public async Task<Result<AdminLessonDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id, cancellationToken);
        if (lesson is null)
        {
            return Result.Failure<AdminLessonDto>(Error.NotFound("Không tìm thấy bài học."));
        }

        return Result.Success(MapToDto(lesson));
    }

    /// <summary>
    /// Cập nhật thông tin cơ bản (Metadata) của lesson.
    /// Không còn tự auto-generate Transcript hay Templates (đã tách riêng endpoint).
    /// </summary>
    public async Task<Result> UpdateAsync(Guid id, UpdateLessonMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
            return Result.Failure(Error.Validation(errors));
        }
        var lesson = await _lessonRepository.GetByIdAsync(id, cancellationToken);
        if (lesson is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy bài học."));
        }

        lesson.Title = request.Title;
        lesson.Description = request.Description;
        lesson.LessonType = request.LessonType;
        lesson.Level = request.Level;
        lesson.Category = request.Category;
        lesson.AudioUrl = request.AudioUrl;
        lesson.MediaUrl = request.MediaUrl;
        lesson.MediaType = request.MediaType;
        lesson.ThumbnailUrl = request.ThumbnailUrl;
        lesson.IsPremiumOnly = request.IsPremiumOnly;
        lesson.DisplayOrder = request.DisplayOrder;
        lesson.Tags = request.Tags;
        lesson.UpdatedAt = DateTime.UtcNow;

        await _lessonRepository.UpdateAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await InvalidateLessonCacheAsync(id);

        return Result.Success();
    }

    /// <summary>
    /// Xóa lesson (soft delete - set status = archived)
    /// </summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id, cancellationToken);
        if (lesson is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy bài học."));
        }

        lesson.Status = "archived";
        lesson.UpdatedAt = DateTime.UtcNow;

        await _lessonRepository.UpdateAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await InvalidateLessonCacheAsync(id);

        return Result.Success();
    }

    /// <summary>
    /// Re-generate DictationTemplates cho lesson hiện có (admin trigger thủ công)
    /// </summary>
    public async Task<Result> RegenerateTemplatesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id, cancellationToken);
        if (lesson is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy bài học."));
        }

        if (string.IsNullOrWhiteSpace(lesson.FullTranscript))
        {
            return Result.Failure(Error.Validation("Bài học chưa có FullTranscript, không thể generate template."));
        }

        GenerateDictationData(lesson, null);
        lesson.UpdatedAt = DateTime.UtcNow;

        await _lessonRepository.UpdateAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await InvalidateLessonCacheAsync(id);

        _logger.LogInformation("Manually re-generated DictationTemplates for lesson {LessonId}", id);
        return Result.Success();
    }

    /// <summary>
    /// Logic chính: generate TimedTranscript (nếu chưa có) → generate DictationTemplates cho 4 levels
    /// </summary>
    private void GenerateDictationData(Lesson lesson, string? providedTimedTranscript)
    {
        try
        {
            // Bước 1: TimedTranscript
            if (!string.IsNullOrWhiteSpace(providedTimedTranscript))
            {
                // Admin cung cấp sẵn (VD: từ YouTube VTT)
                lesson.TimedTranscript = providedTimedTranscript;
            }
            else if (!string.IsNullOrWhiteSpace(lesson.FullTranscript) && lesson.DurationSeconds > 0)
            {
                // Auto-generate từ FullTranscript + DurationSeconds
                lesson.TimedTranscript = DictationTemplateGenerator.GenerateTimedTranscript(
                    lesson.FullTranscript, lesson.DurationSeconds);
            }

            // Bước 2: DictationTemplates cho tất cả levels
            if (!string.IsNullOrWhiteSpace(lesson.TimedTranscript))
            {
                lesson.DictationTemplates = DictationTemplateGenerator.GenerateAllTemplates(lesson.TimedTranscript);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate DictationTemplates for lesson '{Title}'", lesson.Title);
            // Không throw — lesson vẫn tạo được, chỉ không có templates
        }
    }

    /// <summary>
    /// Cho phép Moderator tự custom lại lỗ hổng (DictationTemplates)
    /// Ghi đè trực tiếp cữ liệu nhận từ Frontend vào Database.
    /// </summary>
    public async Task<Result> UpdateDictationTemplatesAsync(
        Guid id, UpdateDictationTemplatesRequest request, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id, cancellationToken);
        if (lesson is null)
            return Result.Failure(Error.NotFound("Bài học không tồn tại."));

        if (string.IsNullOrWhiteSpace(request.DictationTemplatesJson))
            return Result.Failure(Error.Validation("DictationTemplatesJson không được để trống."));

        try
        {
            var normalizedTemplates = ParseDictationTemplates(request.DictationTemplatesJson);
            lesson.DictationTemplates = JsonSerializer.Serialize(normalizedTemplates, DictationJsonOptions);
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Validation($"DictationTemplatesJson không hợp lệ: {ex.Message}"));
        }

        await _lessonRepository.UpdateAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await InvalidateLessonCacheAsync(id);

        _logger.LogInformation("Moderator updated manually customized DictationTemplates for lesson {LessonId}", id);
        return Result.Success();
    }

    public static Dictionary<string, DictationTemplate> ParseDictationTemplates(string dictationTemplatesJson)
    {
        if (string.IsNullOrWhiteSpace(dictationTemplatesJson))
            throw new ArgumentException("DictationTemplatesJson không được để trống.");

        var node = JsonNode.Parse(dictationTemplatesJson)
                   ?? throw new ArgumentException("DictationTemplatesJson phải là JSON object hoặc array.");

        var templates = node switch
        {
            JsonArray array => ParseTemplateArray(array),
            JsonObject obj => obj["segments"] is not null || obj["level"] is not null
                ? ParseSingleTemplateObject(obj)
                : ParseTemplateObject(obj),
            _ => throw new ArgumentException("DictationTemplatesJson phải là JSON object hoặc array.")
        };

        if (templates.Count == 0)
            throw new ArgumentException("DictationTemplatesJson không chứa level nào hợp lệ.");

        return templates
            .OrderBy(entry => GetLevelOrder(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, DictationTemplate> ParseTemplateObject(JsonObject root)
    {
        var templates = new Dictionary<string, DictationTemplate>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root)
        {
            if (property.Value is not JsonObject templateObject)
                continue;

            var template = ReadTemplate(property.Key, templateObject);
            templates[template.Level] = template;
        }

        return templates;
    }

    private static Dictionary<string, DictationTemplate> ParseSingleTemplateObject(JsonObject root)
    {
        var singleTemplate = ReadTemplate(null, root);
        return new Dictionary<string, DictationTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            [singleTemplate.Level] = singleTemplate
        };
    }

    private static Dictionary<string, DictationTemplate> ParseTemplateArray(JsonArray root)
    {
        var templates = new Dictionary<string, DictationTemplate>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in root)
        {
            if (node is not JsonObject templateObject)
                continue;

            var template = ReadTemplate(null, templateObject);
            ValidateTemplate(template);
            templates[template.Level] = template;
        }

        return templates;
    }

    private static DictationTemplate ReadTemplate(string? fallbackLevelKey, JsonObject element)
    {
        var body = JsonSerializer.Deserialize<DictationTemplateBody>(element.ToJsonString(), DictationJsonOptions)
                   ?? throw new ArgumentException("Không thể parse một template.");

        var level = ReadLevelString(element, fallbackLevelKey);
        if (string.IsNullOrWhiteSpace(level))
            throw new ArgumentException("Mỗi template phải có trường 'level'.");

        return new DictationTemplate
        {
            Level = level,
            BlankPercentage = body.BlankPercentage,
            Segments = body.Segments ?? new List<DictationSegment>(),
            TotalBlanks = body.TotalBlanks,
            TotalWords = body.TotalWords
        };
    }

    private static string ReadLevelString(JsonObject element, string? fallbackLevelKey)
    {
        var rawJson = element.ToJsonString();
        var match = System.Text.RegularExpressions.Regex.Match(
            rawJson,
            "\"level\"\\s*:\\s*(?<value>\\d+|\"[^\"]+\")",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        if (match.Success)
        {
            var rawLevel = match.Groups["value"].Value.Trim();
            if (rawLevel.StartsWith('"') && rawLevel.EndsWith('"') && rawLevel.Length >= 2)
            {
                rawLevel = rawLevel[1..^1];
            }

            if (int.TryParse(rawLevel, out var numericLevel))
            {
                return numericLevel switch
                {
                    0 => Level.Beginner.ToString(),
                    1 => Level.Intermediate.ToString(),
                    2 => Level.Advanced.ToString(),
                    3 => Level.Expert.ToString(),
                    _ when Enum.IsDefined(typeof(Level), numericLevel) => ((Level)numericLevel).ToString(),
                    _ => string.Empty
                };
            }

            return NormalizeLevelName(rawLevel);
        }

        return NormalizeLevelName(fallbackLevelKey);
    }

    private static string NormalizeLevelName(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return string.Empty;

        if (Enum.TryParse<Level>(rawValue, true, out var parsedLevel))
            return parsedLevel.ToString();

        return rawValue.Trim();
    }

    private static void ValidateTemplate(DictationTemplate template)
    {
        if (template.Segments is null || template.Segments.Count == 0)
            throw new ArgumentException($"Template level '{template.Level}' phải có ít nhất một segment.");

        foreach (var segment in template.Segments)
        {
            if (segment.Words is null || segment.Words.Count == 0)
                throw new ArgumentException($"Template level '{template.Level}' có segment thiếu 'words'.");
        }
    }

    private static int GetLevelOrder(string level)
    {
        return Enum.TryParse<Level>(level, true, out var parsedLevel)
            ? (int)parsedLevel
            : int.MaxValue;
    }

    private sealed class DictationTemplateBody
    {
        public int BlankPercentage { get; set; }
        public List<DictationSegment>? Segments { get; set; }
        public int TotalBlanks { get; set; }
        public int TotalWords { get; set; }
    }

    /// <summary>
    /// Quick-create lesson — admin chỉ cần Title + paste SRT/VTT/plain transcript.
    /// </summary>
    public async Task<Result<QuickCreateLessonResponse>> QuickCreateAsync(
        QuickCreateLessonRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<QuickCreateLessonResponse>(Error.Validation("Title không được để trống."));

        if (string.IsNullOrWhiteSpace(request.Transcript))
            return Result.Failure<QuickCreateLessonResponse>(Error.Validation("Transcript không được để trống."));

        // Parse transcript theo format hoặc auto-detect.
        List<TimedSegment> segments;
        ParsedTranscriptDto transcriptPayload;
        try
        {
            var parseResult = AdminTranscriptService.ParseTranscriptPayload(
                request.Transcript,
                request.Format,
                request.DurationSeconds);

            transcriptPayload = parseResult.Transcript;
            segments = parseResult.Segments
                .Select(segment => new TimedSegment
                {
                    StartTime = segment.StartTime,
                    EndTime = segment.EndTime,
                    Text = segment.Text
                })
                .ToList();
        }
        catch (Exception ex)
        {
            return Result.Failure<QuickCreateLessonResponse>(
                Error.Validation($"Không thể parse transcript: {ex.Message}"));
        }

        if (segments.Count == 0)
            return Result.Failure<QuickCreateLessonResponse>(
                Error.Validation("Transcript không chứa segment nào. Kiểm tra lại format."));

        var timedTranscriptJson = System.Text.Json.JsonSerializer.Serialize(segments,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        var fullTranscript = transcriptPayload.FullTranscript;
        var duration = request.DurationSeconds
            ?? (int)Math.Ceiling(segments.Max(s => s.EndTime));

        // Auto-detect mediaType từ URL
        var mediaType = request.MediaType;
        if (string.IsNullOrWhiteSpace(mediaType) && !string.IsNullOrWhiteSpace(request.MediaUrl))
        {
            mediaType = request.MediaUrl.Contains("youtube.com") || request.MediaUrl.Contains("youtu.be")
                ? "youtube" : "audio";
        }

        var lesson = new Lesson
        {
            Title = request.Title,
            Description = request.Description,
            LessonType = request.LessonType,
            Level = request.Level,
            Category = request.Category,
            AudioUrl = request.MediaUrl ?? string.Empty,
            MediaUrl = request.MediaUrl,
            MediaType = mediaType,
            DurationSeconds = duration,
            ThumbnailUrl = request.ThumbnailUrl,
            FullTranscript = fullTranscript,
            TimedTranscript = timedTranscriptJson,
            IsPremiumOnly = request.IsPremiumOnly,
            DisplayOrder = request.DisplayOrder,
            Tags = request.Tags,
            Status = "draft"
        };

        // Auto-generate DictationTemplates cho 4 levels
        var hasTemplates = false;
        try
        {
            lesson.DictationTemplates = DictationTemplateGenerator.GenerateAllTemplates(timedTranscriptJson);
            hasTemplates = !string.IsNullOrWhiteSpace(lesson.DictationTemplates);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate DictationTemplates for quick-create '{Title}'", request.Title);
        }

        await _lessonRepository.AddAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var wordCount = segments.Sum(s =>
            s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

        _logger.LogInformation(
            "Quick-created lesson '{Title}' (Id: {LessonId}): {Segments} segments, {Words} words, templates={HasTemplates}",
            lesson.Title, lesson.Id, segments.Count, wordCount, hasTemplates);

        return Result.Success(new QuickCreateLessonResponse
        {
            LessonId = lesson.Id,
            Title = lesson.Title,
            Status = lesson.Status,
            SegmentCount = segments.Count,
            WordCount = wordCount,
            DurationSeconds = duration,
            HasDictationTemplates = hasTemplates,
            Transcript = transcriptPayload,
            Message = $"✅ Tạo bài '{lesson.Title}' thành công: {segments.Count} segments, {wordCount} từ."
                      + (hasTemplates ? " Đã generate DictationTemplates cho 4 levels." : "")
                      + " Status = draft → PATCH /status = published khi sẵn sàng."
        });
    }

    private static AdminLessonDto MapToDto(Lesson lesson)
    {
        var mediaType = lesson.MediaType;
        var isYouTube = string.Equals(mediaType, "youtube", StringComparison.OrdinalIgnoreCase);

        return new AdminLessonDto
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            LessonType = lesson.LessonType.ToString(),
            Level = lesson.Level.ToString(),
            Category = lesson.Category,
            AudioUrl = lesson.AudioUrl,
            MediaUrl = lesson.MediaUrl,
            MediaType = lesson.MediaType ?? "audio",
            VideoId = isYouTube ? ExtractYouTubeVideoId(lesson.MediaUrl) : null,
            EmbedUrl = isYouTube ? lesson.MediaUrl : null,
            DurationSeconds = lesson.DurationSeconds,
            ThumbnailUrl = lesson.ThumbnailUrl,
            FullTranscript = lesson.FullTranscript,
            TimedTranscript = lesson.TimedTranscript,
            HasDictationTemplates = !string.IsNullOrWhiteSpace(lesson.DictationTemplates),
            IsPremiumOnly = lesson.IsPremiumOnly,
            DisplayOrder = lesson.DisplayOrder,
            Tags = lesson.Tags,
            Status = lesson.Status,
            CompletionsCount = lesson.CompletionsCount,
            AvgScore = lesson.AvgScore,
            CreatedAt = lesson.CreatedAt,
            UpdatedAt = lesson.UpdatedAt
        };
    }

    /// <summary>
    /// Extract YouTube video ID from embed URL (https://www.youtube.com/embed/{videoId}).
    /// </summary>
    private static string? ExtractYouTubeVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(url, @"(?:embed|v|vi)[/=]([a-zA-Z0-9_-]{11})");
        return match.Success ? match.Groups[1].Value : null;
    }
}

/// <summary>
/// Response cho admin lessons list
/// </summary>
public class AdminLessonsResponse
{
    public List<AdminLessonDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
