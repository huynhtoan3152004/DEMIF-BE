using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Users.AssignRole;

/// <summary>
/// AssignRole Service - gán role cho user
/// Fix: handle unique constraint (UserId, RoleId) khi re-assign expired role
/// </summary>
public class AssignRoleService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<AssignRoleService> _logger;

    public AssignRoleService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IApplicationDbContext dbContext,
        ILogger<AssignRoleService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(
        Guid userId,
        AssignRoleRequest request,
        Guid? assignedBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Kiểm tra user tồn tại
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("AssignRole failed: User {UserId} not found", userId);
                return Result.Failure(Error.NotFound("Không tìm thấy người dùng."));
            }

            // 2. Kiểm tra role tồn tại
            var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
            if (role is null)
            {
                _logger.LogWarning("AssignRole failed: Role '{RoleName}' not found", request.RoleName);
                return Result.Failure(Error.NotFound($"Không tìm thấy vai trò '{request.RoleName}'."));
            }

            // 3. Query trực tiếp DB để tìm UserRole (kể cả expired) — tránh unique constraint violation
            var existingUserRole = await _dbContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, cancellationToken);

            if (existingUserRole is not null)
            {
                // 3a. Nếu role còn active → Conflict
                if (existingUserRole.ExpiresAt == null || existingUserRole.ExpiresAt > DateTime.UtcNow)
                {
                    _logger.LogWarning("AssignRole failed: User {UserId} already has active role '{RoleName}'", userId, request.RoleName);
                    return Result.Failure(Error.Conflict($"Người dùng đã có vai trò '{request.RoleName}'."));
                }

                // 3b. Nếu role đã expired → re-activate (update row cũ thay vì insert mới)
                _logger.LogInformation("Re-activating expired role '{RoleName}' for user {UserId}", request.RoleName, userId);
                existingUserRole.AssignedAt = DateTime.UtcNow;
                existingUserRole.AssignedBy = assignedBy;
                existingUserRole.ExpiresAt = request.ExpiresAt;
                existingUserRole.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // 4. Chưa có row nào → tạo mới
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = assignedBy,
                    ExpiresAt = request.ExpiresAt
                };

                _dbContext.UserRoles.Add(userRole);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully assigned role '{RoleName}' to user {UserId}", request.RoleName, userId);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while assigning role '{RoleName}' to user {UserId}. Inner: {InnerMessage}",
                request.RoleName, userId, ex.InnerException?.Message);
            return Result.Failure(Error.Internal($"Lỗi database khi gán role: {ex.InnerException?.Message ?? ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while assigning role '{RoleName}' to user {UserId}", request.RoleName, userId);
            return Result.Failure(Error.Internal("Đã xảy ra lỗi không mong đợi khi gán role."));
        }
    }
}
