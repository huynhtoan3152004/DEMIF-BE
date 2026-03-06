using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Payments.GetHistory;

/// <summary>
/// Lịch sử tất cả giao dịch của user (kể cả Pending, Failed, Completed).
/// </summary>
public class GetPaymentHistoryService
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentHistoryService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<GetPaymentHistoryResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetByUserIdAsync(userId, cancellationToken);

        var items = payments
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentHistoryItem
            {
                Id              = p.Id,
                ReferenceCode   = p.PaymentReference,
                Amount          = p.Amount,
                Currency        = p.Currency,
                PaymentMethod   = p.PaymentMethod,
                Status          = p.Status.ToString(),
                TransactionId   = p.TransactionId,
                BankCode        = p.BankCode,
                CreatedAt       = p.CreatedAt,
                CompletedAt     = p.CompletedAt,
            })
            .ToList();

        return Result.Success(new GetPaymentHistoryResponse { Items = items });
    }
}

public class GetPaymentHistoryResponse
{
    public List<PaymentHistoryItem> Items { get; set; } = new();
}

public class PaymentHistoryItem
{
    public Guid      Id            { get; set; }
    public string    ReferenceCode { get; set; } = string.Empty;
    public decimal   Amount        { get; set; }
    public string    Currency      { get; set; } = "VND";
    public string    PaymentMethod { get; set; } = string.Empty;
    public string    Status        { get; set; } = string.Empty;
    public string?   TransactionId { get; set; }
    public string?   BankCode      { get; set; }
    public DateTime  CreatedAt     { get; set; }
    public DateTime? CompletedAt   { get; set; }
}
