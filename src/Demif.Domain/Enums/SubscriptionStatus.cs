namespace Demif.Domain.Enums;

/// <summary>
/// Trạng thái subscription của user
/// </summary>
public enum SubscriptionStatus
{
    Active = 1,
    Expired = 2,
    Cancelled = 3,
    PendingPayment = 4
}
