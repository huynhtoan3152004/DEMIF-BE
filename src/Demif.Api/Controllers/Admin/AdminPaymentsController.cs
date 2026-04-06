using Demif.Application.Features.Admin.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

[Route("api/admin/payments")]
[ApiController]
[Authorize(Policy = "RequireAdmin")]
public class AdminPaymentsController : ControllerBase
{
    private readonly AdminPaymentService _paymentService;

    public AdminPaymentsController(AdminPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Lấy danh sách giao dịch (có phân trang và filter)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _paymentService.GetAllAsync(page, pageSize, status, search, dateFrom, dateTo, cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Lấy chi tiết một giao dịch
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure) return NotFound(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Đánh dấu là đã hoàn tiền (Manual refund)
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentService.RefundAsync(id, request, cancellationToken);
        if (result.IsFailure) return BadRequest(new { error = result.Error.Message });

        return Ok(new { message = result.Value });
    }
}
