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

        // 3. Kiểm tra user đã có subscription active chưa
        var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken);
        if (existingSubscription is not null)
        {
            return Result.Failure<SubscribeResponse>(Error.Conflict("Bạn đã có gói đăng ký đang hoạt động."));
        }

        // 4. Tạo payment reference duy nhất
        var paymentReference = GeneratePaymentReference(userId);

        // 5. Tạo subscription (pending)
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = plan.DurationDays.HasValue 
                ? DateTime.UtcNow.AddDays(plan.DurationDays.Value) 
                : null, // null = lifetime
            Status = SubscriptionStatus.PendingPayment,
            AutoRenew = request.AutoRenew
        };

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);

        // 6. Tạo payment record
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
