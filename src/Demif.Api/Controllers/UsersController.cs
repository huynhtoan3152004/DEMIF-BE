using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Users.AssignRole;
using Demif.Application.Features.Users.CreateUser;
using Demif.Application.Features.Users.DeleteUser;
using Demif.Application.Features.Users.GetUserById;
using Demif.Application.Features.Users.GetUsers;
using Demif.Application.Features.Users.RemoveRole;
using Demif.Application.Features.Users.UpdateUser;
using Demif.Application.Features.Users.UpdateUserStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Users Controller - API quản lý users cho Admin
/// Yêu cầu role Admin để truy cập
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class UsersController : ControllerBase
{
    private readonly GetUsersService _getUsersService;
    private readonly GetUserByIdService _getUserByIdService;
    private readonly CreateUserService _createUserService;
    private readonly UpdateUserService _updateUserService;
    private readonly UpdateUserStatusService _updateUserStatusService;
    private readonly DeleteUserService _deleteUserService;
    private readonly AssignRoleService _assignRoleService;
    private readonly RemoveRoleService _removeRoleService;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(
        GetUsersService getUsersService,
        GetUserByIdService getUserByIdService,
        CreateUserService createUserService,
        UpdateUserService updateUserService,
        UpdateUserStatusService updateUserStatusService,
        DeleteUserService deleteUserService,
        AssignRoleService assignRoleService,
        RemoveRoleService removeRoleService,
        ICurrentUserService currentUserService)
    {
        _getUsersService = getUsersService;
        _getUserByIdService = getUserByIdService;
        _createUserService = createUserService;
        _updateUserService = updateUserService;
        _updateUserStatusService = updateUserStatusService;
        _deleteUserService = deleteUserService;
        _assignRoleService = assignRoleService;
        _removeRoleService = removeRoleService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách users với pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetUsersResponse), 200)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _getUsersService.ExecuteAsync(request, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Lấy chi tiết user theo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetUserByIdResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getUserByIdService.ExecuteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Tạo user mới
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var createdBy = _currentUserService.UserId;
        var result = await _createUserService.ExecuteAsync(request, createdBy, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Conflict" => Conflict(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return CreatedAtAction(nameof(GetUserById), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>
    /// Cập nhật thông tin user
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateUserService.ExecuteAsync(id, request, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                "Conflict" => Conflict(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { message = "User updated successfully" });
    }

    /// <summary>
    /// Xóa user (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _deleteUserService.ExecuteAsync(id, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Message });
        }

        return Ok(new { message = "User deleted successfully" });
    }

    /// <summary>
    /// Thay đổi status user (Activate/Deactivate/Ban)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _updateUserStatusService.ExecuteAsync(id, request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { message = "User status updated successfully" });
    }

    /// <summary>
    /// Gán role cho user
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> AssignRole(
        Guid id,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var assignedBy = _currentUserService.UserId;
        var result = await _assignRoleService.ExecuteAsync(id, request, assignedBy, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                "Conflict" => Conflict(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { message = "Role assigned successfully" });
    }

    /// <summary>
    /// Xóa role khỏi user
    /// </summary>
    [HttpDelete("{id:guid}/roles/{roleName}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveRole(
        Guid id,
        string roleName,
        CancellationToken cancellationToken)
    {
        var result = await _removeRoleService.ExecuteAsync(id, roleName, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { message = "Role removed successfully" });
    }
}
