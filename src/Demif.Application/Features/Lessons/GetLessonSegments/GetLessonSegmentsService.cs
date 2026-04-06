using System.Text.Json;
using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Lessons.GetLessonSegments;

/// <summary>
/// Service lấy segments của lesson theo level config.
/// Segment có startTime/endTime từ TimedTranscript (YouTube VTT hoặc manual SRT).
/// Text của segment chỉ được trả về với Beginner level (showTranscriptBefore = true).
/// </summary>
public class GetLessonSegmentsService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ICacheService _cacheService;
    private readonly IApplicationDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Level configs — định nghĩa hành vi UI cho từng level
    private static readonly Dictionary<Level, LevelConfig> Configs = new()
    {
        [Level.Beginner] = new LevelConfig
        {
            Level = "Beginner",
            ShowTranscriptBefore = true,   // Thấy transcript khi đang gõ
            ShowTranscriptAfter = true,
            MaxReplays = -1,               // Unlimited
            ShowWordCount = true
        },
        [Level.Intermediate] = new LevelConfig
        {
            Level = "Intermediate",
            ShowTranscriptBefore = false,
            ShowTranscriptAfter = true,    // Reveal sau khi submit
            MaxReplays = 3,
            ShowWordCount = true
        },
        [Level.Advanced] = new LevelConfig
        {
            Level = "Advanced",
            ShowTranscriptBefore = false,
            ShowTranscriptAfter = false,   // Phải click "Xem đáp án" thủ công
            MaxReplays = 2,
            ShowWordCount = false
        },
        [Level.Expert] = new LevelConfig
        {
            Level = "Expert",
            ShowTranscriptBefore = false,
            ShowTranscriptAfter = false,
            MaxReplays = 1,
            ShowWordCount = false
        }
    };

    public GetLessonSegmentsService(
        ILessonRepository lessonRepository,
        IUserSubscriptionRepository subscriptionRepository,
        ICacheService cacheService,
        IApplicationDbContext dbContext)
    {
        _lessonRepository = lessonRepository;
        _subscriptionRepository = subscriptionRepository;
        _cacheService = cacheService;
        _dbContext = dbContext;
    }

    public async Task<Result<LessonSegmentsResponse>> ExecuteAsync(
        Guid lessonId,
        string levelStr,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Parse level
        if (!Enum.TryParse<Level>(levelStr, ignoreCase: true, out var level))
            return Result.Failure<LessonSegmentsResponse>(
                Error.Validation($"Level '{levelStr}' không hợp lệ. Dùng: Beginner, Intermediate, Advanced, Expert."));

        var cacheKey = $"lesson:{lessonId}:segments:level_{level}";

        var response = await _cacheService.GetOrCreateAsync(cacheKey, async (ct) =>
        {
            // Load lesson
            var lesson = await _lessonRepository.GetByIdAsync(lessonId, ct);
            if (lesson is null || lesson.Status != "published")
                return null;

            // Phải có TimedTranscript — đây là nguồn segments chính xác
            if (string.IsNullOrWhiteSpace(lesson.TimedTranscript))
                throw new InvalidOperationException("Missing TimedTranscript");

            // Parse segments từ TimedTranscript JSON
            List<TimedSegment>? segments;
            try
            {
                segments = JsonSerializer.Deserialize<List<TimedSegment>>(lesson.TimedTranscript, JsonOptions);
            }
            catch
            {
                throw new InvalidOperationException("Invalid JSON");
            }

            if (segments is null || segments.Count == 0)
                throw new InvalidOperationException("No segments");

            var config = Configs[level];

            var segmentDtos = segments.Select((seg, index) => new LessonSegmentDto
            {
                Index = index,
                StartTime = seg.StartTime,
                EndTime = seg.EndTime,
                WordCount = CountWords(seg.Text),
                // Chỉ trả text nếu level Beginner (ShowTranscriptBefore = true)
                Text = config.ShowTranscriptBefore ? seg.Text : null
            }).ToList();

            return new LessonSegmentsResponse
            {
                LessonId = lesson.Id,
                Title = lesson.Title,
                Description = lesson.Description,
                AudioUrl = lesson.MediaUrl ?? lesson.AudioUrl,
                MediaType = lesson.MediaType ?? "audio",
                DurationSeconds = lesson.DurationSeconds,
                ThumbnailUrl = lesson.ThumbnailUrl,
                IsPremiumOnly = lesson.IsPremiumOnly,
                LevelConfig = config,
                Segments = segmentDtos,
                TotalSegments = segmentDtos.Count
            };
        }, TimeSpan.FromDays(1), cancellationToken);

        if (response is null)
        {
            // Re-check why it's null by looking at DB (fallback error messages)
            var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
            if (lesson is null || lesson.Status != "published")
                return Result.Failure<LessonSegmentsResponse>(Error.NotFound("Không tìm thấy bài học."));

            if (string.IsNullOrWhiteSpace(lesson.TimedTranscript))
                return Result.Failure<LessonSegmentsResponse>(Error.NotFound("Bài học chưa có Timed Transcript. Vui lòng liên hệ admin."));

            return Result.Failure<LessonSegmentsResponse>(Error.Validation("Dữ liệu transcript không hợp lệ. Vui lòng liên hệ admin."));
        }

        // Premium check
        if (response.IsPremiumOnly)
        {
            if (!userId.HasValue)
                return Result.Failure<LessonSegmentsResponse>(
                    Error.Forbidden("Bài học này chỉ dành cho Premium. Vui lòng đăng nhập."));

            var hasPremium = await _subscriptionRepository.HasActiveSubscriptionAsync(userId.Value, cancellationToken);
            if (!hasPremium)
                return Result.Failure<LessonSegmentsResponse>(
                    Error.Forbidden("Bài học này chỉ dành cho Premium. Vui lòng nâng cấp tài khoản."));
        }

        // Merge user progress per segment (realtime, không cache)
        if (userId.HasValue)
        {
            var exerciseProgress = await _dbContext.UserExercises
                .Where(e => e.UserId == userId.Value && e.LessonId == lessonId && e.SegmentIndex != null)
                .GroupBy(e => e.SegmentIndex!.Value)
                .Select(g => new
                {
                    SegmentIndex = g.Key,
                    BestScore = g.Max(e => e.Score),
                    Attempts = g.Sum(e => e.Attempts)
                })
                .ToDictionaryAsync(x => x.SegmentIndex, x => x, cancellationToken);

            foreach (var seg in response.Segments)
            {
                if (exerciseProgress.TryGetValue(seg.Index, out var progress))
                {
                    seg.IsCompleted = true;
                    seg.BestScore = progress.BestScore;
                    seg.Attempts = progress.Attempts;
                }
                else
                {
                    seg.IsCompleted = false;
                    seg.BestScore = null;
                    seg.Attempts = null;
                }
            }

            var completedCount = exerciseProgress.Count;
            response.CompletedCount = completedCount;
            response.ProgressPercent = response.TotalSegments > 0
                ? Math.Round((decimal)completedCount / response.TotalSegments * 100, 1)
                : 0;
        }

        return Result.Success(response);
    }

    private static int CountWords(string text)
        => string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
