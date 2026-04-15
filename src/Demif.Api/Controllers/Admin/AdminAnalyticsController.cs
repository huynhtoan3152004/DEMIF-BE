using Demif.Application.Features.Admin.Analytics;
using Demif.Application.Features.Admin.Analytics.Content;
using Demif.Application.Features.Admin.Analytics.Lessons;
using Demif.Application.Features.Admin.Analytics.Lessons.Access;
using Demif.Application.Features.Admin.Analytics.Overview;
using Demif.Application.Features.Admin.Analytics.Payments;
using Demif.Application.Features.Admin.Analytics.Users;
using Demif.Application.Features.Admin.Analytics.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

[Route("api/admin/analytics")]
[ApiController]
[Authorize(Policy = "RequireAdmin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly GetAdminAnalyticsService _analyticsService;
    private readonly GetAdminAnalyticsOverviewService _overviewService;
    private readonly GetAdminUserAnalyticsService _userAnalyticsService;
    private readonly GetAdminLessonAnalyticsService _lessonAnalyticsService;
    private readonly GetAdminLessonAccessAnalyticsService _lessonAccessAnalyticsService;
    private readonly GetAdminVocabularyAnalyticsService _vocabularyAnalyticsService;
    private readonly GetAdminPaymentAnalyticsService _paymentAnalyticsService;
    private readonly GetAdminContentAnalyticsService _contentAnalyticsService;

    public AdminAnalyticsController(
        GetAdminAnalyticsService analyticsService,
        GetAdminAnalyticsOverviewService overviewService,
        GetAdminUserAnalyticsService userAnalyticsService,
        GetAdminLessonAnalyticsService lessonAnalyticsService,
        GetAdminLessonAccessAnalyticsService lessonAccessAnalyticsService,
        GetAdminVocabularyAnalyticsService vocabularyAnalyticsService,
        GetAdminPaymentAnalyticsService paymentAnalyticsService,
        GetAdminContentAnalyticsService contentAnalyticsService)
    {
        _analyticsService = analyticsService;
        _overviewService = overviewService;
        _userAnalyticsService = userAnalyticsService;
        _lessonAnalyticsService = lessonAnalyticsService;
        _lessonAccessAnalyticsService = lessonAccessAnalyticsService;
        _vocabularyAnalyticsService = vocabularyAnalyticsService;
        _paymentAnalyticsService = paymentAnalyticsService;
        _contentAnalyticsService = contentAnalyticsService;
    }

    /// <summary>
    /// Lấy thống kê tổng quan (DAU, MAU, Bài học tương tác, Phân loại doanh thu Premium/Pro)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.ExecuteAsync(cancellationToken);
        
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _overviewService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _userAnalyticsService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    [HttpGet("lessons")]
    public async Task<IActionResult> GetLessons(CancellationToken cancellationToken)
    {
        var result = await _lessonAnalyticsService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    [HttpGet("lessons/access")]
    public async Task<IActionResult> GetLessonAccess(CancellationToken cancellationToken)
    {
        var result = await _lessonAccessAnalyticsService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    [HttpGet("vocabulary")]
    public async Task<IActionResult> GetVocabulary(CancellationToken cancellationToken)
    {
        var result = await _vocabularyAnalyticsService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(CancellationToken cancellationToken)
    {
        var result = await _paymentAnalyticsService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    [HttpGet("content")]
    public async Task<IActionResult> GetContent(CancellationToken cancellationToken)
    {
        var result = await _contentAnalyticsService.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }
}
