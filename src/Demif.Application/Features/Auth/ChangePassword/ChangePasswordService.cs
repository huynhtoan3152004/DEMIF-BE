using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Auth.ChangePassword;

/// <summary>
/// ChangePassword Service - xử lý logic đổi mật khẩu
/// </summary>
public class ChangePasswordService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IApplicationDbContext _dbContext;

    public ChangePasswordService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _dbContext = dbContext;
    }

    public async Task<Result> ExecuteAsync(
        Guid userId,
        ChangePasswordRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy user
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("User not found."));
        }

        // 2. Verify mật khẩu hiện tại
        if (user.PasswordHash is null || !_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(Error.Unauthorized("Current password is incorrect."));
        }

        // 3. Kiểm tra mật khẩu mới không trùng mật khẩu cũ
        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            return Result.Failure(Error.Validation("New password must be different from current password."));
        }

        // 4. Hash mật khẩu mới và cập nhật
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // 5. Revoke tất cả refresh tokens (bắt buộc login lại)
        await _refreshTokenRepository.RevokeAllUserTokensAsync(
            userId,
            "Password changed",
            ipAddress,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
