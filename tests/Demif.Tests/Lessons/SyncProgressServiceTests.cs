using Demif.Application.Abstractions.Persistence;
using Demif.Application.Features.Lessons.Tracking;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Demif.Tests.Lessons;

public class SyncProgressServiceTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly SyncProgressService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();

    public SyncProgressServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _service = new SyncProgressService(_dbContext);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSnapshotWithCompletedAndRemainingSegments()
    {
        _dbContext.Lessons.Add(CreateLesson(4));
        _dbContext.UserExercises.AddRange(
            new UserExercise
            {
                UserId = _userId,
                LessonId = _lessonId,
                ExerciseType = ExerciseType.Dictation,
                SegmentIndex = 0,
                Score = 90,
                Attempts = 1
            },
            new UserExercise
            {
                UserId = _userId,
                LessonId = _lessonId,
                ExerciseType = ExerciseType.Shadowing,
                SegmentIndex = 2,
                Score = 85,
                Attempts = 1
            });

        await _dbContext.SaveChangesAsync();

        var result = await _service.ExecuteAsync(_userId, _lessonId, new SyncProgressRequest
        {
            SegmentIndex = 1,
            IsCompleted = false
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Test Lesson", result.Value.LessonTitle);
        Assert.Equal("Beginner", result.Value.LessonLevel);
        Assert.Equal(4, result.Value.TotalSegments);
        Assert.Equal(2, result.Value.CompletedSegments);
        Assert.Equal(2, result.Value.RemainingSegments);
        Assert.Equal(50, result.Value.ProgressPercent);
        Assert.Equal(1, result.Value.NextUncompletedSegmentIndex);
        Assert.Equal(new[] { 0, 2 }, result.Value.CompletedSegmentIndexes);
        Assert.Equal(new[] { 1, 3 }, result.Value.RemainingSegmentIndexes);
        Assert.Equal("InProgress", result.Value.Status);
        Assert.Equal(1, result.Value.LastSegmentIndex);
        Assert.False(result.Value.IsLessonCompleted);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidSegmentIndex_ReturnsValidationError()
    {
        _dbContext.Lessons.Add(CreateLesson(2));
        await _dbContext.SaveChangesAsync();

        var result = await _service.ExecuteAsync(_userId, _lessonId, new SyncProgressRequest
        {
            SegmentIndex = 5,
            IsCompleted = false
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedLesson_ReturnsAllSegmentsCompleted()
    {
        _dbContext.Lessons.Add(CreateLesson(3));
        await _dbContext.SaveChangesAsync();

        var result = await _service.ExecuteAsync(_userId, _lessonId, new SyncProgressRequest
        {
            SegmentIndex = 2,
            IsCompleted = true
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLessonCompleted);
        Assert.Equal(3, result.Value.CompletedSegments);
        Assert.Empty(result.Value.RemainingSegmentIndexes);
        Assert.Equal(100, result.Value.ProgressPercent);
        Assert.Equal("Completed", result.Value.Status);
    }

    private Lesson CreateLesson(int segmentCount)
    {
        var segments = Enumerable.Range(0, segmentCount)
            .Select(i => new { startTime = i * 2.0, endTime = (i + 1) * 2.0, text = $"Segment {i} text" })
            .ToList();

        return new Lesson
        {
            Id = _lessonId,
            Title = "Test Lesson",
            Level = "Beginner",
            AudioUrl = "https://example.com/audio.mp3",
            FullTranscript = "test",
            TimedTranscript = System.Text.Json.JsonSerializer.Serialize(segments),
            Status = "published"
        };
    }
}