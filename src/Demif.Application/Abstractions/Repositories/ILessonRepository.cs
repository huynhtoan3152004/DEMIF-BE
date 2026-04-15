using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// Lesson Repository interface
/// </summary>
public interface ILessonRepository : IGenericRepository<Lesson>
{
    Task<IEnumerable<Lesson>> GetByLevelAsync(string level, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetPublishedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy lessons với pagination và filtering
    /// </summary>
    Task<(IEnumerable<Lesson> Items, int TotalCount)> GetPaginatedAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy lessons cho user. Login = full catalog, Guest = free only.
    /// </summary>
    Task<(IEnumerable<Lesson> Items, int TotalCount)> GetForUserAsync(
        int page,
        int pageSize,
        bool isLoggedIn,
        string? level = null,
        string? type = null,
        string? category = null,
        string? mediaType = null,
        string? tag = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số lessons
    /// </summary>
    Task<int> CountAsync(bool? isPremiumOnly = null, CancellationToken cancellationToken = default);
}

