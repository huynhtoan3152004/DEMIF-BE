using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Me.GetProgress;
using Demif.Application.Features.Me.GetStreak;
using Demif.Application.Features.Me.RecordActivity;
using Demif.Application.Features.Payments.GetHistory;
using Demif.Application.Features.Subscriptions.GetMySubscription;
using Demif.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// User's own progress, streak, and activity endpoints.
/// All routes require authentication.
/// </summary>
[ApiController]
[Authorize]
[Route(ApiRoutes.Base + "/me")]
[Produces("application/json")]
[Tags("Me")]
public class MeController : ControllerBase
{
    private readonly GetProgressService _getProgressService;
    private readonly GetStreakService _getStreakService;
    private readonly RecordActivityService _recordActivityService;
    private readonly GetMySubscriptionService _getMySubscriptionService;
    private readonly GetPaymentHistoryService _getPaymentHistoryService;
    private readonly ICurrentUserService _currentUserService;

    public MeController(
        GetProgressService getProgressService,
        GetStreakService getStreakService,
        RecordActivityService recordActivityService,
        GetMySubscriptionService getMySubscriptionService,
        GetPaymentHistoryService getPaymentHistoryService,
        ICurrentUserService currentUserService)
    {
        _getProgressService = getProgressService;
        _getStreakService = getStreakService;
        _recordActivityService = recordActivityService;
        _getMySubscriptionService = getMySubscriptionService;
        _getPaymentHistoryService = getPaymentHistoryService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get my learning progress (points, level, completed lessons/exercises).
    /// </summary>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(GetProgressResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress(CancellationToken ct)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _getProgressService.ExecuteAsync(userId, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get my daily learning streak information.
    /// </summary>
    [HttpGet("streak")]
    [ProducesResponseType(typeof(GetStreakResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStreak(CancellationToken ct)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _getStreakService.ExecuteAsync(userId, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Record a learning activity (dictation or shadowing exercise completion).
    /// Updates streak and progress automatically.
    /// </summary>
    [HttpPost("activity")]
    [ProducesResponseType(typeof(RecordActivityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordActivity([FromBody] RecordActivityRequest request, CancellationToken ct)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _recordActivityService.ExecuteAsync(userId, request, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get my current subscription status (tier, end date, days remaining).
    /// </summary>
    [HttpGet("subscription")]
    [ProducesResponseType(typeof(GetMySubscriptionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubscription(CancellationToken ct)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _getMySubscriptionService.ExecuteAsync(userId, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get my payment history (all transactions — Pending, Completed, Failed).
    /// </summary>
    [HttpGet("payment-history")]
    [ProducesResponseType(typeof(GetPaymentHistoryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentHistory(CancellationToken ct)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _getPaymentHistoryService.ExecuteAsync(userId, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }
}
