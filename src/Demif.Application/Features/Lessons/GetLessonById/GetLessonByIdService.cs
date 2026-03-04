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
        if (lesson is null || lesson.Status != "published")
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

        var mediaUrl = lesson.MediaUrl ?? lesson.AudioUrl;
        var mediaType = lesson.MediaType ?? "audio";
        var isYouTube = string.Equals(mediaType, "youtube", StringComparison.OrdinalIgnoreCase);

        return Result.Success(new GetLessonByIdResponse
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            LessonType = lesson.LessonType.ToString(),
            Level = lesson.Level.ToString(),
            Category = lesson.Category,
            MediaUrl = mediaUrl,
            AudioUrl = lesson.AudioUrl,
            MediaType = mediaType,
            VideoId = isYouTube ? ExtractYouTubeVideoId(mediaUrl) : null,
            EmbedUrl = isYouTube ? mediaUrl : null,
            DurationSeconds = lesson.DurationSeconds,
            ThumbnailUrl = lesson.ThumbnailUrl,
            FullTranscript = lesson.FullTranscript,
            HasDictationExercise = !string.IsNullOrWhiteSpace(lesson.DictationTemplates),
            AvailableDictationLevels = !string.IsNullOrWhiteSpace(lesson.DictationTemplates)
                ? new List<string> { "Beginner", "Intermediate", "Advanced", "Expert" }
                : null,
            IsPremiumOnly = lesson.IsPremiumOnly,
            CompletionsCount = lesson.CompletionsCount,
            AvgScore = lesson.AvgScore,
            Tags = lesson.Tags,
            CreatedAt = lesson.CreatedAt
        });
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
