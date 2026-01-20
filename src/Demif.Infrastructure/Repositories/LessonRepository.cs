using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Repositories;

/// <summary>
/// Lesson Repository implementation
/// </summary>
public class LessonRepository : GenericRepository<Lesson>, ILessonRepository
{
    public LessonRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Lesson>> GetByLevelAsync(Level level, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.Level == level && l.Status == "published")
            .OrderBy(l => l.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lesson>> GetByTypeAsync(LessonType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.LessonType == type && l.Status == "published")
            .OrderBy(l => l.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lesson>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.Status == "published")
            .OrderBy(l => l.Title)
            .ToListAsync(cancellationToken);
    }
}
