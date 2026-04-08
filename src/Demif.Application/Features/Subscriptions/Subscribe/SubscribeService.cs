using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Subscriptions.Subscribe;

/// <summary>
/// Subscribe Service - đăng ký gói Premium
/// </summary>
public class SubscribeService
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _dbContext;

    public SubscribeService(
        ISubscriptionPlanRepository planRepository,
        IUserSubscriptionRepository subscriptionRepository,
        IPaymentRepository paymentRepository,
        IUserRepository userRepository,
        IApplicationDbContext dbContext)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _paymentRepository = paymentRepository;
        _userRepository = userRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<SubscribeResponse>> ExecuteAsync(
        Guid userId,
        SubscribeRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra user tồn tại
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<SubscribeResponse>(Error.NotFound("Không tìm thấy người dùng."));
        }

        // 2. Kiểm tra plan tồn tại và active
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
        {
            return Result.Failure<SubscribeResponse>(Error.NotFound("Gói đăng ký không tồn tại hoặc đã ngừng hoạt động."));
        }

        if (plan.Tier != SubscriptionTier.Premium)
        {
            return Result.Failure<SubscribeResponse>(Error.Validation("Chỉ các gói Premium mới có thể được đăng ký."));
        }

        if (!plan.BillingCycle.IsSupportedPremiumCycle())
        {
            return Result.Failure<SubscribeResponse>(Error.Validation("Chu kỳ gói không hợp lệ."));
        }

        // 3. Kiểm tra lịch sử và trạng thái subscription hiện có của user
        var existingSubscriptions = await _subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);

        if (plan.BillingCycle == BillingCycle.Weekly)
        {
            var hasCompletedTrial = existingSubscriptions.Any(s =>
                s.PlanId == plan.Id &&
                (
                    s.Status == SubscriptionStatus.Expired ||
                    (s.Status == SubscriptionStatus.Active && s.EndDate.HasValue && s.EndDate.Value <= DateTime.UtcNow)
                ));

            if (hasCompletedTrial)
            {
                return Result.Failure<SubscribeResponse>(Error.Conflict("Gói Premium 7 ngày chỉ được đăng ký lại sau khi chu kỳ dùng thử trước đó kết thúc."));
            }
        }

        // 4. Kiểm tra user đã có subscription active hoặc pending chưa
        var activeSubscription = existingSubscriptions.FirstOrDefault(s => s.Status == SubscriptionStatus.Active && (s.EndDate == null || s.EndDate > DateTime.UtcNow));
        if (activeSubscription is not null)
        {
            return Result.Failure<SubscribeResponse>(Error.Conflict("Bạn đã có gói đăng ký đang hoạt động."));
        }

        var pendingSubscriptions = existingSubscriptions.Where(s => s.Status == SubscriptionStatus.PendingPayment).ToList();
        foreach (var pending in pendingSubscriptions)
        {
            if (pending.CreatedAt < DateTime.UtcNow.AddHours(-24))
            {
                // Stale pending -> cancel it
                pending.Status = SubscriptionStatus.Cancelled;
                await _subscriptionRepository.UpdateAsync(pending, cancellationToken);
                
                // We should also find and cancel the associated payment
                var pendingPayment = await _paymentRepository.GetBySubscriptionIdAsync(pending.Id, cancellationToken);
                if (pendingPayment is not null && pendingPayment.Status == PaymentStatus.Pending)
                {
                    pendingPayment.Status = PaymentStatus.Failed;
                    await _paymentRepository.UpdateAsync(pendingPayment, cancellationToken);
                }
            }
            else
            {
                // Still valid pending
                return Result.Failure<SubscribeResponse>(Error.Conflict("Bạn đang có một giao dịch chờ thanh toán. Vui lòng hoàn tất hoặc chờ 24h để thanh toán bị hủy tự động."));
            }
        }

        // 5. Tạo payment reference duy nhất
        var paymentReference = GeneratePaymentReference(userId);

        // 6. Tạo subscription (pending)
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow, // Will be updated to actual payment time upon successful webhook
            EndDate = plan.BillingCycle.GetDurationDays().HasValue 
                ? DateTime.UtcNow.AddDays(plan.BillingCycle.GetDurationDays()!.Value) 
                : null, // Will be recalculated upon successful webhook
            Status = SubscriptionStatus.PendingPayment,
            AutoRenew = request.AutoRenew
        };

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);

        // 7. Tạo payment record
        var payment = new Payment
        {
            UserId = userId,
            PlanId = plan.Id,
            SubscriptionId = subscription.Id,
            Amount = plan.Price,
            Currency = plan.Currency,
            PaymentMethod = request.PaymentMethod,
            PaymentReference = paymentReference,
            Status = PaymentStatus.Pending
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new SubscribeResponse
        {
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            PaymentReference = paymentReference,
            Amount = plan.Price,
            Currency = plan.Currency,
            PlanName = plan.Name,
            Status = "PendingPayment"
        });
    }

    /// <summary>
    /// Tạo mã tham chiếu thanh toán duy nhất
    /// Format: DEMIF{UserId first 4 chars}{timestamp}{random}
    /// </summary>
    private static string GeneratePaymentReference(Guid userId)
    {
        var userPart = userId.ToString("N")[..4].ToUpper();
        var timePart = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()[^6..];
        var randomPart = Guid.NewGuid().ToString("N")[..4].ToUpper();
        return $"DEMIF{userPart}{timePart}{randomPart}";
    }
}
