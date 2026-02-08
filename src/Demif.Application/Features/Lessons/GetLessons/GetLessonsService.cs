using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Lessons.GetLessons;

/// <summary>
/// GetLessons Service - lấy danh sách lessons với filtering và premium access control
/// </summary>
public class GetLessonsService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;

    public GetLessonsService(
        ILessonRepository lessonRepository,
        IUserSubscriptionRepository subscriptionRepository)
    {
        _lessonRepository = lessonRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    /// <summary>
    /// Lấy lessons cho user (có kiểm tra premium access)
    /// </summary>
    public async Task<Result<GetLessonsResponse>> ExecuteAsync(
        GetLessonsRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Kiểm tra user có premium access không
        var hasPremiumAccess = false;
        if (userId.HasValue)
        {
            hasPremiumAccess = await _subscriptionRepository.HasActiveSubscriptionAsync(userId.Value, cancellationToken);
        }

        var (items, totalCount) = await _lessonRepository.GetForUserAsync(
            request.Page,
            request.PageSize,
            hasPremiumAccess,
            request.Level,
            request.Type,
            request.Category,
            cancellationToken);

        var response = new GetLessonsResponse
        {
            Items = items.Select(MapToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return Result.Success(response);
    }

    private static LessonDto MapToDto(Domain.Entities.Lesson lesson)
    {
        return new LessonDto
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            LessonType = lesson.LessonType.ToString(),
            Level = lesson.Level.ToString(),
            Category = lesson.Category,
            MediaUrl = lesson.MediaUrl ?? lesson.AudioUrl,
            AudioUrl = lesson.AudioUrl,
            MediaType = lesson.MediaType ?? "audio",
            DurationSeconds = lesson.DurationSeconds,
            ThumbnailUrl = lesson.ThumbnailUrl,
            IsPremiumOnly = lesson.IsPremiumOnly,
            CompletionsCount = lesson.CompletionsCount,
            AvgScore = lesson.AvgScore,
            Tags = lesson.Tags
        };
    }
}
