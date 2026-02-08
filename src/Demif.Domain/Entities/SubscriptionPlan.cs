using Demif.Domain.Common;
using Demif.Domain.Enums;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity SubscriptionPlan - gói đăng ký Premium
/// </summary>
public class SubscriptionPlan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public BillingCycle BillingCycle { get; set; }

    /// <summary>
    /// Thời hạn gói (ngày). Null = vĩnh viễn
    /// </summary>
    public int? DurationDays { get; set; }

    /// <summary>
    /// JSON array các tính năng: ["Không giới hạn bài học", "AI feedback"]
    /// </summary>
    public string? Features { get; set; }

    /// <summary>
    /// JSON object giới hạn: {"lessonsPerDay": -1, "aiRequests": -1}
    /// </summary>
    public string? Limits { get; set; }

    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
