using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Subscriptions.GetPlans;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Moq;

namespace Demif.Tests.Subscriptions;

public class GetPlansServiceTests
{
    [Fact]
    public async Task ExecuteAsync_FourActivePlans_ReturnsOnlySupportedPremiumPlans()
    {
        var planRepository = new Mock<ISubscriptionPlanRepository>();
        planRepository
            .Setup(r => r.GetActivePlansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Premium 7 Ngày",
                    Tier = SubscriptionTier.Premium,
                    Price = 99000,
                    Currency = "VND",
                    BillingCycle = BillingCycle.Weekly,
                    Features = "[\"Không giới hạn bài học\"]",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Premium Tháng",
                    Tier = SubscriptionTier.Premium,
                    Price = 199000,
                    Currency = "VND",
                    BillingCycle = BillingCycle.Monthly,
                    Features = "[\"Không giới hạn bài học\"]",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Cơ bản",
                    Tier = SubscriptionTier.Basic,
                    Price = 59000,
                    Currency = "VND",
                    BillingCycle = BillingCycle.Monthly,
                    Features = "[\"5 bài học/ngày\"]",
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Premium Vĩnh viễn",
                    Tier = SubscriptionTier.Premium,
                    Price = 1499000,
                    Currency = "VND",
                    BillingCycle = BillingCycle.Lifetime,
                    Features = "[\"Không giới hạn bài học\"]",
                    IsActive = true
                }
            });

        var service = new GetPlansService(planRepository.Object);

        var result = await service.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Plans.Count);
        Assert.All(result.Value.Plans, plan => Assert.Equal("Premium", plan.Tier));
        Assert.DoesNotContain(result.Value.Plans, plan => plan.BillingCycle == BillingCycle.Lifetime.ToString());
        Assert.Contains(result.Value.Plans, plan => plan.Name == "Premium 7 Ngày");
        Assert.Contains(result.Value.Plans, plan => plan.Name == "Premium Tháng");
    }
}