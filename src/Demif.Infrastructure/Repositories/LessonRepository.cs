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

    public async Task<(IEnumerable<Lesson> Items, int TotalCount)> GetPaginatedAsync(
        int page,
        int pageSize,
        Level? level = null,
        LessonType? type = null,
        string? category = null,
        bool? isPremiumOnly = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (level.HasValue)
            query = query.Where(l => l.Level == level.Value);

        if (type.HasValue)
            query = query.Where(l => l.LessonType == type.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        if (isPremiumOnly.HasValue)
            query = query.Where(l => l.IsPremiumOnly == isPremiumOnly.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Lesson> Items, int TotalCount)> GetForUserAsync(
        int page,
        int pageSize,
        bool hasPremiumAccess,
        Level? level = null,
        LessonType? type = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(l => l.Status == "published");

        // Nếu user không có premium, filter bỏ premium lessons
        if (!hasPremiumAccess)
            query = query.Where(l => !l.IsPremiumOnly);

        if (level.HasValue)
            query = query.Where(l => l.Level == level.Value);

        if (type.HasValue)
            query = query.Where(l => l.LessonType == type.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> CountAsync(bool? isPremiumOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(l => l.Status == "published");

        if (isPremiumOnly.HasValue)
            query = query.Where(l => l.IsPremiumOnly == isPremiumOnly.Value);

        return await query.CountAsync(cancellationToken);
    }
}

