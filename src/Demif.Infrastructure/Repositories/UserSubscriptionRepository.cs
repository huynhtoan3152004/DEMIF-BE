using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Demif.Infrastructure.Repositories;

/// <summary>
/// UserSubscription Repository implementation
/// </summary>
public class UserSubscriptionRepository : GenericRepository<UserSubscription>, IUserSubscriptionRepository
{
    public UserSubscriptionRepository(Persistence.ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .Where(s => s.EndDate == null || s.EndDate > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(s => 
                s.UserId == userId && 
                s.Status == SubscriptionStatus.Active &&
                (s.EndDate == null || s.EndDate > DateTime.UtcNow), 
                cancellationToken);
    }

    public async Task<UserSubscription?> GetByIdWithPlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    // ── Admin ──────────────────────────────────────────────────────────────
    public async Task<(IEnumerable<UserSubscription> Items, int Total)> GetAllWithUsersAsync(
        int page, int pageSize,
        string? status, string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(s => s.User)
            .Include(s => s.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<SubscriptionStatus>(status, true, out var statusEnum))
        {
            query = query.Where(s => s.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s =>
                s.User!.Email.ToLower().Contains(term) ||
                s.User.Username.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<UserSubscription?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.User)
            .Include(s => s.Plan)
            .Include(s => s.Payments.OrderByDescending(p => p.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
