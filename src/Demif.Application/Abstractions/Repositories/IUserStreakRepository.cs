using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// UserStreak Repository interface
/// </summary>
public interface IUserStreakRepository
{
    Task<UserStreak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserStreak> UpsertAsync(UserStreak streak, CancellationToken cancellationToken = default);
}
