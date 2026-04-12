using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Lessons.Tracking;

public class SyncProgressService
{
    private readonly IApplicationDbContext _dbContext;

    public SyncProgressService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SyncProgressResponse>> ExecuteAsync(
        Guid userId, 
        Guid lessonId, 
        SyncProgressRequest request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra bài học tồn tại
        var lessonExists = await _dbContext.Lessons.AnyAsync(l => l.Id == lessonId, cancellationToken);
        if (!lessonExists)
            return Result.Failure<SyncProgressResponse>(Error.NotFound("Lesson.NotFound", "Không tìm thấy bài học này."));

        // 2. Upsert UserLessonTracker
        var tracker = await _dbContext.UserLessonTrackers
            .FirstOrDefaultAsync(t => t.UserId == userId && t.LessonId == lessonId, cancellationToken);

        if (tracker == null)
        {
            tracker = new UserLessonTracker
            {
                UserId = userId,
                LessonId = lessonId,
                Status = request.IsCompleted ? LessonProgressStatus.Completed : LessonProgressStatus.InProgress,
                LastSegmentIndex = request.SegmentIndex,
                StartedAt = DateTime.UtcNow
            };
            if (request.IsCompleted)
            {
                tracker.CompletedAt = DateTime.UtcNow;
            }
            _dbContext.UserLessonTrackers.Add(tracker);
        }
        else
        {
            tracker.LastSegmentIndex = request.SegmentIndex;
            
            if (request.IsCompleted && tracker.Status != LessonProgressStatus.Completed)
            {
                tracker.Status = LessonProgressStatus.Completed;
                tracker.CompletedAt = DateTime.UtcNow;
            }
            else if (!request.IsCompleted && tracker.Status == LessonProgressStatus.Started)
            {
                tracker.Status = LessonProgressStatus.InProgress;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new SyncProgressResponse
        {
            UserId = userId,
            LessonId = lessonId,
            Status = tracker.Status.ToString(),
            LastSegmentIndex = tracker.LastSegmentIndex
        });
    }
}

public class SyncProgressRequest
{
    public int SegmentIndex { get; set; }
    public bool IsCompleted { get; set; }
}

public class SyncProgressResponse
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int LastSegmentIndex { get; set; }
}
