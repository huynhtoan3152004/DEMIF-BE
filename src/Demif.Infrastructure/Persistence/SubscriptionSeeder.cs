using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Demif.Infrastructure.Persistence;

/// <summary>
/// Database seeder for subscription plans
/// </summary>
public static class SubscriptionSeeder
{
    public static async Task SeedSubscriptionPlansAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Skip if already seeded
        if (await context.SubscriptionPlans.AnyAsync())
            return;

        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                Name = "Premium Tháng",
                Tier = SubscriptionTier.Premium,
                Price = 199000,
                Currency = "VND",
                BillingCycle = BillingCycle.Monthly,
                DurationDays = 30,
                Features = "[\"Không giới hạn bài học\", \"Bài học Premium độc quyền\", \"AI Feedback\", \"Không quảng cáo\"]",
                BadgeText = "Phổ biến",
                BadgeColor = "#3B82F6",
                IsActive = true
            },
            new()
            {
                Name = "Premium Năm",
                Tier = SubscriptionTier.Premium,
                Price = 1499000,
                Currency = "VND",
                BillingCycle = BillingCycle.Yearly,
                DurationDays = 365,
                Features = "[\"Tất cả tính năng Premium Tháng\", \"Tiết kiệm 37%\", \"Ưu tiên hỗ trợ\", \"Truy cập sớm tính năng mới\"]",
                BadgeText = "Tiết kiệm nhất",
                BadgeColor = "#10B981",
                IsActive = true
            },
            new()
            {
                Name = "Premium Vĩnh viễn",
                Tier = SubscriptionTier.Premium,
                Price = 3999000,
                Currency = "VND",
                BillingCycle = BillingCycle.Lifetime,
                DurationDays = null, // null = lifetime
                Features = "[\"Tất cả tính năng Premium\", \"Trả một lần, dùng mãi mãi\", \"VIP Support\", \"Founder Badge\"]",
                BadgeText = "Một lần",
                BadgeColor = "#8B5CF6",
                IsActive = true
            }
        };

        await context.SubscriptionPlans.AddRangeAsync(plans);
        await context.SaveChangesAsync();
    }
}
