using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Demif.Application.Features.Auth.VerifyEmail;

/// <summary>
/// Verify email token → activate user → issue JWT.
/// GET /api/auth/verify-email?token=xxx
/// </summary>
public class VerifyEmailService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public VerifyEmailService(
        IApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<Result<VerifyEmailResponse>> ExecuteAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        // 1. Tìm user theo token
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                u => u.EmailVerificationToken == token && !u.IsEmailVerified,
                cancellationToken);

        if (user is null)
            return Result.Failure<VerifyEmailResponse>(
                Error.NotFound("Token không hợp lệ hoặc tài khoản đã được xác nhận."));

        // 2. Check hết hạn
        if (user.EmailVerificationExpiry < DateTime.UtcNow)
            return Result.Failure<VerifyEmailResponse>(
                new Error("Auth.TokenExpired", "Link xác nhận đã hết hạn. Vui lòng yêu cầu gửi lại."));

        // 3. Kích hoạt tài khoản
        user.IsEmailVerified = true;
        user.Status = UserStatus.Active;
        user.EmailVerificationToken = null;
        user.EmailVerificationExpiry = null;
        user.LastLoginAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Cấp JWT ngay để auto-login sau verify
        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role.Name)
            .ToList();

        if (!roles.Any()) roles.Add("User");

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresInMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        // Lưu refresh token
        _dbContext.RefreshTokens.Add(new Domain.Entities.RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7"))
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new VerifyEmailResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Roles = roles,
            Message = "Email xác nhận thành công! Chào mừng đến DEMIF 🎉"
        });
    }
}

public class VerifyEmailResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
