using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Admin Lesson Service - CRUD lessons
/// Tích hợp DictationTemplateGenerator: auto-generate TimedTranscript + DictationTemplates
/// </summary>
public class AdminLessonService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<AdminLessonService> _logger;
    private readonly IValidator<CreateUpdateLessonRequest> _validator;

    public AdminLessonService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext,
        ILogger<AdminLessonService> logger,
        IValidator<CreateUpdateLessonRequest> validator)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
        _logger = logger;
        _validator = validator;
    }

    /// <summary>
    /// Lấy tất cả lessons với pagination (admin - không filter premium)
    /// </summary>
    public async Task<Result<AdminLessonsResponse>> GetAllAsync(
        int page = 1,
        int pageSize = 10,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _lessonRepository.GetPaginatedAsync(
            page,
            pageSize,
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
    /// Tạo lesson mới — auto-generate TimedTranscript + DictationTemplates
    /// </summary>
    public async Task<Result<Guid>> CreateAsync(CreateUpdateLessonRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<Guid>(Error.Validation(errors));
        }
        var lesson = new Lesson
        {
            Title = request.Title,
            Description = request.Description,
            LessonType = request.LessonType,
            Level = request.Level,
            Category = request.Category,
            AudioUrl = request.AudioUrl,
            MediaUrl = request.MediaUrl,
            MediaType = request.MediaType,
            DurationSeconds = request.DurationSeconds,
            ThumbnailUrl = request.ThumbnailUrl,
            FullTranscript = request.FullTranscript,
            IsPremiumOnly = request.IsPremiumOnly,
            DisplayOrder = request.DisplayOrder,
            Tags = request.Tags,
            Status = request.Status
        };

        // Auto-generate TimedTranscript + DictationTemplates
        GenerateDictationData(lesson, request.TimedTranscript);

        await _lessonRepository.AddAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created lesson '{Title}' (Id: {LessonId}) with DictationTemplates", lesson.Title, lesson.Id);
        return Result.Success(lesson.Id);
    }

    /// <summary>
    /// Cập nhật lesson — re-generate DictationTemplates nếu transcript thay đổi
    /// </summary>
    public async Task<Result> UpdateAsync(Guid id, CreateUpdateLessonRequest request, CancellationToken cancellationToken = default)
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

        var transcriptChanged = lesson.FullTranscript != request.FullTranscript
                             || lesson.DurationSeconds != request.DurationSeconds;

        lesson.Title = request.Title;
        lesson.Description = request.Description;
        lesson.LessonType = request.LessonType;
        lesson.Level = request.Level;
        lesson.Category = request.Category;
        lesson.AudioUrl = request.AudioUrl;
        lesson.MediaUrl = request.MediaUrl;
        lesson.MediaType = request.MediaType;
        lesson.DurationSeconds = request.DurationSeconds;
        lesson.ThumbnailUrl = request.ThumbnailUrl;
        lesson.FullTranscript = request.FullTranscript;
        lesson.IsPremiumOnly = request.IsPremiumOnly;
        lesson.DisplayOrder = request.DisplayOrder;
        lesson.Tags = request.Tags;
        lesson.Status = request.Status;
        lesson.UpdatedAt = DateTime.UtcNow;

        // Re-generate nếu transcript/duration thay đổi hoặc admin cung cấp TimedTranscript mới
        if (transcriptChanged || !string.IsNullOrWhiteSpace(request.TimedTranscript))
        {
            GenerateDictationData(lesson, request.TimedTranscript);
            _logger.LogInformation("Re-generated DictationTemplates for lesson {LessonId}", id);
        }

        await _lessonRepository.UpdateAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

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

    private static AdminLessonDto MapToDto(Lesson lesson)
    {
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
            MediaType = lesson.MediaType,
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
