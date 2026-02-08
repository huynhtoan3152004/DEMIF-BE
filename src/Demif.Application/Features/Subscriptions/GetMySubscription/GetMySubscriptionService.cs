using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Subscriptions.GetMySubscription;

/// <summary>
/// GetMySubscription Service - lấy subscription hiện tại của user
/// </summary>
public class GetMySubscriptionService
{
    private readonly IUserSubscriptionRepository _subscriptionRepository;

    public GetMySubscriptionService(IUserSubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<GetMySubscriptionResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken);

        if (subscription is null)
        {
            return Result.Success(new GetMySubscriptionResponse
            {
                HasActiveSubscription = false,
                Subscription = null
            });
        }

        // Load plan info
        subscription = await _subscriptionRepository.GetByIdWithPlanAsync(subscription.Id, cancellationToken);

        var daysRemaining = subscription!.EndDate.HasValue
            ? (int?)(subscription.EndDate.Value - DateTime.UtcNow).TotalDays
            : null; // null = lifetime

        return Result.Success(new GetMySubscriptionResponse
        {
            HasActiveSubscription = true,
            Subscription = new SubscriptionDto
            {
                Id = subscription.Id,
                PlanName = subscription.Plan?.Name ?? "Unknown",
                Tier = subscription.Plan?.Tier.ToString() ?? "Unknown",
                BillingCycle = subscription.Plan?.BillingCycle.ToString() ?? "Unknown",
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                Status = subscription.Status.ToString(),
                AutoRenew = subscription.AutoRenew,
                DaysRemaining = daysRemaining.HasValue ? Math.Max(0, (int)daysRemaining) : null
            }
        });
    }
}
