using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Payments.CancelPayment;

/// <summary>
/// Service hủy bỏ đơn thanh toán và subscription đang bị nghẽn (PendingPayment)
/// </summary>
public class CancelPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IApplicationDbContext _dbContext;

    public CancelPaymentService(
        IPaymentRepository paymentRepository,
        IUserSubscriptionRepository subscriptionRepository,
        IApplicationDbContext dbContext)
    {
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<string>> ExecuteAsync(
        Guid userId,
        string referenceCode,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByReferenceAsync(referenceCode, cancellationToken);

        if (payment is null)
            return Result.Failure<string>(Error.NotFound("Không tìm thấy đơn thanh toán."));

        if (payment.UserId != userId)
            return Result.Failure<string>(Error.Validation("Bạn không có quyền thao tác trên đơn thanh toán này."));

        if (payment.Status != PaymentStatus.Pending)
            return Result.Failure<string>(Error.Conflict("Chỉ có thể hủy đơn thanh toán đang chờ xử lý."));

        // Hủy Payment
        payment.Status = PaymentStatus.Failed; // Failed hoặc Cancelled, hệ thống dùng Failed cho giao dịch không thành công
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        // Hủy Subscription liên đới
        if (payment.SubscriptionId.HasValue)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(payment.SubscriptionId.Value, cancellationToken);
            if (subscription is not null && subscription.Status == SubscriptionStatus.PendingPayment)
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success("Đã hủy đơn thanh toán thành công.");
    }
}
