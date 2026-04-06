using Demif.Application.Abstractions.Persistence;
using Demif.Application.Features.Me.Stats;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Demif.Tests.Me;

using Demif.Tests.Helpers;

public class GetActivityHeatmapServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private IApplicationDbContext CreateDbContext(List<UserExercise> exercises)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);
        context.UserExercises.AddRange(exercises);
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task ExecuteAsync_NoExercises_ReturnsAllZeroCounts()
    {
        var dbContext = CreateDbContext(new List<UserExercise>());
        var service = new GetActivityHeatmapService(dbContext);

        var result = await service.ExecuteAsync(_userId, 1);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value.Data, d => Assert.Equal(0, d.Count));
        Assert.Equal(0, result.Value.TotalActivities);
    }

    [Fact]
    public async Task ExecuteAsync_WithExercises_CountsCorrectly()
    {
        var today = DateTime.UtcNow.Date;
        var exercises = new List<UserExercise>
        {
            new() { UserId = _userId, LessonId = Guid.NewGuid(), ExerciseType = ExerciseType.Dictation, CompletedAt = today, Score = 80 },
            new() { UserId = _userId, LessonId = Guid.NewGuid(), ExerciseType = ExerciseType.Dictation, CompletedAt = today, Score = 90 },
            new() { UserId = _userId, LessonId = Guid.NewGuid(), ExerciseType = ExerciseType.Shadowing, CompletedAt = today.AddDays(-1), Score = 70 },
        };

        var dbContext = CreateDbContext(exercises);
        var service = new GetActivityHeatmapService(dbContext);

        var result = await service.ExecuteAsync(_userId, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalActivities);

        var todayData = result.Value.Data.First(d => d.Date == today.ToString("yyyy-MM-dd"));
        Assert.Equal(2, todayData.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesOtherUsers()
    {
        var otherUserId = Guid.NewGuid();
        var exercises = new List<UserExercise>
        {
            new() { UserId = otherUserId, LessonId = Guid.NewGuid(), ExerciseType = ExerciseType.Dictation, CompletedAt = DateTime.UtcNow, Score = 80 },
        };

        var dbContext = CreateDbContext(exercises);
        var service = new GetActivityHeatmapService(dbContext);

        var result = await service.ExecuteAsync(_userId, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalActivities);
    }
}
