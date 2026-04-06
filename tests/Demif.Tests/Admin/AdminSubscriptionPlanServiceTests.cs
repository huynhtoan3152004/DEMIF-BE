using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Subscriptions.Admin;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Demif.Tests.Admin;

public class AdminSubscriptionPlanServiceTests
{
    private IApplicationDbContext CreateDbContext(List<UserSubscription> subscriptions, List<Payment> payments)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);
        context.UserSubscriptions.AddRange(subscriptions);
        context.Payments.AddRange(payments);
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task CreateAsync_NegativePrice_ReturnsValidationFailure()
    {
        var repoMock = new Mock<ISubscriptionPlanRepository>();
        var dbMock = CreateDbContext(new(), new());
        var service = new AdminSubscriptionPlanService(repoMock.Object, dbMock);

        var request = new CreateUpdatePlanRequest { Price = -100 };
        var result = await service.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ZeroDuration_ReturnsValidationFailure()
    {
        var repoMock = new Mock<ISubscriptionPlanRepository>();
        var dbMock = CreateDbContext(new(), new());
        var service = new AdminSubscriptionPlanService(repoMock.Object, dbMock);

        var request = new CreateUpdatePlanRequest { Price = 0, DurationDays = 0 };
        var result = await service.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_PlanInUse_ChangePrice_ReturnsValidationFailure()
    {
        var planId = Guid.NewGuid();
        var subscriptions = new List<UserSubscription>
        {
            new UserSubscription { Id = Guid.NewGuid(), PlanId = planId, UserId = Guid.NewGuid() }
        };
        
        var dbMock = CreateDbContext(subscriptions, new());
        
        var plan = new SubscriptionPlan { Id = planId, Price = 50000, Tier = SubscriptionTier.Premium, Currency = "VND", BillingCycle = BillingCycle.Monthly, DurationDays = 30 };
        var repoMock = new Mock<ISubscriptionPlanRepository>();
        repoMock.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var service = new AdminSubscriptionPlanService(repoMock.Object, dbMock);

        // Attempting to change Price
        var request = new CreateUpdatePlanRequest 
        { 
            Price = 100000, // Changed
            Tier = SubscriptionTier.Premium, 
            Currency = "VND", 
            BillingCycle = BillingCycle.Monthly, 
            DurationDays = 30 
        };
        var result = await service.UpdateAsync(planId, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_PlanInUse_CosmeticChangesOnly_Succeeds()
    {
        var planId = Guid.NewGuid();
        var subscriptions = new List<UserSubscription>
        {
            new UserSubscription { Id = Guid.NewGuid(), PlanId = planId, UserId = Guid.NewGuid() }
        };
        
        var dbMock = CreateDbContext(subscriptions, new());
        
        var plan = new SubscriptionPlan { Id = planId, Price = 50000, Tier = SubscriptionTier.Premium, Currency = "VND", BillingCycle = BillingCycle.Monthly, DurationDays = 30, Name = "Old Name" };
        var repoMock = new Mock<ISubscriptionPlanRepository>();
        repoMock.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var service = new AdminSubscriptionPlanService(repoMock.Object, dbMock);

        // Making ONLY cosmetic changes (Name, Features, BadgeText)
        var request = new CreateUpdatePlanRequest 
        { 
            Name = "New Name",
            Price = 50000, // Unchanged
            Tier = SubscriptionTier.Premium, // Unchanged 
            Currency = "VND", // Unchanged
            BillingCycle = BillingCycle.Monthly, // Unchanged
            DurationDays = 30, // Unchanged
            BadgeText = "Hot", // Changed
            BadgeColor = "Red", // Changed
            IsActive = false // Deactivating plan
        };
        var result = await service.UpdateAsync(planId, request);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_PlanNotInUse_ChangePrice_Succeeds()
    {
        var planId = Guid.NewGuid();
        
        // No subscriptions, no payments
        var dbMock = CreateDbContext(new(), new());
        
        var plan = new SubscriptionPlan { Id = planId, Price = 50000, Tier = SubscriptionTier.Premium, Currency = "VND", BillingCycle = BillingCycle.Monthly, DurationDays = 30 };
        var repoMock = new Mock<ISubscriptionPlanRepository>();
        repoMock.Setup(r => r.GetByIdAsync(planId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var service = new AdminSubscriptionPlanService(repoMock.Object, dbMock);

        // Attempting to change Price
        var request = new CreateUpdatePlanRequest 
        { 
            Price = 100000, // Changed
            DurationDays = 90, // Changed
            Tier = SubscriptionTier.Premium, 
            Currency = "VND", 
            BillingCycle = BillingCycle.Monthly
        };
        var result = await service.UpdateAsync(planId, request);

        Assert.True(result.IsSuccess);
    }
}
