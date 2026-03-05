using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Me.RecordActivity;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Moq;

namespace Demif.Tests.Me;

/// <summary>
/// Unit tests for RecordActivityService
/// Covers: validation, points calc, streak logic, level progression, rolling averages
/// </summary>
public class RecordActivityServiceTests
{
    private readonly Mock<IUserProgressRepository> _progressRepoMock;
    private readonly Mock<IUserStreakRepository> _streakRepoMock;
    private readonly RecordActivityService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public RecordActivityServiceTests()
    {
        _progressRepoMock = new Mock<IUserProgressRepository>();
        _streakRepoMock = new Mock<IUserStreakRepository>();

        // Default: new user (no existing records)
        _progressRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProgress?)null);
        _streakRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);
        _progressRepoMock.Setup(r => r.UpsertAsync(It.IsAny<UserProgress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProgress p, CancellationToken _) => p);
        _streakRepoMock.Setup(r => r.UpsertAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak s, CancellationToken _) => s);

        _service = new RecordActivityService(_progressRepoMock.Object, _streakRepoMock.Object);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(200)]
    public async Task ExecuteAsync_InvalidScore_ReturnsFailure(int score)
    {
        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = score, MinutesSpent = 10 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(50)]
    public async Task ExecuteAsync_ValidScoreBoundaries_Succeeds(int score)
    {
        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = score, MinutesSpent = 5 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
    }

    // ── Points Calculation ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Score80_EarnsCorrectPoints()
    {
        // BasePoints(10) + Score(80) * 1 = 90
        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 80, MinutesSpent = 10 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(90, result.Value.PointsEarned);
        Assert.Equal(90, result.Value.TotalPoints); // new user, was 0
    }

    [Fact]
    public async Task ExecuteAsync_Score0_EarnsBasePointsOnly()
    {
        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 0, MinutesSpent = 5 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.PointsEarned); // only base points
    }

    [Fact]
    public async Task ExecuteAsync_AddsToExistingPoints()
    {
        var existing = new UserProgress { UserId = _userId, TotalPoints = 200 };
        _progressRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 50, MinutesSpent = 10 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(60, result.Value.PointsEarned); // 10 + 50
        Assert.Equal(260, result.Value.TotalPoints); // 200 + 60
    }

    // ── Level Progression ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, "Beginner")]
    [InlineData(499, "Beginner")]
    [InlineData(500, "Intermediate")]
    [InlineData(1499, "Intermediate")]
    [InlineData(1500, "Advanced")]
    [InlineData(3499, "Advanced")]
    [InlineData(3500, "Expert")]
    [InlineData(9999, "Expert")]
    public async Task ExecuteAsync_LevelThresholds_CorrectLevel(int existingPoints, string expectedLevel)
    {
        // Add just enough to start test at existingPoints (score=0 gives 10 pts, so start lower)
        if (existingPoints >= 10)
        {
            var existing = new UserProgress { UserId = _userId, TotalPoints = existingPoints - 10 };
            _progressRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
        }

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 0, MinutesSpent = 1 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedLevel, result.Value.CurrentLevel);
    }

    // ── Rolling Average ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_FirstDictation_AvgEqualScore()
    {
        UserProgress? savedProgress = null;
        _progressRepoMock.Setup(r => r.UpsertAsync(It.IsAny<UserProgress>(), It.IsAny<CancellationToken>()))
            .Callback<UserProgress, CancellationToken>((p, _) => savedProgress = p)
            .ReturnsAsync((UserProgress p, CancellationToken _) => p);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 80, MinutesSpent = 10 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedProgress);
        Assert.Equal(80m, savedProgress!.AvgDictationScore);
        Assert.Equal(1, savedProgress.DictationCompleted);
    }

    [Fact]
    public async Task ExecuteAsync_SecondDictation_RollingAverage()
    {
        // Existing: 1 dictation with avg 80
        var existing = new UserProgress
        {
            UserId = _userId,
            DictationCompleted = 1,
            AvgDictationScore = 80m
        };
        _progressRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        UserProgress? savedProgress = null;
        _progressRepoMock.Setup(r => r.UpsertAsync(It.IsAny<UserProgress>(), It.IsAny<CancellationToken>()))
            .Callback<UserProgress, CancellationToken>((p, _) => savedProgress = p)
            .ReturnsAsync((UserProgress p, CancellationToken _) => p);

        // Second session score = 60 → avg = (80 + 60) / 2 = 70
        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 60, MinutesSpent = 10 };

        await _service.ExecuteAsync(_userId, request);

        Assert.NotNull(savedProgress);
        Assert.Equal(70m, savedProgress!.AvgDictationScore);
        Assert.Equal(2, savedProgress.DictationCompleted);
    }

    [Fact]
    public async Task ExecuteAsync_ShadowingType_UpdatesShadowingFields()
    {
        UserProgress? savedProgress = null;
        _progressRepoMock.Setup(r => r.UpsertAsync(It.IsAny<UserProgress>(), It.IsAny<CancellationToken>()))
            .Callback<UserProgress, CancellationToken>((p, _) => savedProgress = p)
            .ReturnsAsync((UserProgress p, CancellationToken _) => p);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Shadowing, Score = 72, MinutesSpent = 8 };

        await _service.ExecuteAsync(_userId, request);

        Assert.NotNull(savedProgress);
        Assert.Equal(1, savedProgress!.ShadowingCompleted);
        Assert.Equal(0, savedProgress.DictationCompleted);
        Assert.Equal(72m, savedProgress.AvgShadowingScore);
    }

    // ── Streak: First Activity ─────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_FirstEverActivity_StreakStartsAt1()
    {
        UserStreak? savedStreak = null;
        _streakRepoMock.Setup(r => r.UpsertAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Callback<UserStreak, CancellationToken>((s, _) => savedStreak = s)
            .ReturnsAsync((UserStreak s, CancellationToken _) => s);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 60, MinutesSpent = 5 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.StreakIncreased);
        Assert.Equal(1, result.Value.CurrentStreak);
        Assert.NotNull(savedStreak);
        Assert.Equal(1, savedStreak!.CurrentStreak);
        Assert.Equal(1, savedStreak.LongestStreak);
        Assert.Equal(1, savedStreak.TotalActiveDays);
    }

    // ── Streak: Already Learned Today ────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_AlreadyLearnedToday_StreakUnchanged()
    {
        var existingStreak = new UserStreak
        {
            UserId = _userId,
            CurrentStreak = 5,
            LongestStreak = 10,
            TotalActiveDays = 20,
            LastActiveDate = DateTime.UtcNow.Date // today
        };
        _streakRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStreak);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 70, MinutesSpent = 5 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.StreakIncreased);
        Assert.Equal(5, result.Value.CurrentStreak);
    }

    // ── Streak: Last Activity Yesterday ──────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_LastLearnedYesterday_StreakIncremented()
    {
        var existingStreak = new UserStreak
        {
            UserId = _userId,
            CurrentStreak = 4,
            LongestStreak = 7,
            TotalActiveDays = 15,
            LastActiveDate = DateTime.UtcNow.Date.AddDays(-1) // yesterday
        };
        _streakRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStreak);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 70, MinutesSpent = 5 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.StreakIncreased);
        Assert.Equal(5, result.Value.CurrentStreak);
    }

    // ── Streak: Longest Streak Updated ───────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_StreakExceedsLongest_LongestStreakUpdated()
    {
        var existingStreak = new UserStreak
        {
            UserId = _userId,
            CurrentStreak = 7, // same as longest
            LongestStreak = 7,
            TotalActiveDays = 20,
            LastActiveDate = DateTime.UtcNow.Date.AddDays(-1)
        };
        _streakRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStreak);

        UserStreak? savedStreak = null;
        _streakRepoMock.Setup(r => r.UpsertAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()))
            .Callback<UserStreak, CancellationToken>((s, _) => savedStreak = s)
            .ReturnsAsync((UserStreak s, CancellationToken _) => s);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 70, MinutesSpent = 5 };

        await _service.ExecuteAsync(_userId, request);

        Assert.NotNull(savedStreak);
        Assert.Equal(8, savedStreak!.CurrentStreak);
        Assert.Equal(8, savedStreak.LongestStreak); // updated to new max
    }

    // ── Streak: Gap > 1 Day → Reset ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_GapMoreThanOneDay_StreakResetToOne()
    {
        var existingStreak = new UserStreak
        {
            UserId = _userId,
            CurrentStreak = 10,
            LongestStreak = 10,
            TotalActiveDays = 30,
            LastActiveDate = DateTime.UtcNow.Date.AddDays(-5) // 5 days ago
        };
        _streakRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStreak);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 80, MinutesSpent = 10 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.StreakIncreased); // re-starting counts as increased
        Assert.Equal(1, result.Value.CurrentStreak); // reset
    }

    // ── Minutes: Negative clamped to 0 ─────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NegativeMinutes_ClampedToZero()
    {
        UserProgress? savedProgress = null;
        _progressRepoMock.Setup(r => r.UpsertAsync(It.IsAny<UserProgress>(), It.IsAny<CancellationToken>()))
            .Callback<UserProgress, CancellationToken>((p, _) => savedProgress = p)
            .ReturnsAsync((UserProgress p, CancellationToken _) => p);

        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 50, MinutesSpent = -5 };

        var result = await _service.ExecuteAsync(_userId, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedProgress);
        Assert.Equal(0, savedProgress!.TotalMinutes); // clamped to 0
    }

    // ── Repository called once ─────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ValidRequest_UpsertsBothRepos()
    {
        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = 70, MinutesSpent = 10 };

        await _service.ExecuteAsync(_userId, request);

        _progressRepoMock.Verify(r => r.UpsertAsync(It.IsAny<UserProgress>(), It.IsAny<CancellationToken>()), Times.Once);
        _streakRepoMock.Verify(r => r.UpsertAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidScore_DoesNotCallUpsert()
    {
        var request = new RecordActivityRequest { ExerciseType = ExerciseType.Dictation, Score = -10, MinutesSpent = 5 };

        await _service.ExecuteAsync(_userId, request);

        _progressRepoMock.Verify(r => r.UpsertAsync(It.IsAny<UserProgress>(), It.IsAny<CancellationToken>()), Times.Never);
        _streakRepoMock.Verify(r => r.UpsertAsync(It.IsAny<UserStreak>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
