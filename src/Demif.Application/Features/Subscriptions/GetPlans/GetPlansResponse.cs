namespace Demif.Application.Features.Subscriptions.GetPlans;

/// <summary>
/// Response DTO cho subscription plan
/// </summary>
public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public string BillingCycle { get; set; } = string.Empty;
    public int? DurationDays { get; set; }
    public List<string> Features { get; set; } = new();
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
}

/// <summary>
/// Response cho GetPlans
/// </summary>
public class GetPlansResponse
{
    public List<SubscriptionPlanDto> Plans { get; set; } = new();
}
