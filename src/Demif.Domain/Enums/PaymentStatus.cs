namespace Demif.Domain.Enums;

/// <summary>
/// Trạng thái thanh toán
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}
