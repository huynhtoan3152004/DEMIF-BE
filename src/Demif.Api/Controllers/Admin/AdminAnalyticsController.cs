using Demif.Application.Features.Admin.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

[Route("api/admin/analytics")]
[ApiController]
[Authorize(Policy = "RequireAdmin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly GetAdminAnalyticsService _analyticsService;

    public AdminAnalyticsController(GetAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
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
}
