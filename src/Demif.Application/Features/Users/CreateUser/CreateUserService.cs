using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Users.CreateUser;

/// <summary>
/// CreateUser Service - Admin tạo user mới
/// </summary>
public class CreateUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IApplicationDbContext _dbContext;

    public CreateUserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> ExecuteAsync(
        CreateUserRequest request,
        Guid? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra email đã tồn tại
        if (await _userRepository.ExistsEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Email already exists."));
        }

        // 2. Kiểm tra username đã tồn tại
        if (await _userRepository.ExistsUsernameAsync(request.Username, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Username already exists."));
        }

        // 3. Tạo user mới
        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            Username = request.Username.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = UserStatus.Active,
            Country = request.Country,
            NativeLanguage = request.NativeLanguage,
            TargetLanguage = request.TargetLanguage,
            AvatarUrl = request.AvatarUrl,
            AuthProvider = "email"
        };

        await _userRepository.AddAsync(user, cancellationToken);

        // 4. Gán roles
        foreach (var roleName in request.Roles.Distinct())
        {
            var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (role is not null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = createdBy
                };
                user.UserRoles.Add(userRole);
            }
        }

        // Đảm bảo có ít nhất role User
        if (!user.UserRoles.Any())
        {
            var defaultRole = await _roleRepository.GetDefaultRoleAsync(cancellationToken)
                ?? await _roleRepository.GetByNameAsync("User", cancellationToken);

            if (defaultRole is not null)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = createdBy
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
