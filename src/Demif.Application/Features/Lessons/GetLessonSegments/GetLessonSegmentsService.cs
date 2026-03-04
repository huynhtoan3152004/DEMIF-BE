using System.Text.Json;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

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
        IUserSubscriptionRepository subscriptionRepository)
    {
        _lessonRepository = lessonRepository;
        _subscriptionRepository = subscriptionRepository;
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

        // Load lesson
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null || lesson.Status != "published")
            return Result.Failure<LessonSegmentsResponse>(Error.NotFound("Không tìm thấy bài học."));

        // Premium check
        if (lesson.IsPremiumOnly)
        {
            if (!userId.HasValue)
                return Result.Failure<LessonSegmentsResponse>(
                    Error.Forbidden("Bài học này chỉ dành cho Premium. Vui lòng đăng nhập."));

            var hasPremium = await _subscriptionRepository.HasActiveSubscriptionAsync(userId.Value, cancellationToken);
            if (!hasPremium)
                return Result.Failure<LessonSegmentsResponse>(
                    Error.Forbidden("Bài học này chỉ dành cho Premium. Vui lòng nâng cấp tài khoản."));
        }

        // Phải có TimedTranscript — đây là nguồn segments chính xác
        if (string.IsNullOrWhiteSpace(lesson.TimedTranscript))
            return Result.Failure<LessonSegmentsResponse>(
                Error.NotFound("Bài học chưa có Timed Transcript. Vui lòng liên hệ admin."));

        // Parse segments từ TimedTranscript JSON
        List<TimedSegment>? segments;
        try
        {
            segments = JsonSerializer.Deserialize<List<TimedSegment>>(lesson.TimedTranscript, JsonOptions);
        }
        catch
        {
            return Result.Failure<LessonSegmentsResponse>(
                Error.Validation("Dữ liệu transcript không hợp lệ. Vui lòng liên hệ admin."));
        }

        if (segments is null || segments.Count == 0)
            return Result.Failure<LessonSegmentsResponse>(
                Error.NotFound("Bài học chưa có segments. Vui lòng liên hệ admin."));

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

        return Result.Success(new LessonSegmentsResponse
        {
            LessonId = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            AudioUrl = lesson.MediaUrl ?? lesson.AudioUrl,
            MediaType = lesson.MediaType ?? "audio",
            DurationSeconds = lesson.DurationSeconds,
            ThumbnailUrl = lesson.ThumbnailUrl,
            LevelConfig = config,
            Segments = segmentDtos,
            TotalSegments = segmentDtos.Count
        });
    }

    private static int CountWords(string text)
        => string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
