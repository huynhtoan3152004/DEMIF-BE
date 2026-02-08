using Demif.Domain.Enums;

namespace Demif.Application.Features.Subscriptions.Admin;

/// <summary>
/// Request tạo/sửa subscription plan
/// </summary>
public class CreateUpdatePlanRequest
{
    public string Name { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Premium;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public BillingCycle BillingCycle { get; set; }
    public int? DurationDays { get; set; }
    public List<string>? Features { get; set; }
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Response cho plan admin với thống kê
/// </summary>
public class PlanAdminDto
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
    public bool IsActive { get; set; }
    public int TotalSubscribers { get; set; }
    public int ActiveSubscribers { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response cho thống kê tổng quan
/// </summary>
public class SubscriptionStatsResponse
{
    public int TotalPlans { get; set; }
    public int TotalSubscribers { get; set; }
    public int ActiveSubscribers { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<PlanAdminDto> Plans { get; set; } = new();
}
