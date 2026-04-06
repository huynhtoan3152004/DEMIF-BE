using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Me.Stats;
using Demif.Domain.Entities;
using Moq;

namespace Demif.Tests.Me;

public class GetStatsSummaryServiceTests
{
    private readonly Mock<IUserStreakRepository> _streakRepoMock;
    private readonly Mock<IUserProgressRepository> _progressRepoMock;
    private readonly GetStatsSummaryService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public GetStatsSummaryServiceTests()
    {
        _streakRepoMock = new Mock<IUserStreakRepository>();
        _progressRepoMock = new Mock<IUserProgressRepository>();
        _service = new GetStatsSummaryService(_streakRepoMock.Object, _progressRepoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_NewUser_ReturnsAllZeros()
    {
        _streakRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStreak?)null);
        _progressRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProgress?)null);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        var v = result.Value;
        Assert.Equal(0, v.LongestStreak);
        Assert.Equal(0, v.CurrentStreak);
        Assert.Equal(0, v.SavedWordsCount);
        Assert.Equal(0, v.TotalPracticeMinutes);
        Assert.Equal(0, v.TotalXp);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingUser_ReturnsMappedValues()
    {
        _streakRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreak
            {
                UserId = _userId,
                CurrentStreak = 3,
                LongestStreak = 7,
                TotalActiveDays = 20
            });
        _progressRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProgress
            {
                UserId = _userId,
                TotalMinutes = 120,
                TotalPoints = 850
            });

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        var v = result.Value;
        Assert.Equal(7, v.LongestStreak);
        Assert.Equal(3, v.CurrentStreak);
        Assert.Equal(0, v.SavedWordsCount); // placeholder
        Assert.Equal(120, v.TotalPracticeMinutes);
        Assert.Equal(850, v.TotalXp);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyStreakExists_ReturnsProgressAsZero()
    {
        _streakRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStreak { UserId = _userId, LongestStreak = 5 });
        _progressRepoMock.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProgress?)null);

        var result = await _service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.LongestStreak);
        Assert.Equal(0, result.Value.TotalPracticeMinutes);
        Assert.Equal(0, result.Value.TotalXp);
    }
}
