using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Users.UpdateUserStatus;

/// <summary>
/// UpdateUserStatus Service - thay đổi status user
/// </summary>
public class UpdateUserStatusService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IApplicationDbContext _dbContext;

    public UpdateUserStatusService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _dbContext = dbContext;
    }

    public async Task<Result> ExecuteAsync(
        Guid userId,
        UpdateUserStatusRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate status
        if (!Enum.TryParse<UserStatus>(request.Status, true, out var newStatus))
        {
            return Result.Failure(Error.Validation("Giá trị trạng thái không hợp lệ. Giá trị hợp lệ: Active, Inactive, Suspended, Banned"));
        }

        // 2. Lấy user
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy người dùng."));
        }

        // 3. Cập nhật status
        var oldStatus = user.Status;
        user.Status = newStatus;
        user.UpdatedAt = DateTime.UtcNow;

        // 4. Nếu deactivate/ban thì revoke tất cả tokens
        if (newStatus != UserStatus.Active && oldStatus == UserStatus.Active)
        {
            await _refreshTokenRepository.RevokeAllUserTokensAsync(
                userId,
                $"User status changed from {oldStatus} to {newStatus}",
                ipAddress,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
