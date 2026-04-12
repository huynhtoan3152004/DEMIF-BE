namespace Demif.Application.Abstractions.Services;

/// <summary>
/// Service gửi email hệ thống (verification, notification)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi email xác nhận tài khoản khi đăng ký
    /// </summary>
    Task SendEmailVerificationAsync(string toEmail, string username, string verifyUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi lại email xác nhận (resend)
    /// </summary>
    Task ResendEmailVerificationAsync(string toEmail, string username, string verifyUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi email Reset Password với Link Token
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string username, string resetUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi email thông báo hệ thống / ưu đãi cho user.
    /// </summary>
    Task SendSystemAnnouncementAsync(
        string toEmail,
        string username,
        string title,
        string message,
        string? actionUrl,
        CancellationToken cancellationToken = default);
}
