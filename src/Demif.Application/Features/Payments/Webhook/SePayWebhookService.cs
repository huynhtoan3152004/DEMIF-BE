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
        string? authorizationHeader,
        string? apiKeyQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received SEPay webhook for transaction: {TransactionId}", request.Id);

        // 1. Xác thực API Key (theo format của SePay: `Apikey YOUR_API_KEY`)
        var configuredApiKey = _configuration["SEPay:SecretKey"];
        if (!string.IsNullOrEmpty(configuredApiKey))
        {
            var expectedAuthHeader = $"Apikey {configuredApiKey}";
            bool isHeaderValid = !string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.Equals(expectedAuthHeader, StringComparison.OrdinalIgnoreCase);
            bool isQueryValid = !string.IsNullOrEmpty(apiKeyQuery) && apiKeyQuery.Equals(configuredApiKey, StringComparison.Ordinal);

            if (!isHeaderValid && !isQueryValid)
            {
                _logger.LogWarning("Unauthorized webhook access. Header got: {HeaderGot}, Query got: {QueryGot}", authorizationHeader, apiKeyQuery);
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
        // 1. Update payment
        payment.Status = PaymentStatus.Completed;
        payment.TransactionId = request.Id.ToString(); // Set Id from SePay
        payment.BankCode = request.Gateway;           // VCB, TPB...
        payment.BankTransactionNo = request.ReferenceCode; // SMS Reference code
        payment.GatewayResponse = System.Text.Json.JsonSerializer.Serialize(request);
        payment.CompletedAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        // 2. Activate subscription
        DateTime? roleExpiresAt = null;
        if (payment.SubscriptionId.HasValue)
        {
            var subscription = await _subscriptionRepository.GetByIdWithPlanAsync(payment.SubscriptionId.Value, cancellationToken);
            if (subscription is not null)
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.StartDate = DateTime.UtcNow;
                
                // Recalculate EndDate based on actual actvation time
                if (subscription.Plan?.BillingCycle.GetDurationDays().HasValue == true)
                {
                    subscription.EndDate = DateTime.UtcNow.AddDays(subscription.Plan.BillingCycle.GetDurationDays()!.Value);
                }
                else 
                {
                    subscription.EndDate = null;
                }
                
                subscription.UpdatedAt = DateTime.UtcNow;
                roleExpiresAt = subscription.EndDate;

                await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            }
        }

        // 3. Assign Premium role
        await AssignPremiumRoleAsync(payment.UserId, roleExpiresAt, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment processed successfully: {Code}", request.Code ?? request.Content);
        return Result.Success(new SePayWebhookResponse { Success = true, Message = "Payment processed" });
    }

    private async Task AssignPremiumRoleAsync(Guid userId, DateTime? expiresAt, CancellationToken cancellationToken)
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
    }

    // Gỡ bỏ Verify Signature vì ta đổi qua dùng chứng thực API Key headers
}
