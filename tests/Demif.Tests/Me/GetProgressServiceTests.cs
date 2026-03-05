using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Me.GetProgress;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Moq;

namespace Demif.Tests.Me;

/// <summary>
/// Unit tests for GetProgressService
/// </summary>
public class GetProgressServiceTests
{
    private readonly Mock<IUserProgressRepository> _repoMock;
    private readonly GetProgressService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public GetProgressServiceTests()
    {
        _repoMock = new Mock<IUserProgressRepository>();
        _service = new GetProgressService(_repoMock.Object);
    }

    // ── New user — no progress record ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NewUser_ReturnsDefaultZeroValues()
    {
        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProgress?)null);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        var value = result.Value;
        Assert.Equal(0, value.TotalPoints);
        Assert.Equal(0, value.TotalMinutes);
        Assert.Equal(0, value.LessonsCompleted);
        Assert.Equal(0, value.DictationCompleted);
        Assert.Equal(0, value.ShadowingCompleted);
        Assert.Equal(0, value.LevelProgress);
        Assert.Equal(Level.Beginner.ToString(), value.CurrentLevel);
    }

    // ── Existing user — maps all fields correctly ──────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ExistingUser_MapsAllFields()
    {
        var progress = new UserProgress
        {
            UserId = _userId,
            TotalPoints = 1200,
            TotalMinutes = 90,
            LessonsCompleted = 15,
            DictationCompleted = 10,
            ShadowingCompleted = 5,
            AvgDictationScore = 78.5m,
            AvgShadowingScore = 65.0m,
            CurrentLevel = Level.Intermediate,
            LevelProgress = 45
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        var value = result.Value;
        Assert.Equal(1200, value.TotalPoints);
        Assert.Equal(90, value.TotalMinutes);
        Assert.Equal(15, value.LessonsCompleted);
        Assert.Equal(10, value.DictationCompleted);
        Assert.Equal(5, value.ShadowingCompleted);
        Assert.Equal(78.5m, value.AvgDictationScore);
        Assert.Equal(65.0m, value.AvgShadowingScore);
        Assert.Equal(Level.Intermediate.ToString(), value.CurrentLevel);
        Assert.Equal(45, value.LevelProgress);
    }

    // ── Level string is human-readable ──────────────────────────────────────────

    [Theory]
    [InlineData(Level.Beginner, "Beginner")]
    [InlineData(Level.Intermediate, "Intermediate")]
    [InlineData(Level.Advanced, "Advanced")]
    [InlineData(Level.Expert, "Expert")]
    public async Task ExecuteAsync_ReturnsCorrectLevelString(Level level, string expected)
    {
        var progress = new UserProgress { UserId = _userId, CurrentLevel = level };
        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.CurrentLevel);
    }
}
