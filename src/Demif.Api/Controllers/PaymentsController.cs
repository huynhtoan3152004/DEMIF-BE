using Demif.Application.Features.Payments.GetHistory;
using Demif.Application.Features.Payments.GetInfo;
using Demif.Application.Features.Payments.GetStatus;
using Demif.Application.Features.Payments.Webhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Payments — Webhook callbacks + payment info/status for SEPay
/// </summary>
[Route("api/payments")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly SePayWebhookService _sePayWebhookService;
    private readonly GetPaymentInfoService _getPaymentInfoService;
    private readonly GetPaymentStatusService _getPaymentStatusService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        SePayWebhookService sePayWebhookService,
        GetPaymentInfoService getPaymentInfoService,
        GetPaymentStatusService getPaymentStatusService,
        ILogger<PaymentsController> logger)
    {
        _sePayWebhookService = sePayWebhookService;
        _getPaymentInfoService = getPaymentInfoService;
        _getPaymentStatusService = getPaymentStatusService;
        _logger = logger;
    }

    /// <summary>
    /// SEPay payment webhook callback (public endpoint, verified by signature)
    /// </summary>
    [HttpPost("sepay/webhook")]
    public async Task<IActionResult> SePayWebhook(
        [FromBody] SePayWebhookRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received SEPay webhook: {Reference}", request.Code ?? request.Content);

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var result = await _sePayWebhookService.HandleWebhookAsync(request, authHeader, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("SEPay webhook failed: {Error}", result.Error.Message);
            
            // Trả về 200 để SEPay không retry (tránh spam)
            // Nhưng với message báo lỗi
            return Ok(new SePayWebhookResponse
            {
                Success = false,
                Message = result.Error.Message
            });
        }

        return Ok(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Payment Info & Status (requires auth)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// FE gọi thủ công để hủy 1 giao dịch đang chờ thanh toán (PendingPayment).
    /// </summary>
    [HttpPost("{referenceCode}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelPayment(
        string referenceCode,
        [FromServices] Demif.Application.Features.Payments.CancelPayment.CancelPaymentService cancelPaymentService,
        [FromServices] Demif.Application.Abstractions.Services.ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
            return Unauthorized();

        var result = await cancelPaymentService.ExecuteAsync(currentUserService.UserId.Value, referenceCode, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(new { success = true, message = result.Value });
    }    

    /// <summary>
    /// Lấy thông tin thanh toán SEPay: số TK, QR, nội dung CK.
    /// FE dùng để hiển thị màn hình "Chờ thanh toán".
    /// </summary>
    [HttpGet("info/{referenceCode}")]
    [Authorize]
    [ProducesResponseType(typeof(GetPaymentInfoResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPaymentInfo(
        string referenceCode,
        CancellationToken cancellationToken)
    {
        var result = await _getPaymentInfoService.ExecuteAsync(referenceCode, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Polling: kiểm tra trạng thái thanh toán.
    /// FE gọi mỗi 3-5 giây cho đến khi IsCompleted = true.
    /// </summary>
    [HttpGet("{referenceCode}/status")]
    [Authorize]
    [ProducesResponseType(typeof(GetPaymentStatusResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPaymentStatus(
        string referenceCode,
        CancellationToken cancellationToken)
    {
        var result = await _getPaymentStatusService.ExecuteAsync(referenceCode, cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Message });

        return Ok(result.Value);
    }
}
