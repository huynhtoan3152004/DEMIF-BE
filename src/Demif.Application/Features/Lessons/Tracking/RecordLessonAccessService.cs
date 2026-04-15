using Demif.Application.Abstractions.Persistence;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Lessons.Tracking;

public class RecordLessonAccessService
{
    private readonly IApplicationDbContext _dbContext;

    public RecordLessonAccessService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        var tracker = await _dbContext.UserLessonTrackers
            .FirstOrDefaultAsync(x => x.UserId == userId && x.LessonId == lessonId, cancellationToken);

        if (tracker is null)
        {
            _dbContext.UserLessonTrackers.Add(new UserLessonTracker
            {
                UserId = userId,
                LessonId = lessonId,
                Status = LessonProgressStatus.Started,
                LastSegmentIndex = 0,
                StartedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (tracker.Status == LessonProgressStatus.NotStarted)
        {
            tracker.Status = LessonProgressStatus.Started;
            if (tracker.StartedAt == default)
            {
                tracker.StartedAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
