using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Lessons.GetDictationExercise;

/// <summary>
/// Service lấy dictation exercise cho user.
/// GET /api/lessons/{id}/dictation?level=Beginner
/// </summary>
public class GetDictationExerciseService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;

    public GetDictationExerciseService(
        ILessonRepository lessonRepository,
        IUserSubscriptionRepository subscriptionRepository)
    {
        _lessonRepository = lessonRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<DictationExerciseResponse>> ExecuteAsync(
        Guid lessonId,
        Level level,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null || lesson.Status != "published")
        {
            return Result.Failure<DictationExerciseResponse>(Error.NotFound("Không tìm thấy bài học."));
        }

        // Kiểm tra premium access (nhất quán với GetLessonsService + GetLessonByIdService)
        if (lesson.IsPremiumOnly)
        {
            if (!userId.HasValue)
            {
                return Result.Failure<DictationExerciseResponse>(
                    Error.Forbidden("Bài học này chỉ dành cho Premium. Vui lòng đăng nhập và nâng cấp tài khoản."));
            }

            var hasPremium = await _subscriptionRepository.HasActiveSubscriptionAsync(userId.Value, cancellationToken);
            if (!hasPremium)
            {
                return Result.Failure<DictationExerciseResponse>(
                    Error.Forbidden("Bài học này chỉ dành cho Premium. Vui lòng nâng cấp tài khoản."));
            }
        }

        // Kiểm tra lesson có DictationTemplates
        if (string.IsNullOrWhiteSpace(lesson.DictationTemplates))
        {
            return Result.Failure<DictationExerciseResponse>(
                Error.NotFound("Bài học chưa có Dictation template. Vui lòng liên hệ admin."));
        }

        // Lấy template cho level yêu cầu
        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates, level);
        if (template is null)
        {
            return Result.Failure<DictationExerciseResponse>(
                Error.NotFound($"Không tìm thấy template cho level '{level}'."));
        }

        // ⚠️ QUAN TRỌNG: Xóa Answer khỏi response để user không cheat
        StripAnswers(template);

        var mediaUrl = lesson.MediaUrl ?? lesson.AudioUrl;
        var mediaType = lesson.MediaType ?? "audio";
        var isYouTube = string.Equals(mediaType, "youtube", StringComparison.OrdinalIgnoreCase);

        return Result.Success(new DictationExerciseResponse
        {
            LessonId = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            MediaUrl = mediaUrl,
            AudioUrl = lesson.AudioUrl,
            MediaType = mediaType,
            VideoId = isYouTube ? ExtractYouTubeVideoId(mediaUrl) : null,
            EmbedUrl = isYouTube ? mediaUrl : null,
            DurationSeconds = lesson.DurationSeconds,
            Level = level.ToString(),
            Template = template,
            ThumbnailUrl = lesson.ThumbnailUrl
        });
    }

    /// <summary>
    /// Xóa field Answer khỏi template trước khi gửi cho frontend.
    /// User chỉ nhận được Hint + Length, không nhận được đáp án.
    /// </summary>
    private static void StripAnswers(DictationTemplate template)
    {
        foreach (var segment in template.Segments)
        {
            foreach (var word in segment.Words)
            {
                if (word.IsBlank)
                {
                    word.Answer = null; // Không gửi đáp án ra frontend
                }
            }
        }
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
/// Dictation exercise response with media info and blanked template.
/// MediaType determines the content source: "audio" (MP3) or "youtube" (YouTube embed).
/// </summary>
public class DictationExerciseResponse
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Primary media URL for the exercise player.
    /// - "audio": direct MP3/audio file URL
    /// - "youtube": YouTube embed URL
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>Legacy audio URL (use MediaUrl instead). Kept for backward compatibility.</summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Content type: "audio" | "video" | "youtube".
    /// Frontend should render the appropriate player based on this value.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// YouTube Video ID (only present when MediaType == "youtube").
    /// </summary>
    public string? VideoId { get; set; }

    /// <summary>
    /// YouTube embed URL (only present when MediaType == "youtube").
    /// Ready-to-use in an iframe src attribute.
    /// </summary>
    public string? EmbedUrl { get; set; }

    public int DurationSeconds { get; set; }
    public string Level { get; set; } = string.Empty;
    public DictationTemplate Template { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
}
