using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Admin.Notifications;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Moq;

namespace Demif.Tests.Admin;

public class BroadcastSystemNotificationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly BroadcastSystemNotificationService _service;

    public BroadcastSystemNotificationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();

        _service = new BroadcastSystemNotificationService(
            _userRepositoryMock.Object,
            _emailServiceMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<BroadcastSystemNotificationService>>());
    }

    [Fact]
    public async Task ExecuteAsync_NoRecipients_ReturnsFailure()
    {
        _userRepositoryMock
            .Setup(r => r.GetBroadcastRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<User>());

        var result = await _service.ExecuteAsync(new BroadcastSystemNotificationRequest
        {
            Title = "Ưu đãi mới",
            Message = "Giảm giá 30% cho gói premium",
            ActionUrl = "https://example.com/offers"
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Admin.Notification.NoRecipients", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_DeduplicatesEmailsAndSendsAnnouncement()
    {
        _userRepositoryMock
            .Setup(r => r.GetBroadcastRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                BuildUser("user1@example.com", "User 1", UserStatus.Active),
                BuildUser("user1@example.com", "User 1 Duplicate", UserStatus.Inactive),
                BuildUser("user2@example.com", "User 2", UserStatus.Pending),
                BuildUser("blocked@example.com", "Blocked", UserStatus.Suspended)
            });

        _emailServiceMock
            .Setup(s => s.SendSystemAnnouncementAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ExecuteAsync(new BroadcastSystemNotificationRequest
        {
            Title = "Ưu đãi đặc biệt",
            Message = "Nhận ngay ưu đãi 50% trong tuần này",
            ActionUrl = "https://example.com/promo"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.EligibleUserCount);
        Assert.Equal(2, result.Value.SentCount);
        Assert.Equal(0, result.Value.FailedCount);
        Assert.Equal("email", result.Value.Channel);

        _emailServiceMock.Verify(s => s.SendSystemAnnouncementAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Ưu đãi đặc biệt",
            "Nhận ngay ưu đãi 50% trong tuần này",
            "https://example.com/promo",
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static User BuildUser(string email, string username, UserStatus status)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            Status = status
        };
    }
}