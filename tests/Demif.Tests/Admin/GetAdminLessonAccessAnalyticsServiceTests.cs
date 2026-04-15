using Demif.Application.Features.Admin.Analytics.Lessons.Access;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Demif.Tests.Admin;

public class GetAdminLessonAccessAnalyticsServiceTests
{
    private static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_WithLessonTrackers_ReturnsAccessStats()
    {
        var context = CreateDbContext();

        var lesson1 = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Daily Greeting",
            LessonType = "Dictation",
            Level = "Beginner",
            Category = "daily",
            Status = "published",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var lesson2 = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Business Fluency",
            LessonType = "Shadowing",
            Level = "Advanced",
            Category = "business",
            Status = "published",
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        var user1 = new User { Id = Guid.NewGuid(), Username = "u1", Email = "u1@example.com" };
        var user2 = new User { Id = Guid.NewGuid(), Username = "u2", Email = "u2@example.com" };
        var user3 = new User { Id = Guid.NewGuid(), Username = "u3", Email = "u3@example.com" };

        context.Users.AddRange(user1, user2, user3);
        context.Lessons.AddRange(lesson1, lesson2);
        context.UserLessonTrackers.AddRange(
            new UserLessonTracker
            {
                Id = Guid.NewGuid(),
                UserId = user1.Id,
                LessonId = lesson1.Id,
                Status = LessonProgressStatus.Completed,
                LastSegmentIndex = 4,
                StartedAt = DateTime.UtcNow.AddDays(-2),
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            },
            new UserLessonTracker
            {
                Id = Guid.NewGuid(),
                UserId = user2.Id,
                LessonId = lesson1.Id,
                Status = LessonProgressStatus.InProgress,
                LastSegmentIndex = 2,
                StartedAt = DateTime.UtcNow.AddHours(-6)
            },
            new UserLessonTracker
            {
                Id = Guid.NewGuid(),
                UserId = user3.Id,
                LessonId = lesson2.Id,
                Status = LessonProgressStatus.Started,
                LastSegmentIndex = 0,
                StartedAt = DateTime.UtcNow.AddHours(-1)
            });

        await context.SaveChangesAsync();

        var service = new GetAdminLessonAccessAnalyticsService(context);
        var result = await service.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalAccessEvents);
        Assert.Equal(2, result.Value.TotalTrackedLessons);
        Assert.Equal(3, result.Value.TotalTrackedUsers);
        Assert.Equal(2, result.Value.TopAccessedLessons.First().AccessCount);
        Assert.Equal(1, result.Value.ByStatus.Single(x => x.Key == LessonProgressStatus.Completed.ToString()).Count);
        Assert.Equal(1, result.Value.ByStatus.Single(x => x.Key == LessonProgressStatus.InProgress.ToString()).Count);
        Assert.Equal(1, result.Value.ByStatus.Single(x => x.Key == LessonProgressStatus.Started.ToString()).Count);
    }
}
