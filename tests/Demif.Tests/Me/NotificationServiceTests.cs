using Demif.Application.Features.Me.Notifications;
using Demif.Domain.Entities;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Demif.Tests.Me;

public class NotificationServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task GetMyNotifications_ReturnsUnreadFirst_WithCounts()
    {
        var context = CreateDbContext();
        SeedNotifications(context);

        var service = new GetMyNotificationsService(context);

        var result = await service.ExecuteAsync(_userId, page: 1, pageSize: 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.UnreadCount);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.False(result.Value.Items.First().IsRead);
        Assert.Equal("Unread 2", result.Value.Items.First().Title);
    }

    [Fact]
    public async Task GetUnreadNotificationCount_ReturnsOnlyUnreadItems()
    {
        var context = CreateDbContext();
        SeedNotifications(context);

        var service = new GetUnreadNotificationCountService(context);

        var result = await service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.UnreadCount);
    }

    [Fact]
    public async Task MarkNotificationAsRead_UpdatesItemAndReadAt()
    {
        var context = CreateDbContext();
        SeedNotifications(context);
        var targetId = context.UserNotifications.First(notification => notification.UserId == _userId && !notification.IsRead).Id;

        var service = new MarkNotificationAsReadService(context);

        var result = await service.ExecuteAsync(_userId, targetId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsRead);
        Assert.NotNull(result.Value.ReadAt);
        Assert.True(context.UserNotifications.Single(notification => notification.Id == targetId).IsRead);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_UpdatesAllUnreadItems()
    {
        var context = CreateDbContext();
        SeedNotifications(context);

        var service = new MarkAllNotificationsAsReadService(context);

        var result = await service.ExecuteAsync(_userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.UpdatedCount);
        Assert.All(context.UserNotifications.Where(notification => notification.UserId == _userId), notification => Assert.True(notification.IsRead));
    }

    private static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private void SeedNotifications(TestDbContext context)
    {
        context.UserNotifications.AddRange(
            new UserNotification
            {
                UserId = _userId,
                Title = "Unread 1",
                Message = "Message 1",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new UserNotification
            {
                UserId = _userId,
                Title = "Unread 2",
                Message = "Message 2",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-3)
            },
            new UserNotification
            {
                UserId = _userId,
                Title = "Read 1",
                Message = "Message 3",
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddMinutes(-1),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new UserNotification
            {
                UserId = Guid.NewGuid(),
                Title = "Other user",
                Message = "Ignore me",
                IsRead = false
            });

        context.SaveChanges();
    }
}