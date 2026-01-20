using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Repositories;

/// <summary>
/// User Repository implementation
/// </summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);
    }
}
