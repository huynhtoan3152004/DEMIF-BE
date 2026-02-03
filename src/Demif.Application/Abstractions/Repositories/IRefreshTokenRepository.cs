using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// RefreshToken Repository interface
/// </summary>
public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    /// <summary>
    /// Lấy refresh token theo giá trị token
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy refresh token với thông tin user
    /// </summary>
    Task<RefreshToken?> GetByTokenWithUserAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke tất cả tokens của user
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId, string reason, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa các token đã hết hạn
    /// </summary>
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách active tokens của user
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
