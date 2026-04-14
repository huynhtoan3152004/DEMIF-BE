using System.Text.Json;
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
        // 1. Load lesson để lấy metadata + số segment
        var lesson = await _dbContext.Lessons
            .Where(l => l.Id == lessonId)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.Level,
                l.TimedTranscript
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (lesson is null)
            return Result.Failure<SyncProgressResponse>(Error.NotFound("Lesson.NotFound", "Không tìm thấy bài học này."));

        var totalSegments = GetTotalSegments(lesson.TimedTranscript);
        if (totalSegments > 0 && request.SegmentIndex >= totalSegments)
        {
            return Result.Failure<SyncProgressResponse>(
                Error.Validation($"SegmentIndex '{request.SegmentIndex}' không hợp lệ. Bài này chỉ có {totalSegments} segment(s)."));
        }

        if (request.SegmentIndex < 0)
        {
            return Result.Failure<SyncProgressResponse>(
                Error.Validation("SegmentIndex phải lớn hơn hoặc bằng 0."));
        }

        var completedSegmentIndexesQuery = _dbContext.UserExercises
            .Where(e => e.UserId == userId
                     && e.LessonId == lessonId
                     && e.SegmentIndex != null)
            .Select(e => e.SegmentIndex!.Value)
            .Distinct();

        var completedSegmentIndexes = await completedSegmentIndexesQuery
            .OrderBy(index => index)
            .ToListAsync(cancellationToken);

        var tracker = await _dbContext.UserLessonTrackers
            .FirstOrDefaultAsync(t => t.UserId == userId && t.LessonId == lessonId, cancellationToken);

        var inferredStatus = request.IsCompleted
            ? LessonProgressStatus.Completed
            : request.SegmentIndex <= 0
                ? LessonProgressStatus.Started
                : LessonProgressStatus.InProgress;

        if (tracker == null)
        {
            tracker = new UserLessonTracker
            {
                UserId = userId,
                LessonId = lessonId,
                Status = inferredStatus,
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
            else if (!request.IsCompleted && tracker.Status == LessonProgressStatus.NotStarted)
            {
                tracker.Status = inferredStatus;
                tracker.StartedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (tracker.Status == LessonProgressStatus.Completed && totalSegments > 0)
        {
            completedSegmentIndexes = Enumerable.Range(0, totalSegments).ToList();
        }

        var remainingSegmentIndexes = totalSegments > 0
            ? Enumerable.Range(0, totalSegments)
                .Except(completedSegmentIndexes)
                .ToList()
            : new List<int>();

        var nextUncompletedSegmentIndex = remainingSegmentIndexes.FirstOrDefault();
        var hasRemainingSegments = remainingSegmentIndexes.Count > 0;
        var completedCount = totalSegments > 0
            ? completedSegmentIndexes.Count
            : 0;
        var progressPercent = totalSegments > 0
            ? Math.Round((double)completedCount / totalSegments * 100, 1)
            : 0;

        return Result.Success(new SyncProgressResponse
        {
            UserId = userId,
            LessonId = lessonId,
            LessonTitle = lesson.Title,
            LessonLevel = lesson.Level.ToString(),
            Status = tracker.Status.ToString(),
            LastSegmentIndex = tracker.LastSegmentIndex,
            TotalSegments = totalSegments,
            CompletedSegments = completedCount,
            RemainingSegments = totalSegments > 0 ? totalSegments - completedCount : 0,
            ProgressPercent = progressPercent,
            IsLessonCompleted = tracker.Status == LessonProgressStatus.Completed,
            NextUncompletedSegmentIndex = hasRemainingSegments ? nextUncompletedSegmentIndex : null,
            CompletedSegmentIndexes = completedSegmentIndexes,
            RemainingSegmentIndexes = remainingSegmentIndexes
        });
    }

    private static int GetTotalSegments(string? timedTranscript)
    {
        if (string.IsNullOrWhiteSpace(timedTranscript))
            return 0;

        try
        {
            var segments = JsonSerializer.Deserialize<List<TimedSegment>>(timedTranscript, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return segments?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
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
    public string LessonTitle { get; set; } = string.Empty;
    public string LessonLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LastSegmentIndex { get; set; }
    public int TotalSegments { get; set; }
    public int CompletedSegments { get; set; }
    public int RemainingSegments { get; set; }
    public double ProgressPercent { get; set; }
    public bool IsLessonCompleted { get; set; }
    public int? NextUncompletedSegmentIndex { get; set; }
    public List<int> CompletedSegmentIndexes { get; set; } = new();
    public List<int> RemainingSegmentIndexes { get; set; } = new();
}
