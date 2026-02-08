using Demif.Application.Features.Payments.Webhook;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// API Controller cho Payments và Webhooks
/// </summary>
[Route("api/payments")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly SePayWebhookService _sePayWebhookService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        SePayWebhookService sePayWebhookService,
        ILogger<PaymentsController> logger)
    {
        _sePayWebhookService = sePayWebhookService;
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
        _logger.LogInformation("Received SEPay webhook: {Reference}", request.ReferenceCode);

        var result = await _sePayWebhookService.HandleWebhookAsync(request, cancellationToken);

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
}
