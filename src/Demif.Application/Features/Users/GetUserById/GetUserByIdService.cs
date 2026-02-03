using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Users.GetUserById;

/// <summary>
/// GetUserById Service - lấy chi tiết user theo ID
/// </summary>
public class GetUserByIdService
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetUserByIdResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<GetUserByIdResponse>(Error.NotFound("User not found."));
        }

        var response = new GetUserByIdResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status.ToString(),
            Country = user.Country,
            NativeLanguage = user.NativeLanguage,
            TargetLanguage = user.TargetLanguage,
            CurrentLevel = user.CurrentLevel.ToString(),
            DailyGoalMinutes = user.DailyGoalMinutes,
            AuthProvider = user.AuthProvider,
            Roles = user.UserRoles.Select(ur => new UserRoleDto
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role.Name,
                AssignedAt = ur.AssignedAt,
                ExpiresAt = ur.ExpiresAt
            }).ToList(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return response;
    }
}
