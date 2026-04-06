using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// UserSubscription Repository interface
/// </summary>
public interface IUserSubscriptionRepository : IGenericRepository<UserSubscription>
{
    Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSubscription?> GetActiveOrPendingSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSubscription?> GetByIdWithPlanAsync(Guid id, CancellationToken cancellationToken = default);

    // ── Admin ──────────────────────────────────────────────────────────────
    /// <summary>Danh sách tất cả subscription kèm User + Plan (phân trang)</summary>
    Task<(IEnumerable<UserSubscription> Items, int Total)> GetAllWithUsersAsync(
        int page, int pageSize,
        string? status, string? search,
        CancellationToken cancellationToken = default);

    /// <summary>Chi tiết subscription kèm User + Plan + Payments</summary>
    Task<UserSubscription?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
