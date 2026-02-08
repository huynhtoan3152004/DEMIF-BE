using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Admin Lesson Service - CRUD lessons
/// </summary>
public class AdminLessonService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;

    public AdminLessonService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
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
    /// Tạo lesson mới
    /// </summary>
    public async Task<Result<Guid>> CreateAsync(CreateUpdateLessonRequest request, CancellationToken cancellationToken = default)
    {
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
            DictationTemplate = request.DictationTemplate,
            IsPremiumOnly = request.IsPremiumOnly,
            DisplayOrder = request.DisplayOrder,
            Tags = request.Tags,
            Status = request.Status
        };

        await _lessonRepository.AddAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(lesson.Id);
    }

    /// <summary>
    /// Cập nhật lesson
    /// </summary>
    public async Task<Result> UpdateAsync(Guid id, CreateUpdateLessonRequest request, CancellationToken cancellationToken = default)
    {
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
        lesson.DurationSeconds = request.DurationSeconds;
        lesson.ThumbnailUrl = request.ThumbnailUrl;
        lesson.FullTranscript = request.FullTranscript;
        lesson.DictationTemplate = request.DictationTemplate;
        lesson.IsPremiumOnly = request.IsPremiumOnly;
        lesson.DisplayOrder = request.DisplayOrder;
        lesson.Tags = request.Tags;
        lesson.Status = request.Status;
        lesson.UpdatedAt = DateTime.UtcNow;

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
            DictationTemplate = lesson.DictationTemplate,
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
