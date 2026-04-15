using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories
{
    public interface IBlogRepository : IGenericRepository<Blog>
    {
        Task<(IReadOnlyList<Blog> Items, int TotalCount)> GetPagedAsync(
            string? search,
            string? category,
            string? tag,
            string? status,
            bool includeDeleted,
            string sortBy,
            string sortDirection,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<Blog?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default);

        Task<Blog?> GetByIdWithAuthorAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);

        Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    }
}