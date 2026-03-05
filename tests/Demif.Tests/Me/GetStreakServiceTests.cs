using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Me.GetStreak;
using Demif.Domain.Entities;
using Moq;

namespace Demif.Tests.Me;

/// <summary>
/// Unit tests for GetStreakService
/// </summary>
public class GetStreakServiceTests
{
    private readonly Mock<IUserStreakRepository> _repoMock;
    private readonly GetStreakService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public GetStreakServiceTests()
    {
        _repoMock = new Mock<IUserStreakRepository>();
        _service = new GetStreakService(_repoMock.Object);
    }

    // ── New user — no streak record ────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NewUser_ReturnsDefaultZeroStreak()
    {
        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        var value = result.Value;
        Assert.Equal(0, value.CurrentStreak);
        Assert.Equal(0, value.LongestStreak);
        Assert.Equal(0, value.TotalActiveDays);
        Assert.Equal(1, value.FreezesAvailable); // default 1 freeze
        Assert.Null(value.LastActiveDate);
        Assert.False(value.LearnedToday);
    }

    // ── User learned today ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_LearnedToday_LearnedTodayIsTrue()
    {
        var streak = new UserStreak
        {
            UserId = _userId,
            CurrentStreak = 5,
            LongestStreak = 10,
            TotalActiveDays = 20,
            FreezesAvailable = 1,
            LastActiveDate = DateTime.UtcNow.Date // today
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.LearnedToday);
        Assert.Equal(5, result.Value.CurrentStreak);
    }

    // ── User last learned yesterday ────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_LastLearnedYesterday_LearnedTodayIsFalse()
    {
        var streak = new UserStreak
        {
            UserId = _userId,
            CurrentStreak = 3,
            LongestStreak = 7,
            TotalActiveDays = 12,
            LastActiveDate = DateTime.UtcNow.Date.AddDays(-1) // yesterday
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.LearnedToday);
        Assert.Equal(3, result.Value.CurrentStreak);
    }

    // ── Maps all fields correctly ──────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ExistingStreak_MapsAllFields()
    {
        var lastActive = DateTime.UtcNow.Date.AddDays(-3);
        var streak = new UserStreak
        {
            UserId = _userId,
            CurrentStreak = 7,
            LongestStreak = 14,
            TotalActiveDays = 30,
            FreezesAvailable = 2,
            LastActiveDate = lastActive
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        var value = result.Value;
        Assert.Equal(7, value.CurrentStreak);
        Assert.Equal(14, value.LongestStreak);
        Assert.Equal(30, value.TotalActiveDays);
        Assert.Equal(2, value.FreezesAvailable);
        Assert.Equal(lastActive, value.LastActiveDate);
        Assert.False(value.LearnedToday);
    }
}
