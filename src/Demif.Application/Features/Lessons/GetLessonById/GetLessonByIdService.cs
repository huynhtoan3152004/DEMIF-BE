using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Lessons.GetLessonById;

/// <summary>
/// GetLessonById Service - lấy chi tiết lesson với kiểm tra premium access
/// </summary>
public class GetLessonByIdService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ICacheService _cacheService;

    public GetLessonByIdService(
        ILessonRepository lessonRepository,
        IUserSubscriptionRepository subscriptionRepository,
        ICacheService cacheService)
    {
        _lessonRepository = lessonRepository;
        _subscriptionRepository = subscriptionRepository;
        _cacheService = cacheService;
    }

    public async Task<Result<GetLessonByIdResponse>> ExecuteAsync(
        Guid lessonId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"lesson:{lessonId}";

        // 1. Fetch from Cache or DB
        var response = await _cacheService.GetOrCreateAsync(cacheKey, async (ct) =>
        {
            var lesson = await _lessonRepository.GetByIdAsync(lessonId, ct);
            if (lesson is null || lesson.Status != "published")
            {
                return null;
            }

            var mediaUrl = lesson.MediaUrl ?? lesson.AudioUrl;
            var mediaType = lesson.MediaType ?? "audio";
            var isYouTube = string.Equals(mediaType, "youtube", StringComparison.OrdinalIgnoreCase);

            return new GetLessonByIdResponse
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Description = lesson.Description,
                LessonType = lesson.LessonType,
                Level = lesson.Level,
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
            };
        }, TimeSpan.FromDays(1), cancellationToken);

        if (response is null)
        {
            return Result.Failure<GetLessonByIdResponse>(Error.NotFound("Không tìm thấy bài học."));
        }

        // 2. Kiểm tra quyền truy cập nếu lesson là premium only
        if (response.IsPremiumOnly)
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

        return Result.Success(response);
    }

    // Extract YouTube video ID from embed URL (https://www.youtube.com/embed/{videoId}).
    /// </summary>
    private static string? ExtractYouTubeVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(url, @"(?:embed|v|vi)[/=]([a-zA-Z0-9_-]{11})");
        return match.Success ? match.Groups[1].Value : null;
    }
}
