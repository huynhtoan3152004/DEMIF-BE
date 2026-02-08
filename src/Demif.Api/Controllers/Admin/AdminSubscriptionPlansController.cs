using Demif.Application.Features.Subscriptions.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

/// <summary>
/// Admin API Controller cho quản lý Subscription Plans
/// </summary>
[Route("api/admin/subscription-plans")]
[ApiController]
[Authorize(Policy = "RequireAdmin")]
public class AdminSubscriptionPlansController : ControllerBase
{
    private readonly AdminSubscriptionPlanService _adminService;

    public AdminSubscriptionPlansController(AdminSubscriptionPlanService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Lấy tất cả plans với thống kê người đăng ký
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetAllWithStatsAsync(cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Lấy thống kê tổng quan
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetAllWithStatsAsync(cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(new
        {
            result.Value.TotalPlans,
            result.Value.TotalSubscribers,
            result.Value.ActiveSubscribers,
            result.Value.TotalRevenue
        });
    }

    /// <summary>
    /// Tạo plan mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUpdatePlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return CreatedAtAction(nameof(GetAll), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>
    /// Cập nhật plan (bao gồm giá)
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CreateUpdatePlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Xóa plan (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return NoContent();
    }
}
