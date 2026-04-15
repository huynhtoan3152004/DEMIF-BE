using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Repositories
{
    // Kế thừa GenericRepository (đã có sẵn các hàm Add, Update, Delete...) 
    // và thực thi IBlogRepository
    public class BlogRepository : GenericRepository<Blog>, IBlogRepository
    {
        public BlogRepository(ApplicationDbContext context) : base(context)
        {
            // Nếu sau này bạn cần viết các câu query phức tạp dành riêng cho Blog
            // (ví dụ: lấy top 5 bài viết nhiều view nhất), bạn sẽ viết thêm ở đây.
            // Còn các thao tác cơ bản thì GenericRepository đã lo hết rồi!
        }

        public async Task<(IReadOnlyList<Blog> Items, int TotalCount)> GetPagedAsync(
            string? search,
            string? category,
            string? tag,
            string? status,
            bool includeDeleted,
            string sortBy,
            string sortDirection,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Blogs
                .AsNoTracking()
                .Include(x => x.Author)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(x => x.Tags != null && EF.Functions.ILike(x.Tags, $"%{tag.Trim()}%"));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(x => EF.Functions.ILike(x.Title, $"%{keyword}%")
                    || (x.Summary != null && EF.Functions.ILike(x.Summary, $"%{keyword}%"))
                    || EF.Functions.ILike(x.Content, $"%{keyword}%"));
            }

            query = ApplySort(query, sortBy, sortDirection);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Blog?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Blogs
                .AsNoTracking()
                .Include(x => x.Author)
                .Where(x => x.Slug == slug);

            if (!includeDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Blog?> GetByIdWithAuthorAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Blogs
                .Include(x => x.Author)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Blogs.AsNoTracking().Where(x => x.Slug == slug);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        private static IQueryable<Blog> ApplySort(IQueryable<Blog> query, string sortBy, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "views" => descending ? query.OrderByDescending(x => x.ViewCount) : query.OrderBy(x => x.ViewCount),
                "title" => descending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "createdat" => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                "publishedat" => descending ? query.OrderByDescending(x => x.PublishedAt ?? x.CreatedAt) : query.OrderBy(x => x.PublishedAt ?? x.CreatedAt),
                _ => descending ? query.OrderByDescending(x => x.PublishedAt ?? x.CreatedAt) : query.OrderBy(x => x.PublishedAt ?? x.CreatedAt)
            };
        }
    }
}