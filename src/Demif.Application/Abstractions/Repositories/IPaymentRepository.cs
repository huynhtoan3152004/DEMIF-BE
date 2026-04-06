using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// Payment Repository interface
/// </summary>
public interface IPaymentRepository : IGenericRepository<Payment>
{
    /// <summary>
    /// Lấy payment theo reference code (cho webhook matching)
    /// </summary>
    Task<Payment?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả payment của user
    /// </summary>
    Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy payment theo transaction ID từ gateway
    /// </summary>
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<Payment?> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Payment>> GetPendingPaymentsOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy payment với subscription info
    /// </summary>
    Task<Payment?> GetByIdWithSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lấy tất cả payments kềm theo user và subscription (phân trang, filter cho Admin)
    /// </summary>
    Task<(IEnumerable<Payment> Items, int Total)> GetAllWithDetailsAsync(
        int page, int pageSize,
        string? status, string? search,
        DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default);
}
