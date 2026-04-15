using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Lessons.GetLessons;

/// <summary>
/// GetLessons Service - lấy danh sách lessons với filtering và premium access control
/// </summary>
public class GetLessonsService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ICacheService _cacheService;

    public GetLessonsService(ILessonRepository lessonRepository, ICacheService cacheService)
    {
        _lessonRepository = lessonRepository;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Lấy lessons cho user.
    /// Login user → thấy TẤT CẢ bài published (free + premium). FE tự xử lý lock/redirect.
    /// Guest → chỉ thấy bài free.
    /// </summary>
    public async Task<Result<GetLessonsResponse>> ExecuteAsync(
        GetLessonsRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Login user = full catalog, Guest = free only
        var isLoggedIn = userId.HasValue;
        var normalizedLevel = NormalizeLevelFilter(request.Level);
        var normalizedType = NormalizeLessonTypeFilter(request.Type);

        var cacheKey = $"lessons:{request.Page}:{request.PageSize}:{normalizedLevel}:{normalizedType}:{request.Category}:{request.MediaType}:{request.Tag}:{request.Search}:{isLoggedIn}";

        var response = await _cacheService.GetOrCreateAsync(cacheKey, async (ct) =>
        {
            var (items, totalCount) = await _lessonRepository.GetForUserAsync(
                request.Page,
                request.PageSize,
                isLoggedIn,
                normalizedLevel,
                normalizedType,
                request.Category,
            request.MediaType,
            request.Tag,
            request.Search,
                ct);

            return new GetLessonsResponse
            {
                Items = items.Select(MapToDto).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }, TimeSpan.FromHours(2), cancellationToken);

        return Result.Success(response!);
    }

    private static LessonDto MapToDto(Domain.Entities.Lesson lesson)
    {
        var mediaUrl = lesson.MediaUrl ?? lesson.AudioUrl;
        var mediaType = lesson.MediaType ?? "audio";
        var isYouTube = string.Equals(mediaType, "youtube", StringComparison.OrdinalIgnoreCase);

        return new LessonDto
        {
            Id = lesson.Id,
            Title = lesson.Title ?? string.Empty,
            Description = lesson.Description,
            LessonType = lesson.LessonType ?? "Dictation",
            Level = lesson.Level ?? "Beginner",
            Category = lesson.Category,
            MediaUrl = mediaUrl,
            AudioUrl = lesson.AudioUrl,
            MediaType = mediaType,
            VideoId = isYouTube ? ExtractYouTubeVideoId(mediaUrl) : null,
            EmbedUrl = isYouTube ? mediaUrl : null,
            DurationSeconds = lesson.DurationSeconds,
            ThumbnailUrl = lesson.ThumbnailUrl,
            IsPremiumOnly = lesson.IsPremiumOnly,
            CompletionsCount = lesson.CompletionsCount,
            AvgScore = lesson.AvgScore,
            Tags = lesson.Tags
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

    private static string? NormalizeLevelFilter(string? value)
    {
        return LessonValueNormalizer.TryNormalizeLevel(value, out var normalized)
            ? normalized
            : string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeLessonTypeFilter(string? value)
    {
        return LessonValueNormalizer.TryNormalizeLessonType(value, out var normalized)
            ? normalized
            : string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
