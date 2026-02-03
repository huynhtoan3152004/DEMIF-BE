using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// Role Repository interface
/// </summary>
public interface IRoleRepository : IGenericRepository<Role>
{
    /// <summary>
    /// Lấy role theo tên
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy role mặc định cho user mới
    /// </summary>
    Task<Role?> GetDefaultRoleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách roles của user
    /// </summary>
    Task<IEnumerable<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả roles active
    /// </summary>
    Task<IEnumerable<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default);
}
