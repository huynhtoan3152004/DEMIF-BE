namespace Demif.Application.Features.Subscriptions.GetMySubscription;

/// <summary>
/// Response cho subscription hiện tại của user
/// </summary>
public class GetMySubscriptionResponse
{
    public bool HasActiveSubscription { get; set; }
    public SubscriptionDto? Subscription { get; set; }
}

/// <summary>
/// DTO cho subscription
/// </summary>
public class SubscriptionDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool AutoRenew { get; set; }
    public int? DaysRemaining { get; set; }
}
