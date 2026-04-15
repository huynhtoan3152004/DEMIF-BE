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
        var eventQuery = from accessEvent in _dbContext.LessonAccessEvents.AsNoTracking()
                         join lesson in _dbContext.Lessons.AsNoTracking() on accessEvent.LessonId equals lesson.Id
                         select new
                         {
                             accessEvent.UserId,
                             accessEvent.LessonId,
                             accessEvent.AccessType,
                             accessEvent.AccessedAt,
                             lesson.Id,
                             lesson.Title,
                             lesson.LessonType,
                             lesson.Level,
                             lesson.Category,
                             lesson.CreatedAt
                         };

        var accessEvents = await eventQuery.ToListAsync(cancellationToken);
        var trackers = await _dbContext.UserLessonTrackers.AsNoTracking().ToListAsync(cancellationToken);

        var grouped = accessEvents
            .GroupBy(x => new { x.LessonId, x.Title, x.LessonType, x.Level, x.Category, x.CreatedAt })
            .Select(group =>
            {
                var lessonTrackers = trackers.Where(x => x.LessonId == group.Key.LessonId).ToList();
                var trackerCounts = lessonTrackers.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count());
                var accessTimes = group.Select(x => x.AccessedAt).Where(x => x != default).ToList();
                var trackerTotal = lessonTrackers.Count;

                return new LessonAccessItem
                {
                    LessonId = group.Key.LessonId,
                    Title = group.Key.Title,
                    LessonType = group.Key.LessonType,
                    Level = group.Key.Level,
                    Category = group.Key.Category,
                    AccessCount = group.Count(),
                    UniqueUsers = group.Select(x => x.UserId).Distinct().Count(),
                    CompletedCount = trackerCounts.TryGetValue(Demif.Domain.Enums.LessonProgressStatus.Completed, out var completed) ? completed : 0,
                    InProgressCount = trackerCounts.TryGetValue(Demif.Domain.Enums.LessonProgressStatus.InProgress, out var inProgress) ? inProgress : 0,
                    StartedCount = trackerCounts.TryGetValue(Demif.Domain.Enums.LessonProgressStatus.Started, out var started) ? started : 0,
                    CompletionRate = trackerTotal > 0
                        ? Math.Round((decimal)(trackerCounts.TryGetValue(Demif.Domain.Enums.LessonProgressStatus.Completed, out var count) ? count : 0) / trackerTotal * 100, 1)
                        : 0,
                    FirstAccessedAt = accessTimes.Count > 0 ? accessTimes.Min() : null,
                    LastAccessedAt = accessTimes.Count > 0 ? accessTimes.Max() : null,
                    CreatedAt = group.Key.CreatedAt
                };
            })
            .OrderByDescending(x => x.AccessCount)
            .ThenByDescending(x => x.LastAccessedAt)
            .ToList();

        var totalAccessEvents = accessEvents.Count;
        var totalTrackedLessons = grouped.Count;
        var totalTrackedUsers = accessEvents.Select(x => x.UserId).Distinct().Count();
        var completedTrackers = trackers.Count(x => x.Status == LessonProgressStatus.Completed);
        var inProgressTrackers = trackers.Count(x => x.Status == LessonProgressStatus.InProgress);
        var startedTrackers = trackers.Count(x => x.Status == LessonProgressStatus.Started);
        var byAccessType = accessEvents
            .GroupBy(x => x.AccessType)
            .Select(x => new StatCountItem { Key = x.Key, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

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
            ByStatus = byStatus,
            ByAccessType = byAccessType
        });
    }
}
