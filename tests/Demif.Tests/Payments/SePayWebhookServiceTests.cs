using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Payments.Webhook;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Demif.Tests.Payments;

public class SePayWebhookServiceTests
{
    [Fact]
    public async Task HandleWebhookAsync_SaveChangesConcurrencyException_ReturnsSuccessAck()
    {
        var paymentReference = "DEMIFTEST1234";
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var paymentRepo = new Mock<IPaymentRepository>();
        var subscriptionRepo = new Mock<IUserSubscriptionRepository>();
        var userRepo = new Mock<IUserRepository>();
        var roleRepo = new Mock<IRoleRepository>();
        var dbContext = new Mock<IApplicationDbContext>();

        paymentRepo.Setup(r => r.GetByReferenceAsync(paymentReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = planId,
                SubscriptionId = subscriptionId,
                PaymentReference = paymentReference,
                Amount = 19000,
                Currency = "VND",
                PaymentMethod = "sepay_bank",
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            });

        subscriptionRepo.Setup(r => r.GetByIdWithPlanAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSubscription
            {
                Id = subscriptionId,
                UserId = userId,
                PlanId = planId,
                Status = SubscriptionStatus.PendingPayment,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                Plan = new SubscriptionPlan
                {
                    Id = planId,
                    Name = "Monthly",
                    Tier = SubscriptionTier.Premium,
                    Price = 19000,
                    Currency = "VND",
                    BillingCycle = BillingCycle.Monthly,
                    IsActive = true
                }
            });

        userRepo.Setup(r => r.GetByIdWithRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                Email = "user@test.com",
                Username = "testuser",
                PasswordHash = "hash",
                UserRoles = new List<UserRole>()
            });

        roleRepo.Setup(r => r.GetByNameAsync("Premium", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role
            {
                Id = Guid.NewGuid(),
                Name = "Premium"
            });

        dbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("expected to affect 1 row but affected 0 rows"));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SEPay:SecretKey"] = "spsk_test_123",
            })
            .Build();

        var service = new SePayWebhookService(
            paymentRepo.Object,
            subscriptionRepo.Object,
            userRepo.Object,
            roleRepo.Object,
            dbContext.Object,
            config,
            NullLogger<SePayWebhookService>.Instance);

        var result = await service.HandleWebhookAsync(
            new SePayWebhookRequest
            {
                Id = 50549621,
                Gateway = "TPBank",
                Content = paymentReference,
                TransferType = "in",
                TransferAmount = 19000,
                ReferenceCode = "669ITC126104A4LP"
            },
            null,
            "spsk_test_123");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Equal("Webhook already processed", result.Value.Message);
    }
}
