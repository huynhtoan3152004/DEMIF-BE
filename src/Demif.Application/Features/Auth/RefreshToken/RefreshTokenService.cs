using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Microsoft.Extensions.Configuration;

namespace Demif.Application.Features.Auth.RefreshToken;

/// <summary>
/// RefreshToken Service - xử lý logic refresh access token
/// Hỗ trợ token rotation để bảo mật hơn
/// </summary>
public class RefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public RefreshTokenService(
        IRefreshTokenRepository refreshTokenRepository,
        IRoleRepository roleRepository,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _roleRepository = roleRepository;
        _jwtTokenService = jwtTokenService;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<Result<RefreshTokenResponse>> ExecuteAsync(
        RefreshTokenRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Tìm refresh token trong database
        var existingToken = await _refreshTokenRepository.GetByTokenWithUserAsync(
            request.RefreshToken, cancellationToken);

        if (existingToken is null)
        {
            return Result.Failure<RefreshTokenResponse>(Error.Unauthorized("Invalid refresh token."));
        }

        // 2. Kiểm tra token còn hiệu lực không
        if (!existingToken.IsActive)
        {
            // Token đã bị revoke hoặc hết hạn
            // Revoke tất cả token của user nếu token đã bị revoke (có thể bị đánh cắp)
            if (existingToken.IsRevoked)
            {
                await _refreshTokenRepository.RevokeAllUserTokensAsync(
                    existingToken.UserId,
                    "Attempted reuse of revoked token",
                    ipAddress,
                    cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return Result.Failure<RefreshTokenResponse>(Error.Unauthorized("Token is no longer valid."));
        }

        var user = existingToken.User;
        if (user is null)
        {
            return Result.Failure<RefreshTokenResponse>(Error.NotFound("User not found."));
        }

        // 3. Revoke token cũ (token rotation)
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.RevokeReason = "Replaced by new token";
        existingToken.RevokedByIp = ipAddress;

        // 4. Tạo token mới
        var roles = await _roleRepository.GetUserRoleNamesAsync(user.Id, cancellationToken);
        var roleList = roles.ToList();
        
        // Fallback nếu user không có role
        if (!roleList.Any())
        {
            roleList.Add("User");
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roleList);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        // 5. Lưu refresh token mới
        var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            Token = newRefreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            CreatedByIp = ipAddress
        };

        existingToken.ReplacedByToken = newRefreshTokenValue;
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 6. Trả về response
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        return new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
    }
}
