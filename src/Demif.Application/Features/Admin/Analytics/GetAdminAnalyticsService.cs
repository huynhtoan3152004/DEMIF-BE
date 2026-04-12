using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Admin.Analytics;

public class GetAdminAnalyticsService
{
    private readonly IApplicationDbContext _dbContext;

    public GetAdminAnalyticsService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminAnalyticsResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // 1. User Metrics (DAU/MAU) using User.LastLoginAt
        var dau = await _dbContext.Users
            .CountAsync(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= today, cancellationToken);
            
        var mau = await _dbContext.Users
            .CountAsync(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= firstDayOfMonth, cancellationToken);

        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);

        // 2. Content Engagement
        var difficultLessons = await _dbContext.Lessons
            .Where(l => l.Status == "published" && l.CompletionsCount > 0)
            .OrderBy(l => l.AvgScore)
            .Take(5)
            .Select(l => new LessonStatItem
            {
                LessonId = l.Id,
                Title = l.Title,
                AvgScore = l.AvgScore,
                CompletionsCount = l.CompletionsCount
            })
            .ToListAsync(cancellationToken);

        var popularLessons = await _dbContext.Lessons
            .Where(l => l.Status == "published" && l.CompletionsCount > 0)
            .OrderByDescending(l => l.CompletionsCount)
            .Take(5)
            .Select(l => new LessonStatItem
            {
                LessonId = l.Id,
                Title = l.Title,
                AvgScore = l.AvgScore,
                CompletionsCount = l.CompletionsCount
            })
            .ToListAsync(cancellationToken);

        // 3. Subscription Income Breakdown
        var completedPayments = await _dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .ToListAsync(cancellationToken);

        var totalRevenue = completedPayments.Sum(p => p.Amount);
        
        // Cần join qua SubscriptionPlans nếu muốn phân biệt, hoặc check Payment PlanId...
        // Tuy nhiên Payment có PlanId nhưng không mapping trực tiếp reference Plan, nên ta lặp bộ nhớ (vì list Plans thường nhỏ)
        var plans = await _dbContext.SubscriptionPlans.ToListAsync(cancellationToken);
        var premiumPlanIds = plans.Where(p => p.Name.Contains("Premium", StringComparison.OrdinalIgnoreCase)).Select(p => p.Id).ToList();
        
        var premiumRevenue = completedPayments
            .Where(p => premiumPlanIds.Contains(p.PlanId))
            .Sum(p => p.Amount);

        var proPlanIds = plans.Where(p => p.Name.Contains("Pro", StringComparison.OrdinalIgnoreCase)).Select(p => p.Id).ToList();
        var proRevenue = completedPayments
            .Where(p => proPlanIds.Contains(p.PlanId))
            .Sum(p => p.Amount);

        return Result.Success(new AdminAnalyticsResponse
        {
            DailyActiveUsers = dau,
            MonthlyActiveUsers = mau,
            TotalUsers = totalUsers,
            DifficultLessons = difficultLessons,
            PopularLessons = popularLessons,
            RevenueStats = new RevenueBreakdown
            {
                TotalRevenue = totalRevenue,
                PremiumRevenue = premiumRevenue,
                ProRevenue = proRevenue,
                OtherRevenue = totalRevenue - premiumRevenue - proRevenue
            }
        });
    }
}

public class AdminAnalyticsResponse
{
    // User Stats
    public int DailyActiveUsers { get; set; }
    public int MonthlyActiveUsers { get; set; }
    public int TotalUsers { get; set; }

    // Content Stats
    public List<LessonStatItem> DifficultLessons { get; set; } = new();
    public List<LessonStatItem> PopularLessons { get; set; } = new();

    // Revenue Breakdown
    public RevenueBreakdown RevenueStats { get; set; } = new();
}

public class LessonStatItem
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal AvgScore { get; set; }
    public int CompletionsCount { get; set; }
}

public class RevenueBreakdown
{
    public decimal TotalRevenue { get; set; }
    public decimal PremiumRevenue { get; set; }
    public decimal ProRevenue { get; set; }
    public decimal OtherRevenue { get; set; }
}
