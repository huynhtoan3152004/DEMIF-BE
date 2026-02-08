using System.Text.Json;
using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Subscriptions.Admin;

/// <summary>
/// Admin Subscription Plan Service - CRUD và thống kê gói subscription
/// </summary>
public class AdminSubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IApplicationDbContext _dbContext;

    public AdminSubscriptionPlanService(
        ISubscriptionPlanRepository planRepository,
        IApplicationDbContext dbContext)
    {
        _planRepository = planRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lấy tất cả plans với thống kê
    /// </summary>
    public async Task<Result<SubscriptionStatsResponse>> GetAllWithStatsAsync(CancellationToken cancellationToken = default)
    {
        var plansWithStats = await _planRepository.GetPlansWithStatsAsync(cancellationToken);

        var plans = plansWithStats.Select(p => new PlanAdminDto
        {
            Id = p.Plan.Id,
            Name = p.Plan.Name,
            Tier = p.Plan.Tier.ToString(),
            Price = p.Plan.Price,
            Currency = p.Plan.Currency,
            BillingCycle = p.Plan.BillingCycle.ToString(),
            DurationDays = p.Plan.DurationDays,
            Features = ParseFeatures(p.Plan.Features),
            BadgeText = p.Plan.BadgeText,
            BadgeColor = p.Plan.BadgeColor,
            IsActive = p.Plan.IsActive,
            TotalSubscribers = p.SubscriberCount,
            ActiveSubscribers = p.ActiveCount,
            CreatedAt = p.Plan.CreatedAt,
            UpdatedAt = p.Plan.UpdatedAt
        }).ToList();

        return Result.Success(new SubscriptionStatsResponse
        {
            TotalPlans = plans.Count,
            TotalSubscribers = plans.Sum(p => p.TotalSubscribers),
            ActiveSubscribers = plans.Sum(p => p.ActiveSubscribers),
            TotalRevenue = 0, // TODO: Calculate from payments
            Plans = plans
        });
    }

    /// <summary>
    /// Tạo plan mới
    /// </summary>
    public async Task<Result<Guid>> CreateAsync(CreateUpdatePlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = new SubscriptionPlan
        {
            Name = request.Name,
            Tier = request.Tier,
            Price = request.Price,
            Currency = request.Currency,
            BillingCycle = request.BillingCycle,
            DurationDays = request.DurationDays,
            Features = request.Features != null ? JsonSerializer.Serialize(request.Features) : null,
            BadgeText = request.BadgeText,
            BadgeColor = request.BadgeColor,
            IsActive = request.IsActive
        };

        await _planRepository.AddAsync(plan, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.Id);
    }

    /// <summary>
    /// Cập nhật plan (bao gồm giá)
    /// </summary>
    public async Task<Result> UpdateAsync(Guid id, CreateUpdatePlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy gói đăng ký."));
        }

        plan.Name = request.Name;
        plan.Tier = request.Tier;
        plan.Price = request.Price;
        plan.Currency = request.Currency;
        plan.BillingCycle = request.BillingCycle;
        plan.DurationDays = request.DurationDays;
        plan.Features = request.Features != null ? JsonSerializer.Serialize(request.Features) : null;
        plan.BadgeText = request.BadgeText;
        plan.BadgeColor = request.BadgeColor;
        plan.IsActive = request.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await _planRepository.UpdateAsync(plan, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Xóa plan (soft delete - set IsActive = false)
    /// </summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return Result.Failure(Error.NotFound("Không tìm thấy gói đăng ký."));
        }

        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;

        await _planRepository.UpdateAsync(plan, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static List<string> ParseFeatures(string? featuresJson)
    {
        if (string.IsNullOrEmpty(featuresJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(featuresJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
