using Demif.Application.Features.Subscriptions.CancelSubscription;
using Demif.Application.Features.Subscriptions.GetMySubscription;
using Demif.Application.Features.Subscriptions.GetPlans;
using Demif.Application.Features.Subscriptions.Subscribe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// API Controller cho Subscription Plans
/// </summary>
[Route("api/subscription-plans")]
[ApiController]
public class SubscriptionPlansController : ControllerBase
{
    private readonly GetPlansService _getPlansService;
    private readonly SubscribeService _subscribeService;
    private readonly GetMySubscriptionService _getMySubscriptionService;
    private readonly CancelSubscriptionService _cancelSubscriptionService;

    public SubscriptionPlansController(
        GetPlansService getPlansService,
        SubscribeService subscribeService,
        GetMySubscriptionService getMySubscriptionService,
        CancelSubscriptionService cancelSubscriptionService)
    {
        _getPlansService = getPlansService;
        _subscribeService = subscribeService;
        _getMySubscriptionService = getMySubscriptionService;
        _cancelSubscriptionService = cancelSubscriptionService;
    }

    /// <summary>
    /// Lấy danh sách các gói subscription đang active
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var result = await _getPlansService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Đăng ký gói Premium (tạo payment pending)
    /// </summary>
    [HttpPost("subscribe")]
    [Authorize(Policy = "RequireUser")]
    public async Task<IActionResult> Subscribe(
        [FromBody] SubscribeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
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
    /// Lấy subscription hiện tại của user đang đăng nhập
    /// </summary>
    [HttpGet("my-subscription")]
    [Authorize(Policy = "RequireUser")]
    public async Task<IActionResult> GetMySubscription(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _getMySubscriptionService.ExecuteAsync(userId, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Hủy auto-renew subscription
    /// </summary>
    [HttpPost("cancel")]
    [Authorize(Policy = "RequireUser")]
    public async Task<IActionResult> CancelSubscription(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
