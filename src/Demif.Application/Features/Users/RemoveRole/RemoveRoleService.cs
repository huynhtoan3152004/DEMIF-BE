using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Users.RemoveRole;

/// <summary>
/// RemoveRole Service - xóa role khỏi user
/// Fix: dùng direct DbContext query thay vì navigation property để tránh tracking issues
/// </summary>
public class RemoveRoleService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<RemoveRoleService> _logger;

    public RemoveRoleService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IApplicationDbContext dbContext,
        ILogger<RemoveRoleService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Kiểm tra user tồn tại
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("RemoveRole failed: User {UserId} not found", userId);
                return Result.Failure(Error.NotFound("Không tìm thấy người dùng."));
            }

            // 2. Kiểm tra role tồn tại
            var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (role is null)
            {
                _logger.LogWarning("RemoveRole failed: Role '{RoleName}' not found", roleName);
                return Result.Failure(Error.NotFound($"Không tìm thấy vai trò '{roleName}'."));
            }

            // 3. Tìm user role trực tiếp từ DB (tránh tracking issues từ navigation property)
            var userRole = await _dbContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, cancellationToken);

            if (userRole is null)
            {
                _logger.LogWarning("RemoveRole failed: User {UserId} does not have role '{RoleName}'", userId, roleName);
                return Result.Failure(Error.NotFound($"Người dùng không có vai trò '{roleName}'."));
            }

            // 4. Đếm active roles — không cho phép xóa role cuối cùng
            var activeRolesCount = await _dbContext.UserRoles
                .CountAsync(ur => ur.UserId == userId &&
                    (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow), cancellationToken);

            if (activeRolesCount <= 1)
            {
                _logger.LogWarning("RemoveRole failed: Cannot remove last active role for user {UserId}", userId);
                return Result.Failure(Error.Validation("Không thể xóa vai trò cuối cùng của người dùng."));
            }

            // 5. Xóa role trực tiếp từ DbContext
            _dbContext.UserRoles.Remove(userRole);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully removed role '{RoleName}' from user {UserId}", roleName, userId);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while removing role '{RoleName}' from user {UserId}. Inner: {InnerMessage}",
                roleName, userId, ex.InnerException?.Message);
            return Result.Failure(Error.Internal($"Lỗi database khi xóa role: {ex.InnerException?.Message ?? ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while removing role '{RoleName}' from user {UserId}", roleName, userId);
            return Result.Failure(Error.Internal("Đã xảy ra lỗi không mong đợi khi xóa role."));
        }
    }
}
