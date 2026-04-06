using Demif.Domain.Enums;

namespace Demif.Application.Features.Admin.Payments;

// ─── Responses ────────────────────────────────────────────────────────────────

public record AdminPaymentListItemResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    string? PlanName,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    PaymentStatus Status,
    string PaymentReference,
    string? TransactionId,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record AdminPaymentDetailResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    Guid? PlanId,
    string? PlanName,
    Guid? SubscriptionId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string PaymentReference,
    PaymentStatus Status,
    string? TransactionId,
    string? BankCode,
    string? BankTransactionNo,
    string? GatewayResponse,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? Note
);

public record AdminPaymentPagedResponse(
    IEnumerable<AdminPaymentListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

// ─── Requests ─────────────────────────────────────────────────────────────────

public record RefundPaymentRequest(
    string Reason
);
