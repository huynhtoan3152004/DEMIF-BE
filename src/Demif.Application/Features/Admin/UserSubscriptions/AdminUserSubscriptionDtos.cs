using Demif.Domain.Enums;

namespace Demif.Application.Features.Admin.UserSubscriptions;

// ─── Responses ────────────────────────────────────────────────────────────────

public record AdminUserSubscriptionListItemResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    Guid PlanId,
    string PlanName,
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime? EndDate,
    bool AutoRenew,
    DateTime CreatedAt
);

public record AdminUserSubscriptionDetailResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    Guid PlanId,
    string PlanName,
    SubscriptionTier Tier,
    decimal PlanPrice,
    string Currency,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime? EndDate,
    bool AutoRenew,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<AdminPaymentSummaryResponse> Payments
);

public record AdminPaymentSummaryResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    PaymentStatus Status,
    string? TransactionId,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record AdminUserSubscriptionPagedResponse(
    IEnumerable<AdminUserSubscriptionListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

// ─── Requests ─────────────────────────────────────────────────────────────────

public record ExtendSubscriptionRequest(
    int Days,
    string? Note
);

public record CancelSubscriptionRequest(
    string? Reason
);
