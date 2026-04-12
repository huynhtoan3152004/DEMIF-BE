using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Demif.Application.Features.Auth.ForgotPassword;

public class ForgotPasswordService
{
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordService(
        IUserRepository userRepository,
        IApplicationDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _dbContext = dbContext;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result> ExecuteAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        
        // Trick bảo mật: Báo thành công bất kể có tìm thấy user hay không
        // Giúp chống lộ lọt email (Email enumeration attack)
        if (user == null || user.AuthProvider != "email")
        {
            return Result.Success();
        }

        // Tạo Token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes);

        // Lưu vào DB (sống trong 15 phút)
        user.PasswordResetToken = token;
        user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15);
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Gửi email
        var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        var resetToken = Uri.EscapeDataString(token);
        var resetUrl = $"{frontendUrl}/reset-password?token={resetToken}";

        await _emailService.SendPasswordResetEmailAsync(
            user.Email,
            user.Username,
            resetUrl,
            cancellationToken);

        return Result.Success();
    }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
