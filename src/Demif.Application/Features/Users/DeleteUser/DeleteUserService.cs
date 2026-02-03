using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Users.DeleteUser;

/// <summary>
/// DeleteUser Service - soft delete user
/// </summary>
public class DeleteUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IApplicationDbContext _dbContext;

    public DeleteUserService(
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
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy người dùng."));
        }

        // Soft delete: set status to Inactive
        user.Status = UserStatus.Inactive;
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke tất cả tokens
        await _refreshTokenRepository.RevokeAllUserTokensAsync(
            userId,
            "User deleted",
            ipAddress,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
