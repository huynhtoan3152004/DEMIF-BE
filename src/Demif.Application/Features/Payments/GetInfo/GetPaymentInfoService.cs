using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Demif.Application.Features.Payments.GetInfo;

/// <summary>
/// Trả thông tin thanh toán SEPay: số tài khoản, số tiền, nội dung CK, QR URL.
/// Dùng để FE hiển thị màn hình "Chờ thanh toán".
/// </summary>
public class GetPaymentInfoService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IConfiguration _configuration;

    public GetPaymentInfoService(IPaymentRepository paymentRepository, IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _configuration = configuration;
    }

    public async Task<Result<GetPaymentInfoResponse>> ExecuteAsync(
        string referenceCode,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByReferenceAsync(referenceCode, cancellationToken);

        if (payment is null)
            return Result.Failure<GetPaymentInfoResponse>(Error.NotFound("Không tìm thấy đơn thanh toán."));

        if (payment.Status == PaymentStatus.Completed)
            return Result.Failure<GetPaymentInfoResponse>(
                new Error("Payment.AlreadyCompleted", "Đơn thanh toán đã được xử lý."));

        if (payment.Status == PaymentStatus.Failed)
            return Result.Failure<GetPaymentInfoResponse>(
                new Error("Payment.Failed", "Đơn thanh toán đã thất bại."));

        var bankCode    = _configuration["SEPay:BankCode"]      ?? "VCB";
        var accountNo   = _configuration["SEPay:AccountNumber"] ?? "";
        var accountName = _configuration["SEPay:AccountName"]   ?? "DEMIF";
        var qrTemplate  = _configuration["SEPay:QrTemplate"]
            ?? "https://img.vietqr.io/image/{bank}-{account}-compact.png?amount={amount}&addInfo={content}&accountName={name}";

        var qrUrl = qrTemplate
            .Replace("{bank}",    bankCode)
            .Replace("{account}", accountNo)
            .Replace("{amount}",  ((long)payment.Amount).ToString())
            .Replace("{content}", Uri.EscapeDataString(payment.PaymentReference))
            .Replace("{name}",    Uri.EscapeDataString(accountName));

        return Result.Success(new GetPaymentInfoResponse
        {
            ReferenceCode = payment.PaymentReference,
            Amount        = payment.Amount,
            Currency      = payment.Currency,
            BankCode      = bankCode,
            AccountNumber = accountNo,
            AccountName   = accountName,
            TransferContent = payment.PaymentReference,
            QrCodeUrl     = qrUrl,
            Status        = payment.Status.ToString(),
            ExpiredAt     = payment.CreatedAt.AddHours(24),   // tự huỷ sau 24h
        });
    }
}

public class GetPaymentInfoResponse
{
    public string  ReferenceCode    { get; set; } = string.Empty;
    public decimal Amount           { get; set; }
    public string  Currency         { get; set; } = "VND";
    public string  BankCode         { get; set; } = string.Empty;
    public string  AccountNumber    { get; set; } = string.Empty;
    public string  AccountName      { get; set; } = string.Empty;
    /// <summary>Nội dung cần ghi khi chuyển khoản (= ReferenceCode)</summary>
    public string  TransferContent  { get; set; } = string.Empty;
    public string  QrCodeUrl        { get; set; } = string.Empty;
    public string  Status           { get; set; } = string.Empty;
    public DateTime ExpiredAt       { get; set; }
}
