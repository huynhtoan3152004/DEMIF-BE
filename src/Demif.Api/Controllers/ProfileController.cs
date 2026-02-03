using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Auth.ChangePassword;
using Demif.Application.Features.Profile.GetMyProfile;
using Demif.Application.Features.Profile.UpdateMyProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Profile Controller - API cho user xem/cập nhật profile của mình
/// Yêu cầu đăng nhập (any authenticated user)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly GetMyProfileService _getMyProfileService;
    private readonly UpdateMyProfileService _updateMyProfileService;
    private readonly ChangePasswordService _changePasswordService;
    private readonly ICurrentUserService _currentUserService;

    public ProfileController(
        GetMyProfileService getMyProfileService,
        UpdateMyProfileService updateMyProfileService,
        ChangePasswordService changePasswordService,
        ICurrentUserService currentUserService)
    {
        _getMyProfileService = getMyProfileService;
        _updateMyProfileService = updateMyProfileService;
        _changePasswordService = changePasswordService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy profile của user hiện tại
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(GetMyProfileResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var result = await _getMyProfileService.ExecuteAsync(userId.Value, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cập nhật profile của user hiện tại
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var result = await _updateMyProfileService.ExecuteAsync(userId.Value, request, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Conflict" => Conflict(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { message = "Profile updated successfully" });
    }

    /// <summary>
    /// Đổi mật khẩu
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _changePasswordService.ExecuteAsync(userId.Value, request, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Unauthorized" => Unauthorized(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { message = "Password changed successfully. Please login again." });
    }
}
