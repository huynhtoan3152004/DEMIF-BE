using Demif.Domain.Entities;
using Demif.Domain.Enums;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// SubscriptionPlan Repository interface
/// </summary>
public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan>
{
    /// <summary>
    /// Lấy danh sách gói đang active
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy gói theo tier và billing cycle
    /// </summary>
    Task<SubscriptionPlan?> GetByTierAndCycleAsync(SubscriptionTier tier, BillingCycle cycle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thống kê số người đăng ký theo gói
    /// </summary>
    Task<IEnumerable<(SubscriptionPlan Plan, int SubscriberCount, int ActiveCount)>> GetPlansWithStatsAsync(CancellationToken cancellationToken = default);
}
