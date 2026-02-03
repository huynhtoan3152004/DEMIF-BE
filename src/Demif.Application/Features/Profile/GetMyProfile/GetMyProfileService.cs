using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Profile.GetMyProfile;

/// <summary>
/// GetMyProfile Service - lấy profile của user hiện tại
/// </summary>
public class GetMyProfileService
{
    private readonly IUserRepository _userRepository;

    public GetMyProfileService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetMyProfileResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<GetMyProfileResponse>(Error.NotFound("User not found."));
        }

        return new GetMyProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            Country = user.Country,
            NativeLanguage = user.NativeLanguage,
            TargetLanguage = user.TargetLanguage,
            CurrentLevel = user.CurrentLevel.ToString(),
            DailyGoalMinutes = user.DailyGoalMinutes,
            Roles = user.UserRoles
                .Where(ur => ur.Role.IsActive && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
                .Select(ur => ur.Role.Name)
                .ToList(),
            AuthProvider = user.AuthProvider,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
