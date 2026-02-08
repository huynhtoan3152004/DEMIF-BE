namespace Demif.Application.Features.Payments.Webhook;

/// <summary>
/// SEPay Webhook request payload
/// Theo format của SEPay gateway
/// </summary>
public class SePayWebhookRequest
{
    /// <summary>
    /// Mã giao dịch SEPay
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Mã tham chiếu (PaymentReference đã gửi)
    /// </summary>
    public string? ReferenceCode { get; set; }

    /// <summary>
    /// Số tiền giao dịch
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Mã ngân hàng
    /// </summary>
    public string? BankCode { get; set; }

    /// <summary>
    /// Số giao dịch ngân hàng
    /// </summary>
    public string? BankTransactionNo { get; set; }

    /// <summary>
    /// Nội dung chuyển khoản
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Thời gian giao dịch
    /// </summary>
    public DateTime? TransactionTime { get; set; }

    /// <summary>
    /// Trạng thái: success, failed
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Signature để verify webhook
    /// </summary>
    public string? Signature { get; set; }
}

/// <summary>
/// Response cho webhook
/// </summary>
public class SePayWebhookResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
