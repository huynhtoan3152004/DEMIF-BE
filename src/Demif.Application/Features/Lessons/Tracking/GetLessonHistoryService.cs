using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Lessons.Tracking;

/// <summary>
/// Trả danh sách TẤT CẢ bài học mà user đã tương tác (đã làm ít nhất 1 segment hoặc có tracker).
/// Cho user biết tiến độ tổng thể qua tất cả các bài.
/// GET /api/me/lesson-history
/// </summary>
public class GetLessonHistoryService
{
    private readonly IApplicationDbContext _dbContext;

    public GetLessonHistoryService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<LessonHistoryResponse>> ExecuteAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        string? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        // Get all lesson IDs user has interacted with (via tracker OR exercises)
        var trackedLessonIds = _dbContext.UserLessonTrackers
            .Where(t => t.UserId == userId)
            .Select(t => t.LessonId);

        var exercisedLessonIds = _dbContext.UserExercises
            .Where(e => e.UserId == userId)
            .Select(e => e.LessonId);

        var allLessonIds = trackedLessonIds.Union(exercisedLessonIds).Distinct();

        // Get lesson info + join with tracker + exercise stats
        var query = from lessonId in allLessonIds
                    join lesson in _dbContext.Lessons on lessonId equals lesson.Id
                    join tracker in _dbContext.UserLessonTrackers
                        .Where(t => t.UserId == userId)
                        on lessonId equals tracker.LessonId into trackerJoin
                    from tracker in trackerJoin.DefaultIfEmpty()
                    select new
                    {
                        lesson.Id,
                        lesson.Title,
                        lesson.Level,
                        lesson.LessonType,
                        lesson.Category,
                        lesson.MediaType,
                        lesson.ThumbnailUrl,
                        lesson.DurationSeconds,
                        lesson.IsPremiumOnly,
                        TrackerStatus = tracker != null ? tracker.Status : (LessonProgressStatus?)null,
                        TrackerLastSegment = tracker != null ? tracker.LastSegmentIndex : 0,
                        TrackerStartedAt = tracker != null ? tracker.StartedAt : (DateTime?)null,
                        TrackerCompletedAt = tracker != null ? tracker.CompletedAt : (DateTime?)null
                    };

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<LessonProgressStatus>(statusFilter, true, out var filterStatus))
        {
            query = query.Where(q => q.TrackerStatus == filterStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.TrackerStartedAt ?? DateTime.MinValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Get exercise stats per lesson (completed segment count + best scores)
        var lessonIds = items.Select(i => i.Id).ToList();
        var exerciseStats = await _dbContext.UserExercises
            .Where(e => e.UserId == userId
                     && lessonIds.Contains(e.LessonId)
                     && e.ExerciseType == ExerciseType.Dictation
                     && e.SegmentIndex != null)
            .GroupBy(e => e.LessonId)
            .Select(g => new
            {
                LessonId = g.Key,
                CompletedSegments = g.Count(),
                AvgScore = (int)Math.Round(g.Average(e => (double)e.Score)),
                BestScore = g.Max(e => e.Score),
                LastActivityAt = g.Max(e => e.CompletedAt)
            })
            .ToListAsync(cancellationToken);

        var statsDict = exerciseStats.ToDictionary(s => s.LessonId);

        var result = items.Select(item =>
        {
            statsDict.TryGetValue(item.Id, out var stats);
            return new LessonHistoryItemDto
            {
                LessonId = item.Id,
                Title = item.Title,
                Level = item.Level.ToString(),
                LessonType = item.LessonType.ToString(),
                Category = item.Category,
                MediaType = item.MediaType ?? "audio",
                ThumbnailUrl = item.ThumbnailUrl,
                DurationSeconds = item.DurationSeconds,
                IsPremiumOnly = item.IsPremiumOnly,
                Status = item.TrackerStatus?.ToString() ?? "InProgress",
                LastSegmentIndex = item.TrackerLastSegment,
                CompletedSegments = stats?.CompletedSegments ?? 0,
                AvgScore = stats?.AvgScore ?? 0,
                BestScore = stats?.BestScore ?? 0,
                StartedAt = item.TrackerStartedAt,
                CompletedAt = item.TrackerCompletedAt,
                LastActivityAt = stats?.LastActivityAt ?? item.TrackerStartedAt
            };
        }).ToList();

        return Result.Success(new LessonHistoryResponse
        {
            Items = result,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }
}

public class LessonHistoryItemDto
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string MediaType { get; set; } = "audio";
    public string? ThumbnailUrl { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsPremiumOnly { get; set; }
    public string Status { get; set; } = "InProgress";
    public int LastSegmentIndex { get; set; }
    public int CompletedSegments { get; set; }
    public int AvgScore { get; set; }
    public int BestScore { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

public class LessonHistoryResponse
{
    public List<LessonHistoryItemDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
