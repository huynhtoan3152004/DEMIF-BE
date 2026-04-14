using Demif.Application.Abstractions.Persistence;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Lessons.Tracking;

/// <summary>
/// XP Scoring Service — tính điểm kinh nghiệm khi user hoàn thành segment/lesson.
/// 
/// Formula:
///   - 1 XP per segment (idempotent — chỉ cộng 1 lần per segment)
///   - 10 XP bonus khi hoàn thành toàn bộ bài (tất cả segments đã check)
///
/// Cập nhật: UserProgress.TotalPoints + UserAnalytics.TotalPoints
/// Leaderboard sort theo Streak → TotalPoints nên XP ngay lập tức ảnh hưởng ranking.
/// </summary>
public class XpService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<XpService> _logger;

    private const int SegmentXp = 1;
    private const int LessonCompletionBonusXp = 10;

    public XpService(IApplicationDbContext dbContext, ILogger<XpService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Cộng XP khi user hoàn thành 1 segment. Idempotent — segment đã có exercise thì skip.
    /// </summary>
    public async Task<int> AwardSegmentXpAsync(
        Guid userId, Guid lessonId, int segmentIndex, CancellationToken cancellationToken = default)
    {
        // Check if this segment already has XP awarded (exercise exists = already awarded)
        var alreadyExists = await _dbContext.UserExercises
            .AnyAsync(e => e.UserId == userId
                        && e.LessonId == lessonId
                        && e.SegmentIndex == segmentIndex
                        && e.ExerciseType == ExerciseType.Dictation
                        && e.Attempts > 1, // Attempts > 1 means re-attempt, XP already given
                cancellationToken);

        if (alreadyExists)
        {
            _logger.LogDebug("XP already awarded for user {UserId}, lesson {LessonId}, segment {Segment}",
                userId, lessonId, segmentIndex);
            return 0;
        }

        await AddXpAsync(userId, SegmentXp, cancellationToken);

        _logger.LogInformation(
            "Awarded {Xp} XP to user {UserId} for segment {Segment} of lesson {LessonId}",
            SegmentXp, userId, segmentIndex, lessonId);

        // Check if lesson is now fully completed
        var bonusXp = await TryAwardLessonCompletionAsync(userId, lessonId, cancellationToken);

        return SegmentXp + bonusXp;
    }

    /// <summary>
    /// Cộng XP khi submit toàn bài dictation (SegmentIndex == null trong UserExercise).
    /// XP = score / 10 (0-10 XP tùy độ chính xác) + lesson completion bonus nếu đạt.
    /// </summary>
    public async Task<int> AwardDictationSubmitXpAsync(
        Guid userId, Guid lessonId, int score, CancellationToken cancellationToken = default)
    {
        var xp = Math.Max(1, score / 10); // Minimum 1 XP for attempting
        await AddXpAsync(userId, xp, cancellationToken);

        _logger.LogInformation(
            "Awarded {Xp} XP to user {UserId} for dictation submit (score={Score}) on lesson {LessonId}",
            xp, userId, score, lessonId);

        return xp;
    }

    /// <summary>
    /// Check nếu tất cả segments đã hoàn thành → cộng bonus 10 XP.
    /// </summary>
    private async Task<int> TryAwardLessonCompletionAsync(
        Guid userId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var lesson = await _dbContext.Lessons
                .Where(l => l.Id == lessonId)
                .Select(l => new { l.TimedTranscript })
                .FirstOrDefaultAsync(cancellationToken);

            if (lesson?.TimedTranscript is null) return 0;

            int totalSegments;
            try
            {
                var segments = System.Text.Json.JsonSerializer.Deserialize<List<TimedSegment>>(
                    lesson.TimedTranscript,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                totalSegments = segments?.Count ?? 0;
            }
            catch { return 0; }

            if (totalSegments == 0) return 0;

            var completedCount = await _dbContext.UserExercises
                .CountAsync(e => e.UserId == userId
                              && e.LessonId == lessonId
                              && e.ExerciseType == ExerciseType.Dictation
                              && e.SegmentIndex != null,
                    cancellationToken);

            if (completedCount < totalSegments) return 0;

            // Check if bonus already awarded via tracker
            var tracker = await _dbContext.UserLessonTrackers
                .FirstOrDefaultAsync(t => t.UserId == userId && t.LessonId == lessonId, cancellationToken);

            if (tracker?.Status == LessonProgressStatus.Completed)
                return 0; // Already completed, bonus already given

            // Mark as completed + award bonus
            if (tracker is null)
            {
                tracker = new UserLessonTracker
                {
                    UserId = userId,
                    LessonId = lessonId,
                    Status = LessonProgressStatus.Completed,
                    LastSegmentIndex = totalSegments - 1,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };
                _dbContext.UserLessonTrackers.Add(tracker);
            }
            else
            {
                tracker.Status = LessonProgressStatus.Completed;
                tracker.CompletedAt = DateTime.UtcNow;
                tracker.LastSegmentIndex = totalSegments - 1;
            }

            await AddXpAsync(userId, LessonCompletionBonusXp, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "🎉 Awarded {Xp} XP BONUS to user {UserId} for completing ALL segments of lesson {LessonId}",
                LessonCompletionBonusXp, userId, lessonId);

            return LessonCompletionBonusXp;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check lesson completion bonus for user {UserId}, lesson {LessonId}",
                userId, lessonId);
            return 0;
        }
    }

    /// <summary>
    /// Cộng XP vào UserProgress.TotalPoints + UserAnalytics.TotalPoints.
    /// Tạo UserProgress nếu chưa tồn tại.
    /// </summary>
    private async Task AddXpAsync(Guid userId, int xp, CancellationToken cancellationToken)
    {
        // UserProgress
        var progress = await _dbContext.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (progress is null)
        {
            progress = new UserProgress
            {
                UserId = userId,
                TotalPoints = xp,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.UserProgresses.Add(progress);
        }
        else
        {
            progress.TotalPoints += xp;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        // UserAnalytics
        var analytics = await _dbContext.UserAnalytics
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (analytics is not null)
        {
            analytics.TotalPoints += xp;
            analytics.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
