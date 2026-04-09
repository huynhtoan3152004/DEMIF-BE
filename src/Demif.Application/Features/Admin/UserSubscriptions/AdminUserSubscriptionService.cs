using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Admin.UserSubscriptions;

/// <summary>
/// Admin service for managing user subscriptions (view, extend, cancel).
/// </summary>
public class AdminUserSubscriptionService
{
    private readonly IUserSubscriptionRepository _subscriptionRepo;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IApplicationDbContext _context;

    public AdminUserSubscriptionService(
        IUserSubscriptionRepository subscriptionRepo,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IApplicationDbContext context)
    {
        _subscriptionRepo = subscriptionRepo;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _context = context;
    }

    // ─── Get paginated list ────────────────────────────────────────────────────

    public async Task<Result<AdminUserSubscriptionPagedResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? status,
        string? search,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        SubscriptionStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubscriptionStatus>(status, true, out var s))
            parsedStatus = s;

        var (items, total) = await _subscriptionRepo.GetAllWithUsersAsync(page, pageSize, parsedStatus?.ToString(), search, ct);

        var responses = items.Select(sub => new AdminUserSubscriptionListItemResponse(
            Id: sub.Id,
            UserId: sub.UserId,
            UserEmail: sub.User?.Email ?? string.Empty,
            UserName: sub.User?.Username ?? string.Empty,
            PlanId: sub.PlanId,
            PlanName: sub.Plan?.Name ?? string.Empty,
            Tier: sub.Plan?.Tier ?? SubscriptionTier.Free,
            Status: sub.Status,
            StartDate: sub.StartDate,
            EndDate: sub.EndDate,
            AutoRenew: sub.AutoRenew,
            CreatedAt: sub.CreatedAt
        ));

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return Result<AdminUserSubscriptionPagedResponse>.Success(
            new AdminUserSubscriptionPagedResponse(responses, total, page, pageSize, totalPages));
    }

    // ─── Get detail ───────────────────────────────────────────────────────────

    public async Task<Result<AdminUserSubscriptionDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sub = await _subscriptionRepo.GetByIdWithDetailsAsync(id, ct);
        if (sub is null)
            return Result.Failure<AdminUserSubscriptionDetailResponse>(
                new Error("Admin.Subscription.NotFound", "Subscription not found."));

        var payments = sub.Payments.Select(p => new AdminPaymentSummaryResponse(
            Id: p.Id,
            Amount: p.Amount,
            Currency: p.Currency,
            PaymentMethod: p.PaymentMethod,
            Status: p.Status,
            TransactionId: p.TransactionId,
            CompletedAt: p.CompletedAt,
            CreatedAt: p.CreatedAt
        ));

        return Result<AdminUserSubscriptionDetailResponse>.Success(new AdminUserSubscriptionDetailResponse(
            Id: sub.Id,
            UserId: sub.UserId,
            UserEmail: sub.User?.Email ?? string.Empty,
            UserName: sub.User?.Username ?? string.Empty,
            PlanId: sub.PlanId,
            PlanName: sub.Plan?.Name ?? string.Empty,
            Tier: sub.Plan?.Tier ?? SubscriptionTier.Free,
            PlanPrice: sub.Plan?.Price ?? 0,
            Currency: sub.Plan?.Currency ?? "VND",
            Status: sub.Status,
            StartDate: sub.StartDate,
            EndDate: sub.EndDate,
            AutoRenew: sub.AutoRenew,
            CreatedAt: sub.CreatedAt,
            UpdatedAt: sub.UpdatedAt,
            Payments: payments
        ));
    }

    // ─── Extend subscription ──────────────────────────────────────────────────

    public async Task<Result<string>> ExtendAsync(Guid id, ExtendSubscriptionRequest request, CancellationToken ct = default)
    {
        if (request.Days <= 0)
            return Result.Failure<string>(new Error("Admin.Subscription.InvalidDays", "Days must be greater than 0."));

        var sub = await _subscriptionRepo.GetByIdAsync(id, ct);
        if (sub is null)
            return Result.Failure<string>(new Error("Admin.Subscription.NotFound", "Subscription not found."));

        var baseDate = sub.EndDate.HasValue && sub.EndDate.Value > DateTime.UtcNow
            ? sub.EndDate.Value
            : DateTime.UtcNow;

        sub.EndDate = baseDate.AddDays(request.Days);
        sub.Status = SubscriptionStatus.Active;
        sub.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepo.UpdateAsync(sub, ct);

        // Sync role
        await SyncUserRoleAsync(sub.UserId, ct);
        await _context.SaveChangesAsync(ct);

        return Result<string>.Success($"Subscription extended by {request.Days} day(s). New end date: {sub.EndDate:yyyy-MM-dd}.");
    }

    // ─── Cancel subscription ──────────────────────────────────────────────────

    public async Task<Result<string>> CancelAsync(Guid id, CancelSubscriptionRequest request, CancellationToken ct = default)
    {
        var sub = await _subscriptionRepo.GetByIdAsync(id, ct);
        if (sub is null)
            return Result.Failure<string>(new Error("Admin.Subscription.NotFound", "Subscription not found."));

        if (sub.Status == SubscriptionStatus.Cancelled)
            return Result.Failure<string>(new Error("Admin.Subscription.AlreadyCancelled", "Subscription is already cancelled."));

        sub.Status = SubscriptionStatus.Cancelled;
        sub.AutoRenew = false;
        sub.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepo.UpdateAsync(sub, ct);

        // Sync role
        await SyncUserRoleAsync(sub.UserId, ct);
        await _context.SaveChangesAsync(ct);

        return Result<string>.Success("Subscription cancelled successfully.");
    }

    // ─── Full CRUD ────────────────────────────────────────────────────────────

    public async Task<Result<Guid>> CreateAsync(CreateUserSubscriptionRequest request, CancellationToken ct = default)
    {
        // 1. Auto-expire old active/pending subscriptions
        var existingSubs = await _subscriptionRepo.GetByUserIdAsync(request.UserId, ct);
        foreach (var existing in existingSubs.Where(s => s.Status is SubscriptionStatus.Active or SubscriptionStatus.PendingPayment))
        {
            existing.Status = SubscriptionStatus.Expired;
            existing.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepo.UpdateAsync(existing, ct);
        }

        // 2. Create new subscription
        var sub = new UserSubscription
        {
            UserId = request.UserId,
            PlanId = request.PlanId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            AutoRenew = request.AutoRenew,
            CreatedAt = DateTime.UtcNow
        };

        await _subscriptionRepo.AddAsync(sub, ct);
        
        // 3. Sync role
        await SyncUserRoleAsync(request.UserId, ct);
        
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(sub.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateUserSubscriptionRequest request, CancellationToken ct = default)
    {
        var sub = await _subscriptionRepo.GetByIdAsync(id, ct);
        if (sub is null)
            return Result.Failure(new Error("Admin.Subscription.NotFound", "Subscription not found."));

        sub.PlanId = request.PlanId;
        sub.StartDate = request.StartDate;
        sub.EndDate = request.EndDate;
        sub.Status = request.Status;
        sub.AutoRenew = request.AutoRenew;
        sub.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepo.UpdateAsync(sub, ct);

        // Sync role
        await SyncUserRoleAsync(sub.UserId, ct);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sub = await _subscriptionRepo.GetByIdAsync(id, ct);
        if (sub is null)
            return Result.Failure(new Error("Admin.Subscription.NotFound", "Subscription not found."));

        // Soft delete: Mark as Cancelled
        sub.Status = SubscriptionStatus.Cancelled;
        sub.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepo.UpdateAsync(sub, ct);

        // Sync role
        await SyncUserRoleAsync(sub.UserId, ct);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task SyncUserRoleAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, ct);
        if (user == null) return;

        var premiumRole = await _roleRepository.GetByNameAsync("Premium", ct);
        if (premiumRole == null) return;

        // Find the latest active subscription to get the correct expiration date
        var latestActiveSub = (await _subscriptionRepo.GetByUserIdAsync(userId, ct))
            .Where(s => s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndDate ?? DateTime.MaxValue)
            .FirstOrDefault();

        var existingUserRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == premiumRole.Id);

        if (latestActiveSub != null)
        {
            // Should have premium role
            if (existingUserRole == null)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = premiumRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    ExpiresAt = latestActiveSub.EndDate
                });
            }
            else
            {
                existingUserRole.ExpiresAt = latestActiveSub.EndDate;
            }
        }
        else
        {
            // Should NOT have active premium role (or it should be expired)
            if (existingUserRole != null)
            {
                // We set expiration to now so it effectively expires immediately
                // Alternatively, we could remove the UserRole record, but setting ExpiresAt is safer for history
                existingUserRole.ExpiresAt = DateTime.UtcNow;
            }
        }
    }
