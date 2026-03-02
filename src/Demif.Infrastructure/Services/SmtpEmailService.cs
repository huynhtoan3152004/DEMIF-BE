using Demif.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Demif.Infrastructure.Services;

/// <summary>
/// SMTP Email Service dùng Gmail App Password.
/// Config trong appsettings.json → Smtp section.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromName;
    private readonly string _fromEmail;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
        _host = configuration["Smtp:Host"] ?? "smtp.gmail.com";
        _port = int.Parse(configuration["Smtp:Port"] ?? "587");
        _username = configuration["Smtp:Username"] ?? throw new InvalidOperationException("Smtp:Username is not configured.");
        _password = configuration["Smtp:Password"] ?? throw new InvalidOperationException("Smtp:Password is not configured.");
        _fromName = configuration["Smtp:FromName"] ?? "DEMIF App";
        _fromEmail = configuration["Smtp:FromEmail"] ?? _username;
    }

    public async Task SendEmailVerificationAsync(
        string toEmail, string username, string verifyUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "✅ Xác nhận email đăng ký DEMIF";
        var body = BuildVerificationEmailHtml(username, verifyUrl);
        await SendAsync(toEmail, subject, body);
        _logger.LogInformation("Verification email sent to {Email}", toEmail);
    }

    public async Task ResendEmailVerificationAsync(
        string toEmail, string username, string verifyUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "🔁 Gửi lại email xác nhận DEMIF";
        var body = BuildVerificationEmailHtml(username, verifyUrl, isResend: true);
        await SendAsync(toEmail, subject, body);
        _logger.LogInformation("Verification email resent to {Email}", toEmail);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_host, _port)
        {
            Credentials = new NetworkCredential(_username, _password),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }

    private static string BuildVerificationEmailHtml(string username, string verifyUrl, bool isResend = false)
    {
        var heading = isResend ? "Gửi lại xác nhận email" : "Xác nhận email của bạn";
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"></head>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f5f5f5;">
          <div style="background: white; border-radius: 12px; padding: 32px; box-shadow: 0 2px 8px rgba(0,0,0,0.08);">
            <h1 style="color: #2563eb; margin-bottom: 8px;">🎧 DEMIF</h1>
            <h2 style="color: #1e293b;">{heading}</h2>
            <p style="color: #64748b;">Xin chào <strong>{username}</strong>,</p>
            <p style="color: #64748b;">Cảm ơn bạn đã đăng ký DEMIF! Bấm nút bên dưới để xác nhận email và bắt đầu học.</p>
            <div style="text-align: center; margin: 32px 0;">
              <a href="{verifyUrl}"
                 style="background: #2563eb; color: white; padding: 14px 32px; border-radius: 8px;
                        text-decoration: none; font-size: 16px; font-weight: bold; display: inline-block;">
                Xác nhận Email
              </a>
            </div>
            <p style="color: #94a3b8; font-size: 13px;">Link có hiệu lực trong <strong>24 giờ</strong>.</p>
            <p style="color: #94a3b8; font-size: 13px;">Nếu bạn không đăng ký tài khoản này, hãy bỏ qua email này.</p>
            <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;">
            <p style="color: #cbd5e1; font-size: 12px; text-align: center;">© 2025 DEMIF App</p>
          </div>
        </body>
        </html>
        """;
    }
}
