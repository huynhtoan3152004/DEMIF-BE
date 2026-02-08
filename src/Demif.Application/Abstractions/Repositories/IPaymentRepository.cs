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

    /// <summary>
    /// Lấy payment với subscription info
    /// </summary>
    Task<Payment?> GetByIdWithSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);
}
