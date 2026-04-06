using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Subscriptions.ExpiryCleanup;

/// <summary>
/// Service dọn dẹp các subscription hết hạn và payment treo
/// </summary>
public class SubscriptionExpiryService
{
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<SubscriptionExpiryService> _logger;

    public SubscriptionExpiryService(
        IUserSubscriptionRepository subscriptionRepository,
        IPaymentRepository paymentRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILogger<SubscriptionExpiryService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _paymentRepository = paymentRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task ProcessExpiriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu xử lý dọn dẹp Subscription và Payment.");

        await ProcessExpiredSubscriptionsAsync(cancellationToken);
        await ProcessStalePendingPaymentsAsync(cancellationToken);

        _logger.LogInformation("Hoàn tất xử lý dọn dẹp Subscription và Payment.");
    }

    private async Task ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var (allSubs, _) = await _subscriptionRepository.GetAllWithUsersAsync(1, 1000, "Active", null, cancellationToken);
        var expiredSubs = allSubs.Where(s => s.EndDate.HasValue && s.EndDate.Value <= DateTime.UtcNow).ToList();

        if (!expiredSubs.Any()) return;

        _logger.LogInformation($"Tìm thấy {expiredSubs.Count} subscriptions đã hết hạn.");

        foreach (var sub in expiredSubs)
        {
            sub.Status = SubscriptionStatus.Expired;
            sub.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(sub, cancellationToken);

            // Thu hồi quyền Premium (nếu người dùng không còn gói Active nào khác)
            var hasOtherActive = await _subscriptionRepository.HasActiveSubscriptionAsync(sub.UserId, cancellationToken);
            if (!hasOtherActive)
            {
                await RevokePremiumRoleAsync(sub.UserId, cancellationToken);
            }
        }
    }

    private async Task RevokePremiumRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null) return;

        var premiumRole = await _roleRepository.GetByNameAsync("Premium", cancellationToken);
        if (premiumRole is null) return;

        var activeRoles = user.UserRoles.Where(ur => ur.RoleId == premiumRole.Id).ToList();
        bool changed = false;

        foreach (var userRole in activeRoles)
        {
            // Nếu chưa hết hạn thì force hết hạn
            if (userRole.ExpiresAt == null || userRole.ExpiresAt > DateTime.UtcNow)
            {
                userRole.ExpiresAt = DateTime.UtcNow;
                changed = true;
            }
        }

        if (changed)
        {
            await _userRepository.UpdateAsync(user, cancellationToken);
            _logger.LogInformation($"Đã thu hồi role Premium của user {userId}.");
        }
    }

    private async Task ProcessStalePendingPaymentsAsync(CancellationToken cancellationToken)
    {
        var threshold = DateTime.UtcNow.AddHours(-24);
        var stalePayments = await _paymentRepository.GetPendingPaymentsOlderThanAsync(threshold, cancellationToken);

        if (!stalePayments.Any()) return;

        _logger.LogInformation($"Tìm thấy {stalePayments.Count()} payments treo quá 24h.");

        foreach (var payment in stalePayments)
        {
            payment.Status = PaymentStatus.Failed;
            payment.GatewayResponse = "Tự động hủy do quá 24h không thanh toán.";
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            if (payment.SubscriptionId.HasValue)
            {
                var sub = await _subscriptionRepository.GetByIdAsync(payment.SubscriptionId.Value, cancellationToken);
                if (sub != null && sub.Status == SubscriptionStatus.PendingPayment)
                {
                    sub.Status = SubscriptionStatus.Cancelled;
                    sub.UpdatedAt = DateTime.UtcNow;
                    await _subscriptionRepository.UpdateAsync(sub, cancellationToken);
                }
            }
        }
    }
}
