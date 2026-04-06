using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Repositories;

/// <summary>
/// Payment Repository implementation
/// </summary>
public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(Persistence.ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.PaymentReference == reference, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetPendingPaymentsOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == PaymentStatus.Pending && p.CreatedAt < threshold)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Payment> Items, int Total)> GetAllWithDetailsAsync(
        int page, int pageSize,
        string? status, string? search,
        DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(p => p.User)
            .Include(p => p.Subscription)
                .ThenInclude(s => s != null ? s.Plan : null)
            .Include(p => p.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var statusEnum))
        {
            query = query.Where(p => p.Status == statusEnum);
        }

        if (dateFrom.HasValue) query = query.Where(p => p.CreatedAt >= dateFrom.Value.ToUniversalTime());
        if (dateTo.HasValue) query = query.Where(p => p.CreatedAt <= dateTo.Value.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p => 
                (p.User != null && p.User.Email.ToLower().Contains(term)) ||
                (p.User != null && p.User.Username.ToLower().Contains(term)) ||
                (p.PaymentReference != null && p.PaymentReference.ToLower().Contains(term)) ||
                (p.TransactionId != null && p.TransactionId.ToLower().Contains(term))
            );
        }

        var total = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Plan)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
    }

    public async Task<Payment?> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.SubscriptionId == subscriptionId, cancellationToken);
    }

    public async Task<Payment?> GetByIdWithSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Subscription)
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
