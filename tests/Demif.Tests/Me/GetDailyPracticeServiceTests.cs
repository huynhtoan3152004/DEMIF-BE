using Demif.Application.Abstractions.Persistence;
using Demif.Application.Features.Me.Stats;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Demif.Tests.Me;

using Demif.Tests.Helpers;

public class GetDailyPracticeServiceTests
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
    public async Task ExecuteAsync_NoExercises_Returns30DaysOfZeros()
    {
        var dbContext = CreateDbContext(new List<UserExercise>());
        var service = new GetDailyPracticeService(dbContext);

        var result = await service.ExecuteAsync(_userId, 30);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.Data.Count);
        Assert.Equal("All", result.Value.ExerciseType);
        Assert.All(result.Value.Data, d =>
        {
            Assert.Equal(0, d.Minutes);
            Assert.Equal(0, d.XpEarned);
            Assert.Equal(0, d.SessionsCount);
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithExercises_AggregatesCorrectly()
    {
        var today = DateTime.UtcNow.Date;
        var exercises = new List<UserExercise>
        {
            new() { UserId = _userId, LessonId = Guid.NewGuid(), ExerciseType = ExerciseType.Dictation, CompletedAt = today, Score = 80, TimeSpentSeconds = 600 },
            new() { UserId = _userId, LessonId = Guid.NewGuid(), ExerciseType = ExerciseType.Dictation, CompletedAt = today, Score = 90, TimeSpentSeconds = 900 },
        };

        var dbContext = CreateDbContext(exercises);
        var service = new GetDailyPracticeService(dbContext);

        var result = await service.ExecuteAsync(_userId, 7);

        Assert.True(result.IsSuccess);
        var todayData = result.Value.Data.First(d => d.Date == today.ToString("yyyy-MM-dd"));
        Assert.Equal(2, todayData.SessionsCount);
        Assert.Equal(25, todayData.Minutes); // (600+900)/60 = 25
        Assert.True(todayData.XpEarned > 0);
    }

    [Fact]
    public async Task ExecuteAsync_FilterByExerciseType_OnlyReturnsDictation()
    {
        var today = DateTime.UtcNow.Date;
        var exercises = new List<UserExercise>
        {
            new() { UserId = _userId, LessonId = Guid.NewGuid(), ExerciseType = ExerciseType.Dictation, CompletedAt = today, Score = 80, TimeSpentSeconds = 300 },
            new() { UserId = _userId, LessonId = Guid.NewGuid(), ExerciseType = ExerciseType.Shadowing, CompletedAt = today, Score = 70, TimeSpentSeconds = 600 },
        };

        var dbContext = CreateDbContext(exercises);
        var service = new GetDailyPracticeService(dbContext);

        var result = await service.ExecuteAsync(_userId, 7, ExerciseType.Dictation);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dictation", result.Value.ExerciseType);
        var todayData = result.Value.Data.First(d => d.Date == today.ToString("yyyy-MM-dd"));
        Assert.Equal(1, todayData.SessionsCount);
    }
}
