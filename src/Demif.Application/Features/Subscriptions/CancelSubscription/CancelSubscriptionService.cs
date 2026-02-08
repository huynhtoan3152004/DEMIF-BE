using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Subscriptions.CancelSubscription;

/// <summary>
/// CancelSubscription Service - hủy auto-renew subscription
/// </summary>
public class CancelSubscriptionService
{
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IApplicationDbContext _dbContext;

    public CancelSubscriptionService(
        IUserSubscriptionRepository subscriptionRepository,
        IApplicationDbContext dbContext)
    {
        _subscriptionRepository = subscriptionRepository;
        _dbContext = dbContext;
    }

    public async Task<Result> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure(Error.NotFound("Bạn không có gói đăng ký đang hoạt động."));
        }

        // Hủy auto-renew (subscription vẫn active đến hết hạn)
        subscription.AutoRenew = false;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
