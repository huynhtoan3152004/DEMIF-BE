using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Admin.UserSubscriptions;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Moq;

namespace Demif.Tests.Admin;

/// <summary>
/// Unit tests for AdminUserSubscriptionService
/// Covers: GetAll pagination, GetById not-found, Extend, Cancel
/// </summary>
public class AdminUserSubscriptionServiceTests
{
    private readonly Mock<IUserSubscriptionRepository> _repoMock;
    private readonly AdminUserSubscriptionService _service;

    public AdminUserSubscriptionServiceTests()
    {
        _repoMock = new Mock<IUserSubscriptionRepository>();
        _service = new AdminUserSubscriptionService(_repoMock.Object);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ValidParams_ReturnsPaginatedResult()
    {
        var subs = new List<UserSubscription>
        {
            BuildSubscription(SubscriptionStatus.Active),
            BuildSubscription(SubscriptionStatus.Expired)
        };
        _repoMock.Setup(r => r.GetAllWithUsersAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((subs, 2));

        var result = await _service.GetAllAsync(1, 20, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Equal(2, result.Value.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_PageBelowOne_NormalizesToPage1()
    {
        _repoMock.Setup(r => r.GetAllWithUsersAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserSubscription>(), 0));

        var result = await _service.GetAllAsync(0, 20, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
    }

    [Fact]
    public async Task GetAllAsync_PageSizeOver100_NormalizesTo20()
    {
        _repoMock.Setup(r => r.GetAllWithUsersAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserSubscription>(), 0));

        var result = await _service.GetAllAsync(1, 999, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.PageSize);
    }

    [Fact]
    public async Task GetAllAsync_ValidStatusFilter_PassesStatusToRepo()
    {
        _repoMock.Setup(r => r.GetAllWithUsersAsync(1, 20, "Active", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserSubscription>(), 0));

        var result = await _service.GetAllAsync(1, 20, "Active", null);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.GetAllWithUsersAsync(1, 20, "Active", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_InvalidStatusFilter_PassesNullToRepo()
    {
        // Invalid status string should result in null being passed (enum parse fails)
        _repoMock.Setup(r => r.GetAllWithUsersAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserSubscription>(), 0));

        var result = await _service.GetAllAsync(1, 20, "InvalidStatus", null);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.GetAllWithUsersAsync(1, 20, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_MultiplePages_CorrectTotalPagesCalculated()
    {
        _repoMock.Setup(r => r.GetAllWithUsersAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserSubscription>(), 25));

        var result = await _service.GetAllAsync(1, 10, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalPages); // ceil(25 / 10) = 3
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSubscription?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal("Admin.Subscription.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsDetailWithPayments()
    {
        var id = Guid.NewGuid();
        var sub = BuildSubscription(SubscriptionStatus.Active, id);
        sub.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 299000,
            Currency = "VND",
            PaymentMethod = "sepay_bank",
            Status = PaymentStatus.Completed,
            PaymentReference = "REF001"
        });

        _repoMock.Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var result = await _service.GetByIdAsync(id);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Single(result.Value.Payments);
        Assert.Equal(299000, result.Value.Payments.First().Amount);
    }

    // ── ExtendAsync ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-30)]
    public async Task ExtendAsync_DaysZeroOrNegative_ReturnsFailure(int days)
    {
        var result = await _service.ExtendAsync(Guid.NewGuid(), new ExtendSubscriptionRequest(days, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Admin.Subscription.InvalidDays", result.Error.Code);
    }

    [Fact]
    public async Task ExtendAsync_SubscriptionNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSubscription?)null);

        var result = await _service.ExtendAsync(id, new ExtendSubscriptionRequest(30, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Admin.Subscription.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExtendAsync_ActiveSubscription_ExtendsFromCurrentEndDate()
    {
        var id = Guid.NewGuid();
        var futureEndDate = DateTime.UtcNow.AddDays(10);
        var sub = BuildSubscription(SubscriptionStatus.Active, id);
        sub.EndDate = futureEndDate;

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        UserSubscription? saved = null;
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
            .Callback<UserSubscription, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var result = await _service.ExtendAsync(id, new ExtendSubscriptionRequest(30, "Admin extension"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(saved);
        // Should extend from futureEndDate (still active), not from today
        var expectedEnd = futureEndDate.AddDays(30);
        Assert.Equal(expectedEnd.Date, saved!.EndDate!.Value.Date);
        Assert.Equal(SubscriptionStatus.Active, saved.Status);
    }

    [Fact]
    public async Task ExtendAsync_ExpiredSubscription_ExtendsFromToday()
    {
        var id = Guid.NewGuid();
        var sub = BuildSubscription(SubscriptionStatus.Expired, id);
        sub.EndDate = DateTime.UtcNow.AddDays(-5); // expired 5 days ago

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ExtendAsync(id, new ExtendSubscriptionRequest(30, null));

        Assert.True(result.IsSuccess);
        // Should extend from today since end date was in the past
        Assert.Equal(SubscriptionStatus.Active, sub.Status); // reactivated
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_SubscriptionNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSubscription?)null);

        var result = await _service.CancelAsync(id, new CancelSubscriptionRequest(null));

        Assert.True(result.IsFailure);
        Assert.Equal("Admin.Subscription.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        var sub = BuildSubscription(SubscriptionStatus.Cancelled, id);
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var result = await _service.CancelAsync(id, new CancelSubscriptionRequest("test"));

        Assert.True(result.IsFailure);
        Assert.Equal("Admin.Subscription.AlreadyCancelled", result.Error.Code);
    }

    [Fact]
    public async Task CancelAsync_ActiveSubscription_SetsStatusAndDisablesAutoRenew()
    {
        var id = Guid.NewGuid();
        var sub = BuildSubscription(SubscriptionStatus.Active, id);
        sub.AutoRenew = true;

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        UserSubscription? saved = null;
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
            .Callback<UserSubscription, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var result = await _service.CancelAsync(id, new CancelSubscriptionRequest("User requested cancel"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(saved);
        Assert.Equal(SubscriptionStatus.Cancelled, saved!.Status);
        Assert.False(saved.AutoRenew);
    }

    [Fact]
    public async Task CancelAsync_PendingPaymentSubscription_CanBeCancelled()
    {
        var id = Guid.NewGuid();
        var sub = BuildSubscription(SubscriptionStatus.PendingPayment, id);

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<UserSubscription>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CancelAsync(id, new CancelSubscriptionRequest(null));

        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionStatus.Cancelled, sub.Status);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UserSubscription BuildSubscription(SubscriptionStatus status, Guid? id = null)
    {
        return new UserSubscription
        {
            Id = id ?? Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            Status = status,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(335),
            AutoRenew = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            User = new User { Id = Guid.NewGuid(), Email = "user@test.com", Username = "testuser", PasswordHash = "hash" },
            Plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Premium Monthly",
                Tier = SubscriptionTier.Premium,
                Price = 299000,
                Currency = "VND",
                BillingCycle = BillingCycle.Monthly,
                IsActive = true
            }
        };
    }
}
