using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Auth.ForgotPassword;

public class ResetPasswordService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public ResetPasswordService(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result> ExecuteAsync(ResetPasswordRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user == null || user.PasswordResetExpiry < DateTime.UtcNow)
        {
            return Result.Failure(Error.Validation("Link đổi mật khẩu đã hết hạn hoặc không hợp lệ."));
        }

        // Cập nhật pass mới
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        
        // Huỷ token cũ để không dùng lại được
        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        // Bắt buộc đăng xuất các nơi khác bằng cách Revoke toàn bộ Refresh Tokens
        await _refreshTokenRepository.RevokeAllUserTokensAsync(
            user.Id,
            "Reset password",
            ipAddress,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
