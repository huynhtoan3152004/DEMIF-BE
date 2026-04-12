using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Me.Notifications;

public class GetMyNotificationsService
{
    private readonly IApplicationDbContext _context;

    public GetMyNotificationsService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<GetMyNotificationsResponse>> ExecuteAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var baseQuery = _context.UserNotifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var unreadCount = await baseQuery.CountAsync(notification => !notification.IsRead, cancellationToken);

        var items = await baseQuery
            .OrderBy(notification => notification.IsRead)
            .ThenByDescending(notification => notification.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(notification => new NotificationListItemResponse(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.ActionUrl,
                notification.Channel,
                notification.IsRead,
                notification.ReadAt,
                notification.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Result.Success(new GetMyNotificationsResponse(
            Items: items,
            TotalCount: totalCount,
            UnreadCount: unreadCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages));
    }
}

public class GetUnreadNotificationCountService
{
    private readonly IApplicationDbContext _context;

    public GetUnreadNotificationCountService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UnreadNotificationCountResponse>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadCount = await _context.UserNotifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken);

        return Result.Success(new UnreadNotificationCountResponse(unreadCount));
    }
}

public class MarkNotificationAsReadService
{
    private readonly IApplicationDbContext _context;

    public MarkNotificationAsReadService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MarkNotificationAsReadResponse>> ExecuteAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return Result.Failure<MarkNotificationAsReadResponse>(
                new Error("Notification.NotFound", "Notification not found."));
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new MarkNotificationAsReadResponse(
            notification.Id,
            notification.IsRead,
            notification.ReadAt));
    }
}

public class MarkAllNotificationsAsReadService
{
    private readonly IApplicationDbContext _context;

    public MarkAllNotificationsAsReadService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MarkAllNotificationsAsReadResponse>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _context.UserNotifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadNotifications.Count == 0)
        {
            return Result.Success(new MarkAllNotificationsAsReadResponse(0));
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            notification.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new MarkAllNotificationsAsReadResponse(unreadNotifications.Count));
    }
}

public sealed record NotificationListItemResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? ActionUrl,
    string Channel,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public sealed record GetMyNotificationsResponse(
    IReadOnlyCollection<NotificationListItemResponse> Items,
    int TotalCount,
    int UnreadCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record UnreadNotificationCountResponse(int UnreadCount);

public sealed record MarkNotificationAsReadResponse(
    Guid Id,
    bool IsRead,
    DateTime? ReadAt);

public sealed record MarkAllNotificationsAsReadResponse(int UpdatedCount);
