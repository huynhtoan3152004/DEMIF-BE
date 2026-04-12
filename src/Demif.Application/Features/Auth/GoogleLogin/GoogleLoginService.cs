using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Auth.GoogleLogin;

/// <summary>
/// Google Login Service — thay thế FirebaseLoginService.
/// Flow: NextAuth.js → Google ID Token → Backend verify → JWT
/// </summary>
public class GoogleLoginService
{
    private readonly IGoogleAuthService _googleAuth;
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleLoginService> _logger;
    private readonly Application.Abstractions.Repositories.IUserStreakRepository _streakRepo;

    public GoogleLoginService(
        IGoogleAuthService googleAuth,
        IApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<GoogleLoginService> logger,
        Application.Abstractions.Repositories.IUserStreakRepository streakRepo)
    {
        _googleAuth = googleAuth;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _logger = logger;
        _streakRepo = streakRepo;
    }

    public async Task<Result<GoogleLoginResponse>> ExecuteAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify Google ID Token
        GoogleUserInfo googleUser;
        try
        {
            googleUser = await _googleAuth.VerifyIdTokenAsync(request.IdToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Failure<GoogleLoginResponse>(new Error("Auth.InvalidGoogleToken", ex.Message));
        }

        _logger.LogInformation("Google login: {Email}", googleUser.Email);

        // 2. Tìm user trong DB theo GoogleId hoặc Email
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                u => u.GoogleId == googleUser.GoogleId || u.Email == googleUser.Email,
                cancellationToken);

        bool isNewUser = false;

        if (user is null)
        {
            // 3. Tạo user mới từ Google — email đã verified bởi Google
            isNewUser = true;
            user = new User
            {
                Email = googleUser.Email,
                Username = googleUser.Name ?? googleUser.Email.Split('@')[0],
                GoogleId = googleUser.GoogleId,
                AuthProvider = "google",
                AvatarUrl = googleUser.AvatarUrl,
                IsEmailVerified = true,  // Google đã verify email rồi
                Status = UserStatus.Active,
                LastLoginAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);

            var defaultRole = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.IsDefault && r.IsActive, cancellationToken);

            if (defaultRole is not null)
            {
                _dbContext.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Reload với roles
            user = await _dbContext.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == user.Id, cancellationToken);
        }
        else
        {
            // Update GoogleId nếu login email cũ bằng Google lần đầu
            if (user.GoogleId is null) user.GoogleId = googleUser.GoogleId;
            if (!user.IsEmailVerified) user.IsEmailVerified = true;
            if (user.Status != UserStatus.Active) user.Status = UserStatus.Active;
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Bổ sung: Cập nhật Streak đăng nhập
        await UpdateStreakAsync(user.Id, cancellationToken);

        // 4. Build JWT
        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role.Name)
            .ToList();

        if (!roles.Any()) roles.Add("User");

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        _dbContext.RefreshTokens.Add(new Domain.Entities.RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(int.Parse(_configuration["Jwt:RefreshTokenExpirationHours"] ?? "12"))
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        var expiresInMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        return Result.Success(new GoogleLoginResponse(
            UserId: user.Id,
            Email: user.Email,
            Username: user.Username,
            AvatarUrl: user.AvatarUrl,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Roles: roles,
            IsNewUser: isNewUser
        ));
    }

    private async Task UpdateStreakAsync(Guid userId, CancellationToken cancellationToken)
    {
        var streak = await _streakRepo.GetByUserIdAsync(userId, cancellationToken)
                     ?? new UserStreak { UserId = userId, FreezesAvailable = 1 };

        var today = DateTime.UtcNow.Date;

        if (streak.LastActiveDate is null)
        {
            streak.CurrentStreak = 1;
            streak.LongestStreak = 1;
            streak.TotalActiveDays = 1;
        }
        else
        {
            var lastDate = streak.LastActiveDate.Value.Date;

            if (lastDate == today)
            {
                // Unchanged
            }
            else if (lastDate == today.AddDays(-1))
            {
                streak.CurrentStreak++;
                streak.TotalActiveDays++;
                if (streak.CurrentStreak > streak.LongestStreak)
                    streak.LongestStreak = streak.CurrentStreak;
            }
            else
            {
                // Gián đoạn, reset khôi phục
                streak.CurrentStreak = 1;
                streak.TotalActiveDays++;
            }
        }

        streak.LastActiveDate = today;
        await _streakRepo.UpsertAsync(streak, cancellationToken);
    }
}

public record GoogleLoginRequest(string IdToken);

public record GoogleLoginResponse(
    Guid UserId,
    string Email,
    string Username,
    string? AvatarUrl,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    List<string> Roles,
    bool IsNewUser
);
