using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// User Repository interface
/// </summary>
public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy user với thông tin roles
    /// </summary>
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy user theo email với roles
    /// </summary>
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách users với pagination
    /// </summary>
    Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm users theo email hoặc username
    /// </summary>
    Task<IEnumerable<User>> SearchAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default);
}

