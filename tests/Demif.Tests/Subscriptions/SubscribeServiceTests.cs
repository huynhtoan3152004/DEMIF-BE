using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Subscriptions.Subscribe;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Tests.Helpers;
using Moq;

namespace Demif.Tests.Subscriptions;

public class SubscribeServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WeeklyPlanCancelledBefore_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                Email = "user@test.com",
                Username = "user1",
                PasswordHash = "hash"
            });

        var planRepo = new Mock<ISubscriptionPlanRepository>();
        planRepo.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPlan
            {
                Id = planId,
                Name = "Premium 7 Ngày",
                Tier = SubscriptionTier.Premium,
                Price = 99000,
                Currency = "VND",
                BillingCycle = BillingCycle.Weekly,
                DurationDays = 7,
                IsActive = true
            });

        var subscriptionRepo = new Mock<IUserSubscriptionRepository>();
        subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSubscription>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PlanId = planId,
                    Status = SubscriptionStatus.Cancelled,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            });

        var paymentRepo = new Mock<IPaymentRepository>();
        var dbContext = new Mock<IApplicationDbContext>();

        var service = new SubscribeService(
            planRepo.Object,
            subscriptionRepo.Object,
            paymentRepo.Object,
            userRepo.Object,
            dbContext.Object);

        var result = await service.ExecuteAsync(userId, new SubscribeRequest { PlanId = planId });

        Assert.True(result.IsSuccess);
        subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
        paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WeeklyPlanExpiredBefore_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                Email = "user@test.com",
                Username = "user1",
                PasswordHash = "hash"
            });

        var planRepo = new Mock<ISubscriptionPlanRepository>();
        planRepo.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPlan
            {
                Id = planId,
                Name = "Premium 7 Ngày",
                Tier = SubscriptionTier.Premium,
                Price = 99000,
                Currency = "VND",
                BillingCycle = BillingCycle.Weekly,
                DurationDays = 7,
                IsActive = true
            });

        var subscriptionRepo = new Mock<IUserSubscriptionRepository>();
        subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSubscription>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PlanId = planId,
                    Status = SubscriptionStatus.Expired,
                    EndDate = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            });

        var paymentRepo = new Mock<IPaymentRepository>();
        var dbContext = new Mock<IApplicationDbContext>();

        var service = new SubscribeService(
            planRepo.Object,
            subscriptionRepo.Object,
            paymentRepo.Object,
            userRepo.Object,
            dbContext.Object);

        var result = await service.ExecuteAsync(userId, new SubscribeRequest { PlanId = planId });

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
        Assert.Contains("chỉ được đăng ký lại", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
