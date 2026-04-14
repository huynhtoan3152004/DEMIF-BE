using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Payments.Webhook;

/// <summary>
/// SEPay Webhook Service - xử lý callback khi thanh toán thành công
/// Flow: Verify signature → Update Payment → Activate Subscription → Assign Premium role
/// </summary>
public class SePayWebhookService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SePayWebhookService> _logger;

    public SePayWebhookService(
        IPaymentRepository paymentRepository,
        IUserSubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<SePayWebhookService> logger)
    {
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

        public async Task<Result<SePayWebhookResponse>> HandleWebhookAsync(
        SePayWebhookRequest request,
        string? authorizationHeader,
        string? apiKeyQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received SEPay webhook for transaction: {TransactionId}", request.Id);

        // 1. Xác thực API Key (theo format của SePay: `Apikey YOUR_API_KEY`)
        var configuredApiKey = _configuration["SEPay:SecretKey"]?.Trim();
        if (!string.IsNullOrEmpty(configuredApiKey))
        {
            var expectedAuthHeader = $"Apikey {configuredApiKey}";
            bool isHeaderValid = !string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.Trim().Equals(expectedAuthHeader, StringComparison.OrdinalIgnoreCase);
            bool isQueryValid = !string.IsNullOrEmpty(apiKeyQuery) && apiKeyQuery.Trim().Equals(configuredApiKey, StringComparison.Ordinal);

            if (!isHeaderValid && !isQueryValid)
            {
                _logger.LogWarning("Unauthorized webhook access. Header got: '{HeaderGot}', Query got: '{QueryGot}', Expected: '{Expected}'", authorizationHeader, apiKeyQuery, configuredApiKey);
                return Result.Failure<SePayWebhookResponse>(Error.Validation("Unauthorized"));
            }
        }

        // 2. Tìm payment theo reference code. SePay gởi ở trường `code` nếu nhận diện được.
        // Hoặc tự trích xuất từ `content` nếu `code` null.
        var paymentCode = request.Code;
        if (string.IsNullOrEmpty(paymentCode) && !string.IsNullOrEmpty(request.Content))
        {
            // Fallback: Tìm mã dạng DEMIF... trong Content
            var match = System.Text.RegularExpressions.Regex.Match(request.Content, @"(?i)(DEMIF[A-Z0-9]+)");
            if (match.Success)
            {
                paymentCode = match.Value.ToUpper();
            }
        }

        if (string.IsNullOrEmpty(paymentCode))
        {
            return Result.Failure<SePayWebhookResponse>(Error.Validation("Missing payment code in webhook"));
        }

        var payment = await _paymentRepository.GetByReferenceAsync(paymentCode, cancellationToken);
        if (payment is null)
        {
            _logger.LogWarning("Payment not found for code: {Code}", paymentCode);
            // Vẫn trả về success để SePay không spam (nhưng ko có kết quả)
            return Result.Success(new SePayWebhookResponse { Success = true, Message = "Payment not found but ack" });
        }

        // 3. Kiểm tra payment đã xử lý chưa
        if (payment.Status == PaymentStatus.Completed)
        {
            _logger.LogInformation("Payment already processed: {Code}", paymentCode);
            return Result.Success(new SePayWebhookResponse { Success = true, Message = "Already processed" });
        }

        // 4. Kiểm tra loại giao dịch và số tiền
        if (request.TransferType != "in")
        {
            return Result.Failure<SePayWebhookResponse>(Error.Validation("Not an incoming transfer"));
        }

        if (payment.Amount != request.TransferAmount)
        {
            _logger.LogWarning("Amount mismatch for {Code}: expected {Expected}, got {Got}",
                paymentCode, payment.Amount, request.TransferAmount);
            return Result.Failure<SePayWebhookResponse>(Error.Validation("Amount mismatch"));
        }

        // 5. Nếu tới đây thì giao dịch hợp lệ
        return await ProcessSuccessPaymentAsync(payment, request, cancellationToken);
    }

    private async Task<Result<SePayWebhookResponse>> ProcessSuccessPaymentAsync(
        Payment payment,
        SePayWebhookRequest request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var paymentCompletedNow = await EnsurePaymentCompletedAsync(payment, request, cancellationToken);
                var roleExpiresAt = await EnsureSubscriptionActivatedAsync(payment, cancellationToken);
                await EnsurePremiumRoleAsync(payment.UserId, roleExpiresAt, cancellationToken);

                _logger.LogInformation(
                    paymentCompletedNow
                        ? "Payment processed successfully: {Code}"
                        : "Payment already completed, ensured related subscription/role state: {Code}",
                    request.Code ?? request.Content);

                return Result.Success(new SePayWebhookResponse
                {
                    Success = true,
                    Message = paymentCompletedNow ? "Payment processed" : "Webhook already processed"
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict while processing SEPay webhook for payment {PaymentReference} (attempt {Attempt}). Retrying with fresh state.",
                    payment.PaymentReference,
                    attempt);

                if (_dbContext is DbContext efDbContext)
                {
                    efDbContext.ChangeTracker.Clear();
                }

                var freshPayment = await _paymentRepository.GetByReferenceAsync(payment.PaymentReference, cancellationToken);
                if (freshPayment is null)
                {
                    return Result.Success(new SePayWebhookResponse
                    {
                        Success = true,
                        Message = "Payment not found but ack"
                    });
                }

                payment = freshPayment;
            }
        }

        return Result.Success(new SePayWebhookResponse
        {
            Success = true,
            Message = "Webhook already processed"
        });
    }

    private async Task<bool> EnsurePaymentCompletedAsync(
        Payment payment,
        SePayWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var shouldUpdate = payment.Status != PaymentStatus.Completed
            || payment.TransactionId != request.Id.ToString()
            || payment.BankCode != request.Gateway
            || payment.BankTransactionNo != request.ReferenceCode
            || payment.CompletedAt is null;

        if (!shouldUpdate)
        {
            return false;
        }

        payment.Status = PaymentStatus.Completed;
        payment.TransactionId = request.Id.ToString();
        payment.BankCode = request.Gateway;
        payment.BankTransactionNo = request.ReferenceCode;
        payment.GatewayResponse = System.Text.Json.JsonSerializer.Serialize(request);
        payment.CompletedAt = now;

        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        return true;
    }

    private async Task<DateTime?> EnsureSubscriptionActivatedAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (!payment.SubscriptionId.HasValue)
            return null;

        var subscription = await _subscriptionRepository.GetByIdWithPlanAsync(payment.SubscriptionId.Value, cancellationToken);
        if (subscription is null)
            return null;

        var now = DateTime.UtcNow;
        DateTime? targetEndDate = subscription.Plan?.BillingCycle.GetDurationDays().HasValue == true
            ? now.AddDays(subscription.Plan.BillingCycle.GetDurationDays()!.Value)
            : null;

        var shouldUpdate = subscription.Status != SubscriptionStatus.Active
            || subscription.StartDate == default
            || subscription.EndDate != targetEndDate
            || subscription.UpdatedAt is null;

        if (!shouldUpdate)
            return subscription.EndDate;

        subscription.Status = SubscriptionStatus.Active;
        subscription.StartDate = now;
        subscription.EndDate = targetEndDate;
        subscription.UpdatedAt = now;

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        return subscription.EndDate;
    }

    private async Task EnsurePremiumRoleAsync(Guid userId, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null) return;

        var premiumRole = await _roleRepository.GetByNameAsync("Premium", cancellationToken);
        if (premiumRole is null) return;

        // Check if role exists and still active
        var existingRole = user.UserRoles.FirstOrDefault(ur =>
            ur.RoleId == premiumRole.Id &&
            (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow));

        if (existingRole == null)
        {
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = premiumRole.Id,
                AssignedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            user.UserRoles.Add(userRole);
        }
        else
        {
            // Update expiration if role already exists
            existingRole.ExpiresAt = expiresAt;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    // Gỡ bỏ Verify Signature vì ta đổi qua dùng chứng thực API Key headers
}
