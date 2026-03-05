using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Repositories;

/// <summary>
/// UserStreak Repository implementation
/// </summary>
public class UserStreakRepository : IUserStreakRepository
{
    private readonly ApplicationDbContext _context;

    public UserStreakRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserStreak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserStreaks
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<UserStreak> UpsertAsync(UserStreak streak, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserStreaks
            .FirstOrDefaultAsync(s => s.UserId == streak.UserId, cancellationToken);

        if (existing is null)
        {
            streak.Id = Guid.NewGuid();
            await _context.UserStreaks.AddAsync(streak, cancellationToken);
        }
        else
        {
            existing.CurrentStreak = streak.CurrentStreak;
            existing.LongestStreak = streak.LongestStreak;
            existing.LastActiveDate = streak.LastActiveDate;
            existing.TotalActiveDays = streak.TotalActiveDays;
            existing.FreezeCount = streak.FreezeCount;
            existing.FreezesAvailable = streak.FreezesAvailable;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing ?? streak;
    }
}
