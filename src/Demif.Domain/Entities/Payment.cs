using Demif.Domain.Common;
using Demif.Domain.Enums;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity Payment - thanh toán subscription
/// </summary>
public class Payment : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public Guid? SubscriptionId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";

    /// <summary>
    /// Phương thức thanh toán: sepay_bank, momo, zalopay
    /// </summary>
    public string PaymentMethod { get; set; } = "sepay_bank";

    /// <summary>
    /// Transaction ID từ SEPay/Momo/ZaloPay
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Mã ngân hàng: VCB, TCB, MB, ACB...
    /// </summary>
    public string? BankCode { get; set; }

    /// <summary>
    /// Số giao dịch ngân hàng
    /// </summary>
    public string? BankTransactionNo { get; set; }

    /// <summary>
    /// Mã tham chiếu duy nhất để match với webhook
    /// </summary>
    public string PaymentReference { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// JSON response từ payment gateway
    /// </summary>
    public string? GatewayResponse { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }

    // Navigation
    public virtual User? User { get; set; }
    public virtual SubscriptionPlan? Plan { get; set; }
    public virtual UserSubscription? Subscription { get; set; }
}
