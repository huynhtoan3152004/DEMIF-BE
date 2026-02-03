using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Users.AssignRole;

/// <summary>
/// AssignRole Service - gán role cho user
/// </summary>
public class AssignRoleService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IApplicationDbContext _dbContext;

    public AssignRoleService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _dbContext = dbContext;
    }

    public async Task<Result> ExecuteAsync(
        Guid userId,
        AssignRoleRequest request,
        Guid? assignedBy = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra user tồn tại
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy người dùng."));
        }

        // 2. Kiểm tra role tồn tại
        var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound($"Không tìm thấy vai trò '{request.RoleName}'."));
        }

        // 3. Kiểm tra user đã có role này chưa (không tính role đã hết hạn)
        var existingRole = user.UserRoles.FirstOrDefault(ur =>
            ur.RoleId == role.Id &&
            (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow));

        if (existingRole is not null)
        {
            return Result.Failure(Error.Conflict($"Người dùng đã có vai trò '{request.RoleName}'."));
        }

        // 4. Gán role
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = role.Id,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy,
            ExpiresAt = request.ExpiresAt
        };

        user.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
