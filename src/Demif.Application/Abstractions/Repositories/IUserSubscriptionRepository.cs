using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// UserSubscription Repository interface
/// </summary>
public interface IUserSubscriptionRepository : IGenericRepository<UserSubscription>
{
    /// <summary>
    /// Lấy subscription đang active của user
    /// </summary>
    Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả subscription của user
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra user có subscription active không
    /// </summary>
    Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy subscription với thông tin plan
    /// </summary>
    Task<UserSubscription?> GetByIdWithPlanAsync(Guid id, CancellationToken cancellationToken = default);
}
