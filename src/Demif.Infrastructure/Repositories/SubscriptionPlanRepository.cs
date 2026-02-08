using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Repositories;

/// <summary>
/// SubscriptionPlan Repository implementation
/// </summary>
public class SubscriptionPlanRepository : GenericRepository<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(Persistence.ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetByTierAndCycleAsync(SubscriptionTier tier, BillingCycle cycle, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Tier == tier && p.BillingCycle == cycle && p.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<(SubscriptionPlan Plan, int SubscriberCount, int ActiveCount)>> GetPlansWithStatsAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _dbSet
            .Include(p => p.Subscriptions)
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);

        return plans.Select(p => (
            Plan: p,
            SubscriberCount: p.Subscriptions.Count,
            ActiveCount: p.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active)
        ));
    }
}
