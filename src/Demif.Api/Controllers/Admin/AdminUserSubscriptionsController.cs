using Demif.Application.Features.Admin.UserSubscriptions;
using Demif.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

/// <summary>
/// Admin endpoints for viewing and managing user subscriptions.
/// </summary>
[ApiController]
[Authorize(Policy = "RequireAdmin")]
[Route(ApiRoutes.Base + "/admin/user-subscriptions")]
[Produces("application/json")]
[Tags("Admin - User Subscriptions")]
public class AdminUserSubscriptionsController : ControllerBase
{
    private readonly AdminUserSubscriptionService _service;

    public AdminUserSubscriptionsController(AdminUserSubscriptionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get paginated list of all user subscriptions with optional filters.
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="status">Filter by status: Active, Expired, Cancelled, PendingPayment</param>
    /// <param name="search">Search by user email or username</param>
    [HttpGet]
    [ProducesResponseType(typeof(AdminUserSubscriptionPagedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.GetAllAsync(page, pageSize, status, search, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get detailed information for a single user subscription including payment history.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserSubscriptionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsFailure)
            return NotFound(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Extend a subscription by a given number of days.
    /// If the subscription is expired, extends from today.
    /// </summary>
    [HttpPatch("{id:guid}/extend")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Extend(Guid id, [FromBody] ExtendSubscriptionRequest request, CancellationToken ct)
    {
        var result = await _service.ExtendAsync(id, request, ct);
        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = result.Value });
    }

    /// <summary>
    /// Cancel a user subscription immediately.
    /// AutoRenew is also disabled.
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelSubscriptionRequest request, CancellationToken ct)
    {
        var result = await _service.CancelAsync(id, request, ct);
        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = result.Value });
    }
}
