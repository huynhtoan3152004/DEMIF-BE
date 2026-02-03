using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Users.RemoveRole;

/// <summary>
/// RemoveRole Service - xóa role khỏi user
/// </summary>
public class RemoveRoleService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IApplicationDbContext _dbContext;

    public RemoveRoleService(
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
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra user tồn tại
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy người dùng."));
        }

        // 2. Kiểm tra role tồn tại
        var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound($"Không tìm thấy vai trò '{roleName}'."));
        }

        // 3. Tìm user role
        var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == role.Id);
        if (userRole is null)
        {
            return Result.Failure(Error.NotFound($"Người dùng không có vai trò '{roleName}'."));
        }

        // 4. Không cho phép xóa role cuối cùng
        var activeRolesCount = user.UserRoles.Count(ur =>
            ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow);
        
        if (activeRolesCount <= 1)
        {
            return Result.Failure(Error.Validation("Không thể xóa vai trò cuối cùng của người dùng."));
        }

        // 5. Xóa role
        user.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
