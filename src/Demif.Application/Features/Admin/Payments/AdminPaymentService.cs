using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Admin.Payments;

/// <summary>
/// Admin service cho việc quản lý Payments (Xem danh sách, chi tiết, refund)
/// </summary>
public class AdminPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public AdminPaymentService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<AdminPaymentPagedResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? status,
        string? search,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, total) = await _paymentRepository.GetAllWithDetailsAsync(
            page, pageSize, status, search, dateFrom, dateTo, ct);

        var responses = items.Select(p => new AdminPaymentListItemResponse(
            Id: p.Id,
            UserId: p.UserId,
            UserEmail: p.User?.Email ?? string.Empty,
            UserName: p.User?.Username ?? string.Empty,
            PlanName: p.Plan?.Name ?? p.Subscription?.Plan?.Name,
            Amount: p.Amount,
            Currency: p.Currency,
            PaymentMethod: p.PaymentMethod,
            Status: p.Status,
            PaymentReference: p.PaymentReference,
            TransactionId: p.TransactionId,
            CreatedAt: p.CreatedAt,
            CompletedAt: p.CompletedAt
        ));

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return Result<AdminPaymentPagedResponse>.Success(
            new AdminPaymentPagedResponse(responses, total, page, pageSize, totalPages));
    }

    public async Task<Result<AdminPaymentDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(id, ct);
        if (payment is null)
            return Result.Failure<AdminPaymentDetailResponse>(new Error("Payment.NotFound", "Không tìm thấy thanh toán này."));

        // Load details via GetAllWithDetails directly for simplicity, or we can just return what we have mapped.
        // But GetByIdAsync from repository doesn't have User info included by default. 
        // We'll use the same trick if needed, but wait: the payment object here doesn't have navigation properties loaded.
        
        // Let's use GetAllWithDetailsAsync to fetch just 1 record.
        var (items, _) = await _paymentRepository.GetAllWithDetailsAsync(
            1, 1, null, payment.PaymentReference, null, null, ct);
            
        var p = items.FirstOrDefault();
        if (p == null) return Result.Failure<AdminPaymentDetailResponse>(new Error("Payment.NotFound", "Không thể tải chi tiết thanh toán."));

        return Result<AdminPaymentDetailResponse>.Success(new AdminPaymentDetailResponse(
            Id: p.Id,
            UserId: p.UserId,
            UserEmail: p.User?.Email ?? string.Empty,
            UserName: p.User?.Username ?? string.Empty,
            PlanId: p.PlanId,
            PlanName: p.Plan?.Name ?? p.Subscription?.Plan?.Name,
            SubscriptionId: p.SubscriptionId,
            Amount: p.Amount,
            Currency: p.Currency,
            PaymentMethod: p.PaymentMethod,
            PaymentReference: p.PaymentReference,
            Status: p.Status,
            TransactionId: p.TransactionId,
            BankCode: p.BankCode,
            BankTransactionNo: p.BankTransactionNo,
            GatewayResponse: p.GatewayResponse,
            CreatedAt: p.CreatedAt,
            CompletedAt: p.CompletedAt,
            Note: "N/A"
        ));
    }

    public async Task<Result<string>> RefundAsync(Guid id, RefundPaymentRequest request, CancellationToken ct = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(id, ct);
        if (payment is null)
            return Result.Failure<string>(new Error("Payment.NotFound", "Không tìm thấy thanh toán này."));

        if (payment.Status != PaymentStatus.Completed)
            return Result.Failure<string>(new Error("Payment.NotCompleted", "Chỉ có thể hoàn tiền cho giao dịch đã hoàn tất."));

        // Manual refund in SEPay/Banking system is required first.
        // This just marks it as refunded in our system.
        payment.Status = PaymentStatus.Refunded;
        // Optionally store the reason in GatewayResponse or a new Note field.
        
        await _paymentRepository.UpdateAsync(payment, ct);

        return Result<string>.Success($"Đã đổi trạng thái giao dịch sang Hoàn Tiền. Lý do: {request.Reason}");
    }
}
