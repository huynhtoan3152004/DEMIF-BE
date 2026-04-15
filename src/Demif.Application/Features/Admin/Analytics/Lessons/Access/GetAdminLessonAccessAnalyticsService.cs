using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Application.Features.Admin.Analytics;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Admin.Analytics.Lessons.Access;

public class GetAdminLessonAccessAnalyticsService
{
    private readonly IApplicationDbContext _dbContext;

    public GetAdminLessonAccessAnalyticsService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<LessonAccessAnalyticsResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var trackerQuery = from tracker in _dbContext.UserLessonTrackers.AsNoTracking()
                           join lesson in _dbContext.Lessons.AsNoTracking() on tracker.LessonId equals lesson.Id
                           select new
                           {
                               tracker.UserId,
                               tracker.LessonId,
                               tracker.Status,
                               tracker.StartedAt,
                               tracker.CompletedAt,
                               lesson.Id,
                               lesson.Title,
                               lesson.LessonType,
                               lesson.Level,
                               lesson.Category,
                               lesson.CreatedAt
                           };

        var trackers = await trackerQuery.ToListAsync(cancellationToken);

        var grouped = trackers
            .GroupBy(x => new { x.LessonId, x.Title, x.LessonType, x.Level, x.Category, x.CreatedAt })
            .Select(group =>
            {
                var statusCounts = group.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count());
                var accessTimes = group.Select(x => x.CompletedAt ?? x.StartedAt).Where(x => x != default).ToList();

                return new LessonAccessItem
                {
                    LessonId = group.Key.LessonId,
                    Title = group.Key.Title,
                    LessonType = group.Key.LessonType,
                    Level = group.Key.Level,
                    Category = group.Key.Category,
                    AccessCount = group.Count(),
                    UniqueUsers = group.Select(x => x.UserId).Distinct().Count(),
                    CompletedCount = statusCounts.TryGetValue(LessonProgressStatus.Completed, out var completed) ? completed : 0,
                    InProgressCount = statusCounts.TryGetValue(LessonProgressStatus.InProgress, out var inProgress) ? inProgress : 0,
                    StartedCount = statusCounts.TryGetValue(LessonProgressStatus.Started, out var started) ? started : 0,
                    CompletionRate = group.Count() > 0
                        ? Math.Round((decimal)(statusCounts.TryGetValue(LessonProgressStatus.Completed, out var count) ? count : 0) / group.Count() * 100, 1)
                        : 0,
                    FirstAccessedAt = accessTimes.Count > 0 ? accessTimes.Min() : null,
                    LastAccessedAt = accessTimes.Count > 0 ? accessTimes.Max() : null,
                    CreatedAt = group.Key.CreatedAt
                };
            })
            .OrderByDescending(x => x.AccessCount)
            .ThenByDescending(x => x.LastAccessedAt)
            .ToList();

        var totalAccessEvents = trackers.Count;
        var totalTrackedLessons = grouped.Count;
        var totalTrackedUsers = trackers.Select(x => x.UserId).Distinct().Count();
        var completedTrackers = trackers.Count(x => x.Status == LessonProgressStatus.Completed);
        var inProgressTrackers = trackers.Count(x => x.Status == LessonProgressStatus.InProgress);
        var startedTrackers = trackers.Count(x => x.Status == LessonProgressStatus.Started);

        var byStatus = new List<StatCountItem>
        {
            new() { Key = LessonProgressStatus.Completed.ToString(), Count = completedTrackers },
            new() { Key = LessonProgressStatus.InProgress.ToString(), Count = inProgressTrackers },
            new() { Key = LessonProgressStatus.Started.ToString(), Count = startedTrackers }
        };

        return Result.Success(new LessonAccessAnalyticsResponse
        {
            GeneratedAt = DateTime.UtcNow,
            TotalAccessEvents = totalAccessEvents,
            TotalTrackedLessons = totalTrackedLessons,
            TotalTrackedUsers = totalTrackedUsers,
            CompletedTrackers = completedTrackers,
            InProgressTrackers = inProgressTrackers,
            StartedTrackers = startedTrackers,
            TopAccessedLessons = grouped.Take(10).ToList(),
            RecentAccessedLessons = grouped
                .OrderByDescending(x => x.LastAccessedAt ?? DateTime.MinValue)
                .Take(10)
                .ToList(),
            ByStatus = byStatus
        });
    }
}
