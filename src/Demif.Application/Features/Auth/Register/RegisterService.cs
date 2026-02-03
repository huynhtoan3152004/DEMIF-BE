using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Demif.Application.Features.Auth.Register;

/// <summary>
/// Register Service - xử lý logic đăng ký tài khoản mới
/// </summary>
public class RegisterService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public RegisterService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<Result<RegisterResponse>> ExecuteAsync(
        RegisterRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra email đã tồn tại chưa
        if (await _userRepository.ExistsEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<RegisterResponse>(Error.Conflict("Email already exists."));
        }

        // 2. Kiểm tra username đã tồn tại chưa
        if (await _userRepository.ExistsUsernameAsync(request.Username, cancellationToken))
        {
            return Result.Failure<RegisterResponse>(Error.Conflict("Username already exists."));
        }

        // 3. Lấy role mặc định (User)
        var defaultRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken);
        if (defaultRole is null)
        {
            // Fallback: tìm role "User"
            defaultRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
            if (defaultRole is null)
            {
                return Result.Failure<RegisterResponse>(Error.Internal("Default role not configured."));
            }
        }

        // 4. Tạo user mới
        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            Username = request.Username.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = UserStatus.Active,
            Country = request.Country,
            NativeLanguage = request.NativeLanguage,
            TargetLanguage = request.TargetLanguage,
            AuthProvider = "email",
            LastLoginAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);

        // 5. Gán role mặc định
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = defaultRole.Id,
            AssignedAt = DateTime.UtcNow
        };
        user.UserRoles.Add(userRole);

        // 6. Tạo tokens
        var roles = new List<string> { defaultRole.Name };
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        // 7. Lưu refresh token vào database
        var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        var refreshToken = new Domain.Entities.RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            CreatedByIp = ipAddress
        };
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        // 8. Lưu tất cả thay đổi
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 9. Trả về response
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        return new RegisterResponse
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
}
