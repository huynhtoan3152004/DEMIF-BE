using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
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

    public async Task<IEnumerable<Lesson>> GetByLevelAsync(string level, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.Level == level && l.Status == "published")
            .OrderBy(l => l.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lesson>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
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
        string? level = null,
        string? type = null,
        string? category = null,
        string? mediaType = null,
        string? tag = null,
        string? search = null,
        bool? isPremiumOnly = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(l => l.Level == level);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(l => l.LessonType == type);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        if (!string.IsNullOrWhiteSpace(mediaType))
        {
            var normalizedMediaType = mediaType.Trim().ToLower();
            if (normalizedMediaType == "audio")
            {
                query = query.Where(l => l.MediaType == null || l.MediaType.ToLower() == "audio");
            }
            else
            {
                query = query.Where(l => l.MediaType != null && l.MediaType.ToLower() == normalizedMediaType);
            }
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLower();
            query = query.Where(l => l.Tags != null && l.Tags.ToLower().Contains(normalizedTag));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(normalizedSearch)
                || (l.Description ?? string.Empty).ToLower().Contains(normalizedSearch));
        }

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
        bool isLoggedIn,
        string? level = null,
        string? type = null,
        string? category = null,
        string? mediaType = null,
        string? tag = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(l => l.Status == "published");

        // Guest = chỉ free lessons. Login user = full catalog (FE tự xử lý lock/redirect)
        if (!isLoggedIn)
            query = query.Where(l => !l.IsPremiumOnly);

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(l => l.Level == level);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(l => l.LessonType == type);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        if (!string.IsNullOrWhiteSpace(mediaType))
        {
            var normalizedMediaType = mediaType.Trim().ToLower();
            if (normalizedMediaType == "audio")
            {
                query = query.Where(l => l.MediaType == null || l.MediaType.ToLower() == "audio");
            }
            else
            {
                query = query.Where(l => l.MediaType != null && l.MediaType.ToLower() == normalizedMediaType);
            }
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLower();
            query = query.Where(l => l.Tags != null && l.Tags.ToLower().Contains(normalizedTag));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(normalizedSearch)
                || (l.Description ?? string.Empty).ToLower().Contains(normalizedSearch));
        }

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

