using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Profile.UpdateMyProfile;

/// <summary>
/// UpdateMyProfile Service - cập nhật profile của user hiện tại
/// </summary>
public class UpdateMyProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _dbContext;

    public UpdateMyProfileService(
        IUserRepository userRepository,
        IApplicationDbContext dbContext)
    {
        _userRepository = userRepository;
        _dbContext = dbContext;
    }

    public async Task<Result> ExecuteAsync(
        Guid userId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy người dùng."));
        }

        // Kiểm tra username nếu có cập nhật
        if (!string.IsNullOrWhiteSpace(request.Username) &&
            request.Username.Trim().ToLower() != user.Username.ToLower())
        {
            if (await _userRepository.ExistsUsernameAsync(request.Username, cancellationToken))
            {
                return Result.Failure(Error.Conflict("Tên người dùng đã tồn tại."));
            }
            user.Username = request.Username.Trim();
        }

        // Cập nhật các field khác nếu có
        if (request.AvatarUrl is not null)
            user.AvatarUrl = request.AvatarUrl;

        if (!string.IsNullOrWhiteSpace(request.Country))
            user.Country = request.Country;

        if (!string.IsNullOrWhiteSpace(request.NativeLanguage))
            user.NativeLanguage = request.NativeLanguage;

        if (!string.IsNullOrWhiteSpace(request.TargetLanguage))
            user.TargetLanguage = request.TargetLanguage;

        if (!string.IsNullOrWhiteSpace(request.CurrentLevel) &&
            Enum.TryParse<Level>(request.CurrentLevel, true, out var level))
            user.CurrentLevel = level;

        if (request.DailyGoalMinutes.HasValue && request.DailyGoalMinutes.Value > 0)
            user.DailyGoalMinutes = request.DailyGoalMinutes.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
