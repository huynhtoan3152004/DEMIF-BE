using Demif.Application.Abstractions.Persistence;
using Demif.Application.Features.Lessons.Tracking;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Demif.Tests.Lessons;

/// <summary>
/// Unit tests cho GetCompletedSegmentsService — per-lesson progress tracking
/// </summary>
public class GetCompletedSegmentsServiceTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetCompletedSegmentsService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();

    public GetCompletedSegmentsServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _service = new GetCompletedSegmentsService(_dbContext);
    }

    [Fact]
    public async Task ExecuteAsync_NoExercises_ReturnsEmptyList()
    {
        // Arrange
        _dbContext.Lessons.Add(CreateLesson(5));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(_userId, _lessonId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.CompletedSegments);
        Assert.Equal(0, result.Value.CompletedCount);
        Assert.Equal(5, result.Value.TotalSegments);
        Assert.Equal(0, result.Value.ProgressPercent);
        Assert.Equal("NotStarted", result.Value.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SomeSegmentsCompleted_ReturnsCorrectProgress()
    {
        // Arrange
        _dbContext.Lessons.Add(CreateLesson(4));
        _dbContext.UserExercises.Add(new UserExercise
        {
            UserId = _userId,
            LessonId = _lessonId,
            ExerciseType = ExerciseType.Dictation,
            SegmentIndex = 0,
            Score = 90,
            Attempts = 1
        });
        _dbContext.UserExercises.Add(new UserExercise
        {
            UserId = _userId,
            LessonId = _lessonId,
            ExerciseType = ExerciseType.Dictation,
            SegmentIndex = 2,
            Score = 75,
            Attempts = 2
        });
        _dbContext.UserLessonTrackers.Add(new UserLessonTracker
        {
            UserId = _userId,
            LessonId = _lessonId,
            Status = LessonProgressStatus.InProgress,
            LastSegmentIndex = 2
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(_userId, _lessonId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.CompletedCount);
        Assert.Equal(4, result.Value.TotalSegments);
        Assert.Equal(50, result.Value.ProgressPercent);
        Assert.Equal("InProgress", result.Value.Status);
        Assert.Equal(2, result.Value.CompletedSegments.Count);
        Assert.Equal(0, result.Value.CompletedSegments[0].SegmentIndex);
        Assert.Equal(2, result.Value.CompletedSegments[1].SegmentIndex);
    }

    [Fact]
    public async Task ExecuteAsync_AllSegmentsCompleted_Shows100Percent()
    {
        // Arrange
        _dbContext.Lessons.Add(CreateLesson(2));
        for (int i = 0; i < 2; i++)
        {
            _dbContext.UserExercises.Add(new UserExercise
            {
                UserId = _userId,
                LessonId = _lessonId,
                ExerciseType = ExerciseType.Dictation,
                SegmentIndex = i,
                Score = 100,
                Attempts = 1
            });
        }
        _dbContext.UserLessonTrackers.Add(new UserLessonTracker
        {
            UserId = _userId,
            LessonId = _lessonId,
            Status = LessonProgressStatus.Completed,
            LastSegmentIndex = 1,
            CompletedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(_userId, _lessonId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.ProgressPercent);
        Assert.Equal("Completed", result.Value.Status);
        Assert.NotNull(result.Value.CompletedAt);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistingLesson_ReturnsNotFound()
    {
        // Act
        var result = await _service.ExecuteAsync(_userId, Guid.NewGuid());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_DifferentUser_DoesNotShowOtherUserProgress()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        _dbContext.Lessons.Add(CreateLesson(3));
        _dbContext.UserExercises.Add(new UserExercise
        {
            UserId = otherUserId,
            LessonId = _lessonId,
            ExerciseType = ExerciseType.Dictation,
            SegmentIndex = 0,
            Score = 90,
            Attempts = 1
        });
        await _dbContext.SaveChangesAsync();

        // Act — query as _userId, NOT otherUserId
        var result = await _service.ExecuteAsync(_userId, _lessonId);

        // Assert — should be empty for _userId
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.CompletedSegments);
        Assert.Equal(0, result.Value.CompletedCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShadowingExercises_AreExcluded()
    {
        // Arrange — only Shadowing exercises, not Dictation
        _dbContext.Lessons.Add(CreateLesson(2));
        _dbContext.UserExercises.Add(new UserExercise
        {
            UserId = _userId,
            LessonId = _lessonId,
            ExerciseType = ExerciseType.Shadowing,
            SegmentIndex = 0,
            Score = 80,
            Attempts = 1
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(_userId, _lessonId);

        // Assert — Shadowing not counted
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.CompletedSegments);
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
            AudioUrl = "https://example.com/audio.mp3",
            FullTranscript = "test",
            TimedTranscript = System.Text.Json.JsonSerializer.Serialize(segments),
            Status = "published"
        };
    }
}
