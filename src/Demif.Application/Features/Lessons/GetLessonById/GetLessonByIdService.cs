using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Lessons.GetLessonById;

/// <summary>
/// GetLessonById Service - lấy chi tiết lesson với kiểm tra premium access
/// </summary>
public class GetLessonByIdService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;

    public GetLessonByIdService(
        ILessonRepository lessonRepository,
        IUserSubscriptionRepository subscriptionRepository)
    {
        _lessonRepository = lessonRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<GetLessonByIdResponse>> ExecuteAsync(
        Guid lessonId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null)
        {
            return Result.Failure<GetLessonByIdResponse>(Error.NotFound("Không tìm thấy bài học."));
        }

        // Kiểm tra quyền truy cập nếu lesson là premium only
        if (lesson.IsPremiumOnly)
        {
            if (!userId.HasValue)
            {
                return Result.Failure<GetLessonByIdResponse>(
                    Error.Forbidden("Bài học này yêu cầu gói Premium. Vui lòng đăng nhập."));
            }

            var hasPremiumAccess = await _subscriptionRepository.HasActiveSubscriptionAsync(userId.Value, cancellationToken);
            if (!hasPremiumAccess)
            {
                return Result.Failure<GetLessonByIdResponse>(
                    Error.Forbidden("Bài học này yêu cầu gói Premium. Vui lòng nâng cấp tài khoản."));
            }
        }

        return Result.Success(new GetLessonByIdResponse
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
            FullTranscript = lesson.FullTranscript,
            DictationTemplate = lesson.DictationTemplate,
            IsPremiumOnly = lesson.IsPremiumOnly,
            CompletionsCount = lesson.CompletionsCount,
            AvgScore = lesson.AvgScore,
            Tags = lesson.Tags,
            CreatedAt = lesson.CreatedAt
        });
    }
}
