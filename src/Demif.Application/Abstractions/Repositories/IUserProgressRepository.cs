using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// UserProgress Repository interface
/// </summary>
public interface IUserProgressRepository
{
    Task<UserProgress?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProgress> UpsertAsync(UserProgress progress, CancellationToken cancellationToken = default);
}
