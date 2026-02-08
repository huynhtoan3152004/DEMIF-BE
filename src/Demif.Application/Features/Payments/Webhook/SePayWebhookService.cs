using System.Security.Cryptography;
using System.Text;
using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received SEPay webhook for reference: {Reference}", request.ReferenceCode);

        // 1. Verify signature (optional - nếu SEPay gửi)
        if (!string.IsNullOrEmpty(request.Signature))
        {
            var isValid = VerifySignature(request);
            if (!isValid)
            {
                _logger.LogWarning("Invalid signature for webhook: {Reference}", request.ReferenceCode);
                return Result.Failure<SePayWebhookResponse>(Error.Validation("Invalid signature"));
            }
        }

        // 2. Tìm payment theo reference
        if (string.IsNullOrEmpty(request.ReferenceCode))
        {
            return Result.Failure<SePayWebhookResponse>(Error.Validation("Missing reference code"));
        }

        var payment = await _paymentRepository.GetByReferenceAsync(request.ReferenceCode, cancellationToken);
        if (payment is null)
        {
            _logger.LogWarning("Payment not found for reference: {Reference}", request.ReferenceCode);
            return Result.Failure<SePayWebhookResponse>(Error.NotFound("Payment not found"));
        }

        // 3. Kiểm tra payment đã xử lý chưa
        if (payment.Status == PaymentStatus.Completed)
        {
            _logger.LogInformation("Payment already processed: {Reference}", request.ReferenceCode);
            return Result.Success(new SePayWebhookResponse { Success = true, Message = "Already processed" });
        }

        // 4. Kiểm tra số tiền khớp
        if (payment.Amount != request.Amount)
        {
            _logger.LogWarning("Amount mismatch for {Reference}: expected {Expected}, got {Got}",
                request.ReferenceCode, payment.Amount, request.Amount);
            return Result.Failure<SePayWebhookResponse>(Error.Validation("Amount mismatch"));
        }

        // 5. Xử lý theo status
        if (request.Status?.ToLower() == "success")
        {
            return await ProcessSuccessPaymentAsync(payment, request, cancellationToken);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.GatewayResponse = System.Text.Json.JsonSerializer.Serialize(request);
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(new SePayWebhookResponse { Success = true, Message = "Payment failed" });
        }
    }

    private async Task<Result<SePayWebhookResponse>> ProcessSuccessPaymentAsync(
        Payment payment,
        SePayWebhookRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Update payment
        payment.Status = PaymentStatus.Completed;
        payment.TransactionId = request.TransactionId;
        payment.BankCode = request.BankCode;
        payment.BankTransactionNo = request.BankTransactionNo;
        payment.GatewayResponse = System.Text.Json.JsonSerializer.Serialize(request);
        payment.CompletedAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        // 2. Activate subscription
        if (payment.SubscriptionId.HasValue)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(payment.SubscriptionId.Value, cancellationToken);
            if (subscription is not null)
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.StartDate = DateTime.UtcNow;
                subscription.UpdatedAt = DateTime.UtcNow;

                await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            }
        }

        // 3. Assign Premium role
        await AssignPremiumRoleAsync(payment.UserId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment processed successfully: {Reference}", request.ReferenceCode);
        return Result.Success(new SePayWebhookResponse { Success = true, Message = "Payment processed" });
    }

    private async Task AssignPremiumRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null) return;

        var premiumRole = await _roleRepository.GetByNameAsync("Premium", cancellationToken);
        if (premiumRole is null) return;

        // Kiểm tra đã có role chưa
        var hasRole = user.UserRoles.Any(ur =>
            ur.RoleId == premiumRole.Id &&
            (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow));

        if (!hasRole)
        {
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = premiumRole.Id,
                AssignedAt = DateTime.UtcNow
                // ExpiresAt = subscription.EndDate (if needed)
            };

            user.UserRoles.Add(userRole);
        }
    }

    private bool VerifySignature(SePayWebhookRequest request)
    {
        var secretKey = _configuration["SEPay:SecretKey"];
        if (string.IsNullOrEmpty(secretKey)) return true; // Skip if not configured

        // Tạo chuỗi data để verify (format tùy theo SEPay documentation)
        var data = $"{request.TransactionId}{request.ReferenceCode}{request.Amount}{secretKey}";
        var computedHash = ComputeSha256Hash(data);

        return computedHash.Equals(request.Signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}
