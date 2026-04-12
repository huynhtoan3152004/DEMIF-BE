using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Admin.Notifications;

public class BroadcastSystemNotificationService
{
    private const int MaxDegreeOfParallelism = 5;

    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<BroadcastSystemNotificationService> _logger;

    public BroadcastSystemNotificationService(
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<BroadcastSystemNotificationService> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<BroadcastSystemNotificationResponse>> ExecuteAsync(
        BroadcastSystemNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = request.Title.Trim();
        var message = request.Message.Trim();
        var actionUrl = string.IsNullOrWhiteSpace(request.ActionUrl)
            ? null
            : request.ActionUrl.Trim();

        var recipients = (await _userRepository.GetBroadcastRecipientsAsync(cancellationToken))
            .Where(user => !string.IsNullOrWhiteSpace(user.Email))
            .Select(user => new BroadcastRecipient(
                Email: user.Email.Trim(),
                Username: string.IsNullOrWhiteSpace(user.Username) ? user.Email.Trim() : user.Username.Trim(),
                Status: user.Status))
            .Where(recipient => recipient.Status is not UserStatus.Banned and not UserStatus.Suspended)
            .GroupBy(recipient => recipient.Email, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (recipients.Count == 0)
        {
            return Result.Failure<BroadcastSystemNotificationResponse>(
                new Error("Admin.Notification.NoRecipients", "Không có người nhận hợp lệ để gửi thông báo."));
        }

        var sentCount = 0;
        var failedCount = 0;

        await Parallel.ForEachAsync(
            recipients,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (recipient, ct) =>
            {
                try
                {
                    await _emailService.SendSystemAnnouncementAsync(
                        recipient.Email,
                        recipient.Username,
                        title,
                        message,
                        actionUrl,
                        ct);

                    Interlocked.Increment(ref sentCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedCount);
                    _logger.LogError(ex, "Failed to send system announcement to {Email}", recipient.Email);
                }
            });

        var notificationId = Guid.NewGuid();
        var response = new BroadcastSystemNotificationResponse(
            NotificationId: notificationId,
            Title: title,
            Message: message,
            ActionUrl: actionUrl,
            Audience: "all-reachable-users",
            Channel: "email",
            EligibleUserCount: recipients.Count,
            SentCount: sentCount,
            FailedCount: failedCount,
            Summary: failedCount == 0
                ? $"Đã gửi thông báo đến {sentCount} người dùng."
                : $"Đã gửi thông báo đến {sentCount}/{recipients.Count} người dùng, {failedCount} thất bại."
        );

        return Result.Success(response);
    }

    private sealed record BroadcastRecipient(string Email, string Username, UserStatus Status);
}

public sealed class BroadcastSystemNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
}

public sealed record BroadcastSystemNotificationResponse(
    Guid NotificationId,
    string Title,
    string Message,
    string? ActionUrl,
    string Audience,
    string Channel,
    int EligibleUserCount,
    int SentCount,
    int FailedCount,
    string Summary);