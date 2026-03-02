using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Auth.Register;

/// <summary>
/// Register Service — tạo tài khoản mới + gửi email xác nhận.
/// KHÔNG cấp JWT ngay — user phải verify email trước.
/// </summary>
public class RegisterService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterService> _logger;

    public RegisterService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IApplicationDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<RegisterService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _dbContext = dbContext;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<RegisterResponse>> ExecuteAsync(
        RegisterRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Check email tồn tại
        if (await _userRepository.ExistsEmailAsync(request.Email, cancellationToken))
            return Result.Failure<RegisterResponse>(Error.Conflict("Email đã tồn tại."));

        // 2. Check username tồn tại
        if (await _userRepository.ExistsUsernameAsync(request.Username, cancellationToken))
            return Result.Failure<RegisterResponse>(Error.Conflict("Tên người dùng đã tồn tại."));

        // 3. Lấy default role
        var defaultRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken)
            ?? await _roleRepository.GetByNameAsync("User", cancellationToken);

        if (defaultRole is null)
            return Result.Failure<RegisterResponse>(Error.Internal("Chưa cấu hình vai trò mặc định."));

        // 4. Tạo verification token (hết hạn 24h)
        var verificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        // 5. Tạo user mới — Status = Pending cho đến khi verify email
        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            Username = request.Username.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = UserStatus.Pending,
            Country = request.Country,
            NativeLanguage = request.NativeLanguage,
            TargetLanguage = request.TargetLanguage,
            AuthProvider = "email",
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationExpiry = DateTime.UtcNow.AddHours(24),
            LastLoginAt = null
        };

        // 6. Gán role mặc định
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = defaultRole.Id,
            AssignedAt = DateTime.UtcNow
        };
        user.UserRoles.Add(userRole);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 7. Gửi email xác nhận
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:3000";
        var verifyUrl = $"{frontendUrl}/auth/verify-email?token={verificationToken}";

        await _emailService.SendEmailVerificationAsync(user.Email, user.Username, verifyUrl, cancellationToken);

        _logger.LogInformation("New user registered: {UserId}, email verification sent.", user.Id);

        return Result.Success(new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Message = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.",
            RequiresEmailVerification = true
        });
    }
}
