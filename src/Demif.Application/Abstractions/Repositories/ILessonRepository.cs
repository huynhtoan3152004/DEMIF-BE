using Demif.Domain.Entities;
using Demif.Domain.Enums;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// Lesson Repository interface
/// </summary>
public interface ILessonRepository : IGenericRepository<Lesson>
{
    Task<IEnumerable<Lesson>> GetByLevelAsync(Level level, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetByTypeAsync(LessonType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetPublishedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy lessons với pagination và filtering
    /// </summary>
    Task<(IEnumerable<Lesson> Items, int TotalCount)> GetPaginatedAsync(
        int page,
        int pageSize,
        Level? level = null,
        LessonType? type = null,
        string? category = null,
        bool? isPremiumOnly = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy lessons cho user (lọc premium nếu user không có subscription)
    /// </summary>
    Task<(IEnumerable<Lesson> Items, int TotalCount)> GetForUserAsync(
        int page,
        int pageSize,
        bool hasPremiumAccess,
        Level? level = null,
        LessonType? type = null,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số lessons
    /// </summary>
    Task<int> CountAsync(bool? isPremiumOnly = null, CancellationToken cancellationToken = default);
}

