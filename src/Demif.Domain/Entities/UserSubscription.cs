using Demif.Domain.Common;
using Demif.Domain.Enums;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity UserSubscription - đăng ký của user
/// </summary>
public class UserSubscription : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Ngày hết hạn. Null = vĩnh viễn (lifetime)
    /// </summary>
    public DateTime? EndDate { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.PendingPayment;
    public bool AutoRenew { get; set; }

    // Navigation
    public virtual User? User { get; set; }
    public virtual SubscriptionPlan? Plan { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
