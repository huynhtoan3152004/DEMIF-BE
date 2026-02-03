using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Auth.Logout;

/// <summary>
/// Logout Service - xử lý logic đăng xuất và revoke tokens
/// </summary>
public class LogoutService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IApplicationDbContext _dbContext;

    public LogoutService(
        IRefreshTokenRepository refreshTokenRepository,
        IApplicationDbContext dbContext)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _dbContext = dbContext;
    }

    public async Task<Result> ExecuteAsync(
        LogoutRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Tìm refresh token
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(
            request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            // Token không tồn tại, nhưng vẫn return success vì mục đích là logout
            return Result.Success();
        }

        // 2. Revoke token
        if (refreshToken.IsActive)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokeReason = "User logged out";
            refreshToken.RevokedByIp = ipAddress;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    /// <summary>
    /// Đăng xuất khỏi tất cả thiết bị - revoke tất cả token của user
    /// </summary>
    public async Task<Result> LogoutAllDevicesAsync(
        Guid userId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await _refreshTokenRepository.RevokeAllUserTokensAsync(
            userId,
            "User logged out from all devices",
            ipAddress,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
