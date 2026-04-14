using Demif.Application.Abstractions.Persistence;
using Demif.Application.Features.Lessons.Tracking;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Demif.Tests.Lessons;

/// <summary>
/// Unit tests cho XpService — XP scoring system
/// </summary>
public class XpServiceTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly XpService _xpService;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();

    public XpServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        var loggerMock = new Mock<ILogger<XpService>>();
        _xpService = new XpService(_dbContext, loggerMock.Object);
    }

    [Fact]
    public async Task AwardSegmentXp_FirstAttempt_Awards1Xp()
    {
        // Arrange — create lesson with TimedTranscript
        var lesson = CreateLesson(3);
        _dbContext.Lessons.Add(lesson);

        // Create first exercise (Attempts == 1 means first time)
        _dbContext.UserExercises.Add(new UserExercise
        {
            UserId = _userId,
            LessonId = _lessonId,
            ExerciseType = ExerciseType.Dictation,
            SegmentIndex = 0,
            Score = 85,
            Attempts = 1
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var xp = await _xpService.AwardSegmentXpAsync(_userId, _lessonId, 0);

        // Assert
        Assert.True(xp >= 1);
        var progress = await _dbContext.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == _userId);
        Assert.NotNull(progress);
        Assert.True(progress.TotalPoints >= 1);
    }

    [Fact]
    public async Task AwardSegmentXp_ReAttempt_SkipsXp()
    {
        // Arrange — segment already attempted multiple times
        _dbContext.Lessons.Add(CreateLesson(3));
        _dbContext.UserExercises.Add(new UserExercise
        {
            UserId = _userId,
            LessonId = _lessonId,
            ExerciseType = ExerciseType.Dictation,
            SegmentIndex = 0,
            Score = 85,
            Attempts = 2 // re-attempt
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var xp = await _xpService.AwardSegmentXpAsync(_userId, _lessonId, 0);

        // Assert — no XP for re-attempt
        Assert.Equal(0, xp);
    }

    [Fact]
    public async Task AwardSegmentXp_AllSegmentsCompleted_AwardsBonusXp()
    {
        // Arrange — lesson with 2 segments, both completed
        _dbContext.Lessons.Add(CreateLesson(2));
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
            SegmentIndex = 1,
            Score = 95,
            Attempts = 1
        });
        await _dbContext.SaveChangesAsync();

        // Act — award XP for segment 1 (second segment, completing the lesson)
        var xp = await _xpService.AwardSegmentXpAsync(_userId, _lessonId, 1);

        // Assert — 1 XP for segment + 10 XP bonus for lesson completion = 11
        Assert.Equal(11, xp);

        var tracker = await _dbContext.UserLessonTrackers
            .FirstOrDefaultAsync(t => t.UserId == _userId && t.LessonId == _lessonId);
        Assert.NotNull(tracker);
        Assert.Equal(LessonProgressStatus.Completed, tracker.Status);
    }

    [Fact]
    public async Task AwardSegmentXp_LessonAlreadyCompleted_NoBonusAgain()
    {
        // Arrange — lesson already marked completed
        _dbContext.Lessons.Add(CreateLesson(1));
        _dbContext.UserLessonTrackers.Add(new UserLessonTracker
        {
            UserId = _userId,
            LessonId = _lessonId,
            Status = LessonProgressStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            LastSegmentIndex = 0
        });
        _dbContext.UserExercises.Add(new UserExercise
        {
            UserId = _userId,
            LessonId = _lessonId,
            ExerciseType = ExerciseType.Dictation,
            SegmentIndex = 0,
            Score = 100,
            Attempts = 1
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var xp = await _xpService.AwardSegmentXpAsync(_userId, _lessonId, 0);

        // Assert — 1 XP segment but no bonus (already completed)
        Assert.Equal(1, xp);
    }

    [Fact]
    public async Task AwardDictationSubmitXp_HighScore_AwardsProportionalXp()
    {
        // Act
        var xp = await _xpService.AwardDictationSubmitXpAsync(_userId, _lessonId, 85);

        // Assert — 85 / 10 = 8 XP
        Assert.Equal(8, xp);

        var progress = await _dbContext.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == _userId);
        Assert.NotNull(progress);
        Assert.Equal(8, progress.TotalPoints);
    }

    [Fact]
    public async Task AwardDictationSubmitXp_LowScore_AwardsMinimum1Xp()
    {
        // Act — score is 5 so 5/10 = 0, but minimum is 1
        var xp = await _xpService.AwardDictationSubmitXpAsync(_userId, _lessonId, 5);

        // Assert — minimum 1 XP
        Assert.Equal(1, xp);
    }

    [Fact]
    public async Task AddXp_CreatesUserProgressIfNotExists()
    {
        // Verify no progress exists yet
        var before = await _dbContext.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == _userId);
        Assert.Null(before);

        // Act
        await _xpService.AwardDictationSubmitXpAsync(_userId, _lessonId, 50);

        // Assert — progress created
        var after = await _dbContext.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == _userId);
        Assert.NotNull(after);
        Assert.Equal(5, after.TotalPoints); // 50/10 = 5
    }

    [Fact]
    public async Task AddXp_AccumulatesPoints()
    {
        // Act — two separate XP awards
        await _xpService.AwardDictationSubmitXpAsync(_userId, _lessonId, 80); // 8 XP
        await _xpService.AwardDictationSubmitXpAsync(_userId, Guid.NewGuid(), 60); // 6 XP

        // Assert
        var progress = await _dbContext.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == _userId);
        Assert.NotNull(progress);
        Assert.Equal(14, progress.TotalPoints);
    }

    private Lesson CreateLesson(int segmentCount)
    {
        var segments = Enumerable.Range(0, segmentCount)
            .Select(i => new { startTime = i * 2.0, endTime = (i + 1) * 2.0, text = $"Segment {i} text here" })
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
