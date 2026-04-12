using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Demif.Application.Features.Auth.Login;

/// <summary>
/// Login Service - xử lý logic đăng nhập với hỗ trợ multiple roles
/// </summary>
public class LoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IUserStreakRepository _streakRepo;

    public LoginService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext dbContext,
        IConfiguration configuration,
        IUserStreakRepository streakRepo)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dbContext = dbContext;
        _configuration = configuration;
        _streakRepo = streakRepo;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(
        LoginRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Tìm user theo email với roles
        var user = await _userRepository.GetByEmailWithRolesAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return Result.Failure<LoginResponse>(Error.Unauthorized("Email hoặc mật khẩu không đúng."));
        }

        // 2. Yêu cầu xác nhận email trước khi đăng nhập
        if (!user.IsEmailVerified)
        {
            return Result.Failure<LoginResponse>(
                new Error("Auth.EmailNotVerified", "Email chưa được xác nhận. Vui lòng kiểm tra hộp thư và xác nhận tài khoản."));
        }

        // 3. Kiểm tra user status
        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<LoginResponse>(Error.Unauthorized("Tài khoản của bạn chưa được kích hoạt."));
        }

        // 4. Verify password
        if (user.PasswordHash is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>(Error.Unauthorized("Email hoặc mật khẩu không đúng."));
        }

        // 5. Lấy danh sách roles
        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role.Name)
            .ToList();

        // Fallback nếu user không có role
        if (!roles.Any())
        {
            roles.Add("User");
        }

        // 6. Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        // 7. Tạo refresh token entity
        var refreshTokenHours = int.Parse(_configuration["Jwt:RefreshTokenExpirationHours"] ?? "12");
        var refreshToken = new Domain.Entities.RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(refreshTokenHours),
            CreatedByIp = ipAddress
        };
        _dbContext.RefreshTokens.Add(refreshToken);

        // 8. Cập nhật LastLoginAt
        user.LastLoginAt = DateTime.UtcNow;

        // Bổ sung: Cập nhật Streak đăng nhập
        await UpdateStreakAsync(user.Id, cancellationToken);
        
        // 9. Lưu TẤT CẢ thay đổi 1 LẦN DUY NHẤT
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 10. Return response
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Roles = roles
        };
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

