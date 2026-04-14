using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Lessons.Tracking;

/// <summary>
/// Trả danh sách segments user đã hoàn thành trong 1 lesson cụ thể.
/// Dựa trên UserExercise (per-segment) + UserLessonTracker (overall status).
/// </summary>
public class GetCompletedSegmentsService
{
    private readonly IApplicationDbContext _dbContext;

    public GetCompletedSegmentsService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<LessonProgressResponse>> ExecuteAsync(
        Guid userId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        var lessonExists = await _dbContext.Lessons
            .AnyAsync(l => l.Id == lessonId, cancellationToken);
        if (!lessonExists)
            return Result.Failure<LessonProgressResponse>(
                Error.NotFound("Không tìm thấy bài học."));

        // Get total segments count from TimedTranscript
        var lesson = await _dbContext.Lessons
            .Where(l => l.Id == lessonId)
            .Select(l => new { l.TimedTranscript, l.Title })
            .FirstAsync(cancellationToken);

        var totalSegments = 0;
        if (!string.IsNullOrWhiteSpace(lesson.TimedTranscript))
        {
            try
            {
                var segments = System.Text.Json.JsonSerializer.Deserialize<List<TimedSegment>>(
                    lesson.TimedTranscript,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                totalSegments = segments?.Count ?? 0;
            }
            catch { /* ignore parse errors */ }
        }

        // Get completed segments from UserExercise
        var completedSegments = await _dbContext.UserExercises
            .Where(e => e.UserId == userId
                     && e.LessonId == lessonId
                     && e.ExerciseType == ExerciseType.Dictation
                     && e.SegmentIndex != null)
            .Select(e => new CompletedSegmentDto
            {
                SegmentIndex = e.SegmentIndex!.Value,
                BestScore = e.Score,
                Attempts = e.Attempts,
                LastAttemptAt = e.CompletedAt
            })
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync(cancellationToken);

        // Get overall tracker
        var tracker = await _dbContext.UserLessonTrackers
            .Where(t => t.UserId == userId && t.LessonId == lessonId)
            .Select(t => new { t.Status, t.LastSegmentIndex, t.StartedAt, t.CompletedAt })
            .FirstOrDefaultAsync(cancellationToken);

        var completedCount = completedSegments.Count;
        var progressPercent = totalSegments > 0
            ? Math.Round((double)completedCount / totalSegments * 100, 1)
            : 0;

        return Result.Success(new LessonProgressResponse
        {
            LessonId = lessonId,
            LessonTitle = lesson.Title,
            TotalSegments = totalSegments,
            CompletedCount = completedCount,
            ProgressPercent = progressPercent,
            Status = tracker?.Status.ToString() ?? "NotStarted",
            LastSegmentIndex = tracker?.LastSegmentIndex ?? 0,
            StartedAt = tracker?.StartedAt,
            CompletedAt = tracker?.CompletedAt,
            CompletedSegments = completedSegments
        });
    }
}

public class CompletedSegmentDto
{
    public int SegmentIndex { get; set; }
    public int BestScore { get; set; }
    public int Attempts { get; set; }
    public DateTime LastAttemptAt { get; set; }
}

public class LessonProgressResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public int TotalSegments { get; set; }
    public int CompletedCount { get; set; }
    public double ProgressPercent { get; set; }
    public string Status { get; set; } = "NotStarted";
    public int LastSegmentIndex { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<CompletedSegmentDto> CompletedSegments { get; set; } = new();
}
