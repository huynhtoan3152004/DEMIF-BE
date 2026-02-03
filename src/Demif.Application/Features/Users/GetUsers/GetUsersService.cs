using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Users.GetUsers;

/// <summary>
/// GetUsers Service - lấy danh sách users với pagination
/// </summary>
public class GetUsersService
{
    private readonly IUserRepository _userRepository;

    public GetUsersService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetUsersResponse>> ExecuteAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination
        if (request.Page < 1) request.Page = 1;
        if (request.PageSize < 1) request.PageSize = 10;
        if (request.PageSize > 100) request.PageSize = 100; // Max page size

        var (users, totalCount) = await _userRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            request.Status,
            cancellationToken);

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            Username = u.Username,
            AvatarUrl = u.AvatarUrl,
            Status = u.Status.ToString(),
            Roles = u.UserRoles
                .Where(ur => ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow)
                .Select(ur => ur.Role.Name)
                .ToList(),
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return new GetUsersResponse
        {
            Users = userDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
