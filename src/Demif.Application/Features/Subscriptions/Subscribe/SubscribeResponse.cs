namespace Demif.Application.Features.Subscriptions.Subscribe;

/// <summary>
/// Response cho đăng ký subscription
/// </summary>
public class SubscribeResponse
{
    /// <summary>
    /// ID subscription được tạo
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// ID payment được tạo
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Mã tham chiếu thanh toán (dùng cho SEPay webhook matching)
    /// </summary>
    public string PaymentReference { get; set; } = string.Empty;

    /// <summary>
    /// Số tiền cần thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Đơn vị tiền tệ
    /// </summary>
    public string Currency { get; set; } = "VND";

    /// <summary>
    /// Tên gói đăng ký
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái: PendingPayment
    /// </summary>
    public string Status { get; set; } = "PendingPayment";
}
