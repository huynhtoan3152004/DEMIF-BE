namespace Demif.Application.Features.Subscriptions.Subscribe;

/// <summary>
/// Request cho đăng ký subscription
/// </summary>
public class SubscribeRequest
{
    /// <summary>
    /// ID của gói muốn đăng ký
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Phương thức thanh toán: sepay_bank, momo, zalopay
    /// </summary>
    public string PaymentMethod { get; set; } = "sepay_bank";

    /// <summary>
    /// Tự động gia hạn
    /// </summary>
    public bool AutoRenew { get; set; }
}
