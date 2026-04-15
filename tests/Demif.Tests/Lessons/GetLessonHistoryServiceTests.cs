using Demif.Application.Abstractions.Persistence;
using Demif.Application.Features.Lessons.Tracking;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Demif.Tests.Lessons;

public class GetLessonHistoryServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();

    private IApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_CountsDistinctSegmentsAndCalculatesPercentFromTranscript()
    {
        var dbContext = CreateDbContext();
        dbContext.Lessons.Add(CreateLesson(segmentCount: 4));
        dbContext.UserLessonTrackers.Add(new UserLessonTracker
        {
            UserId = _userId,
            LessonId = _lessonId,
            Status = LessonProgressStatus.InProgress,
            LastSegmentIndex = 1,
            StartedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        dbContext.UserExercises.AddRange(
            new UserExercise
            {
                UserId = _userId,
                LessonId = _lessonId,
                ExerciseType = ExerciseType.Dictation,
                SegmentIndex = 0,
                Score = 90,
                Attempts = 1,
                CompletedAt = DateTime.UtcNow.AddMinutes(-9)
            },
            new UserExercise
            {
                UserId = _userId,
                LessonId = _lessonId,
                ExerciseType = ExerciseType.Dictation,
                SegmentIndex = 1,
                Score = 70,
                Attempts = 1,
                CompletedAt = DateTime.UtcNow.AddMinutes(-8)
            },
            new UserExercise
            {
                UserId = _userId,
                LessonId = _lessonId,
                ExerciseType = ExerciseType.Dictation,
                SegmentIndex = 1,
                Score = 95,
                Attempts = 2,
                CompletedAt = DateTime.UtcNow.AddMinutes(-7)
            });
        await dbContext.SaveChangesAsync();

        var service = new GetLessonHistoryService(dbContext);

        var result = await service.ExecuteAsync(_userId, page: 1, pageSize: 10);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(4, item.TotalSegments);
        Assert.Equal(2, item.CompletedSegments);
        Assert.Equal(50, item.ProgressPercent);
        Assert.Equal("InProgress", item.Status);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedLesson_OverridesToFullProgress()
    {
        var dbContext = CreateDbContext();
        dbContext.Lessons.Add(CreateLesson(segmentCount: 3));
        dbContext.UserLessonTrackers.Add(new UserLessonTracker
        {
            UserId = _userId,
            LessonId = _lessonId,
            Status = LessonProgressStatus.Completed,
            LastSegmentIndex = 2,
            CompletedAt = DateTime.UtcNow
        });
        dbContext.UserExercises.Add(new UserExercise
        {
            UserId = _userId,
            LessonId = _lessonId,
            ExerciseType = ExerciseType.Dictation,
            SegmentIndex = 0,
            Score = 100,
            Attempts = 1,
            CompletedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await dbContext.SaveChangesAsync();

        var service = new GetLessonHistoryService(dbContext);

        var result = await service.ExecuteAsync(_userId, page: 1, pageSize: 10);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(3, item.TotalSegments);
        Assert.Equal(3, item.CompletedSegments);
        Assert.Equal(100, item.ProgressPercent);
        Assert.Equal("Completed", item.Status);
    }

    private Lesson CreateLesson(int segmentCount)
    {
        var segments = Enumerable.Range(0, segmentCount)
            .Select(i => new { startTime = i * 2.0, endTime = (i + 1) * 2.0, text = $"Segment {i}" })
            .ToList();

        return new Lesson
        {
            Id = _lessonId,
            Title = "History Lesson",
            AudioUrl = "https://example.com/audio.mp3",
            FullTranscript = "test",
            TimedTranscript = System.Text.Json.JsonSerializer.Serialize(segments),
            Status = "published"
        };
    }
}
