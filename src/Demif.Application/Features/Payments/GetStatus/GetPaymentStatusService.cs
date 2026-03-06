using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Payments.GetStatus;

/// <summary>
/// Polling endpoint: kiểm tra trạng thái thanh toán theo referenceCode.
/// FE gọi mỗi 3-5 giây để biết thanh toán đã xác nhận chưa.
/// </summary>
public class GetPaymentStatusService
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentStatusService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<GetPaymentStatusResponse>> ExecuteAsync(
        string referenceCode,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByReferenceAsync(referenceCode, cancellationToken);

        if (payment is null)
            return Result.Failure<GetPaymentStatusResponse>(Error.NotFound("Không tìm thấy đơn thanh toán."));

        return Result.Success(new GetPaymentStatusResponse
        {
            ReferenceCode = payment.PaymentReference,
            Status        = payment.Status.ToString(),
            IsCompleted   = payment.Status == PaymentStatus.Completed,
            IsFailed      = payment.Status == PaymentStatus.Failed,
            CompletedAt   = payment.CompletedAt,
            TransactionId = payment.TransactionId,
            BankCode      = payment.BankCode,
        });
    }
}

public class GetPaymentStatusResponse
{
    public string    ReferenceCode { get; set; } = string.Empty;
    public string    Status        { get; set; } = string.Empty;
    public bool      IsCompleted   { get; set; }
    public bool      IsFailed      { get; set; }
    public DateTime? CompletedAt   { get; set; }
    public string?   TransactionId { get; set; }
    public string?   BankCode      { get; set; }
}
