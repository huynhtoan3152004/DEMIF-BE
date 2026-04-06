using Demif.Application.Features.Subscriptions.CancelSubscription;
using Demif.Application.Features.Subscriptions.GetMySubscription;
using Demif.Application.Features.Subscriptions.GetPlans;
using Demif.Application.Features.Subscriptions.Subscribe;
using Demif.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Subscription Plans — Browse plans, subscribe, manage subscription
/// </summary>
[Route("api/subscription-plans")]
[ApiController]
public class SubscriptionPlansController : ControllerBase
{
    private readonly GetPlansService _getPlansService;
    private readonly SubscribeService _subscribeService;
    private readonly GetMySubscriptionService _getMySubscriptionService;
    private readonly CancelSubscriptionService _cancelSubscriptionService;
    private readonly ICurrentUserService _currentUserService;

    public SubscriptionPlansController(
        GetPlansService getPlansService,
        SubscribeService subscribeService,
        GetMySubscriptionService getMySubscriptionService,
        CancelSubscriptionService cancelSubscriptionService,
        ICurrentUserService currentUserService)
    {
        _getPlansService = getPlansService;
        _subscribeService = subscribeService;
        _getMySubscriptionService = getMySubscriptionService;
        _cancelSubscriptionService = cancelSubscriptionService;
        _currentUserService = currentUserService;
    }

    // ═══════════════════════════════════════════════════════════════
    // Public — Browse Plans
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// List all active subscription plans.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var result = await _getPlansService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Authenticated — Manage My Subscription
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Subscribe to a Premium plan (creates pending payment).
    /// </summary>
    [HttpPost("subscribe")]
    [Authorize(Policy = "RequireUser")]
    public async Task<IActionResult> Subscribe(
        [FromBody] SubscribeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _subscribeService.ExecuteAsync(userId, request, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                "Conflict" => Conflict(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get current user's active subscription.
    /// </summary>
    [HttpGet("my-subscription")]
    [Authorize(Policy = "RequireUser")]
    public async Task<IActionResult> GetMySubscription(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _getMySubscriptionService.ExecuteAsync(userId, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Cancel auto-renewal for current subscription.
    /// </summary>
    [HttpPost("cancel")]
    [Authorize(Policy = "RequireUser")]
    public async Task<IActionResult> CancelSubscription(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _cancelSubscriptionService.ExecuteAsync(userId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { message = "Đã hủy tự động gia hạn. Gói của bạn sẽ hết hạn theo thời gian còn lại." });
    }

}
