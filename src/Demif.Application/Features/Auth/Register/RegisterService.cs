using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Demif.Application.Features.Auth.Register;

/// <summary>
/// Register Service - xử lý logic đăng ký tài khoản mới
/// </summary>
public class RegisterService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public RegisterService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IApplicationDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _dbContext = dbContext;
        _emailService = emailService;
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
            return Result.Failure<RegisterResponse>(Error.Conflict("Email đã tồn tại."));
        }

        // 2. Kiểm tra username đã tồn tại chưa
        if (await _userRepository.ExistsUsernameAsync(request.Username, cancellationToken))
        {
            return Result.Failure<RegisterResponse>(Error.Conflict("Tên người dùng đã tồn tại."));
        }

        // 3. Lấy role mặc định (User)
        var defaultRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken);
        if (defaultRole is null)
        {
            // Fallback: tìm role "User"
            defaultRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
            if (defaultRole is null)
            {
                return Result.Failure<RegisterResponse>(Error.Internal("Chưa cấu hình vai trò mặc định."));
            }
        }

        // 4. Tạo user mới
        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            Username = request.Username.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = UserStatus.Pending,
            AuthProvider = "email",
            EmailVerificationToken = GenerateEmailVerificationToken(),
            EmailVerificationExpiry = DateTime.UtcNow.AddHours(24)
        };

        // 5. Gán role mặc định
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = defaultRole.Id,
            AssignedAt = DateTime.UtcNow
        };
        user.UserRoles.Add(userRole);

        // 6. Thêm user vào context (CHƯA save)
        _dbContext.Users.Add(user);

        // 7. Lưu TẤT CẢ thay đổi 1 LẦN DUY NHẤT
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 8. Gửi email xác nhận
        var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        var token = Uri.EscapeDataString(user.EmailVerificationToken!);
        var verifyUrl = $"{frontendUrl}/verify-email?token={token}";

        await _emailService.SendEmailVerificationAsync(
            user.Email,
            user.Username,
            verifyUrl,
            cancellationToken);

        // 9. Trả về response
        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Message = "Vui lòng kiểm tra email để xác nhận tài khoản.",
            RequiresEmailVerification = true
        };
    }

    private static string GenerateEmailVerificationToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes);
    }
}
