using Demif.Api.Contracts;
using Demif.Application.Features.Admin.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = "RequireAdmin")]
[Route(ApiRoutes.Base + "/admin/notifications")]
[Tags("Admin - Notifications")]
public class AdminNotificationsController : ControllerBase
{
    private readonly BroadcastSystemNotificationService _notificationService;

    public AdminNotificationsController(BroadcastSystemNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gửi thông báo hệ thống đến toàn bộ user đủ điều kiện.
    /// </summary>
    [HttpPost("broadcast")]
    [ProducesResponseType(typeof(BroadcastSystemNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastSystemNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = await _notificationService.ExecuteAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message, code = result.Error.Code });
        }

        return Ok(result.Value);
    }
}