using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Application.Features.Admin.Analytics.Lessons.Access;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Admin.Analytics;

public class GetAdminAnalyticsService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetAdminLessonAccessAnalyticsService _lessonAccessAnalyticsService;

    public GetAdminAnalyticsService(IApplicationDbContext dbContext, GetAdminLessonAccessAnalyticsService lessonAccessAnalyticsService)
    {
        _dbContext = dbContext;
        _lessonAccessAnalyticsService = lessonAccessAnalyticsService;
    }

    public GetAdminAnalyticsService(IApplicationDbContext dbContext)
        : this(dbContext, new GetAdminLessonAccessAnalyticsService(dbContext))
    {
    }

    public async Task<Result<AdminAnalyticsResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var expiringSoonThreshold = now.AddDays(30);

        var users = await _dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
        var lessons = await _dbContext.Lessons.AsNoTracking().ToListAsync(cancellationToken);
        var exercises = await _dbContext.UserExercises.AsNoTracking().Include(x => x.Lesson).ToListAsync(cancellationToken);
        var vocabularies = await _dbContext.UserVocabularies.AsNoTracking().Include(x => x.Lesson).ToListAsync(cancellationToken);
        var plans = await _dbContext.SubscriptionPlans.AsNoTracking().ToListAsync(cancellationToken);
        var subscriptions = await _dbContext.UserSubscriptions.AsNoTracking().Include(x => x.Plan).ToListAsync(cancellationToken);
        var payments = await _dbContext.Payments.AsNoTracking().Include(x => x.Plan).ToListAsync(cancellationToken);
        var blogs = await _dbContext.Blogs.AsNoTracking().ToListAsync(cancellationToken);
        var notifications = await _dbContext.UserNotifications.AsNoTracking().ToListAsync(cancellationToken);
        var progresses = await _dbContext.UserProgresses.AsNoTracking().ToListAsync(cancellationToken);
        var streaks = await _dbContext.UserStreaks.AsNoTracking().ToListAsync(cancellationToken);
        var analyticsProfiles = await _dbContext.UserAnalytics.AsNoTracking().ToListAsync(cancellationToken);
        var userRoles = await _dbContext.UserRoles.AsNoTracking().Include(x => x.Role).ToListAsync(cancellationToken);

        var activeUsers = users.Count(x => x.Status == UserStatus.Active);
        var pendingUsers = users.Count(x => x.Status == UserStatus.Pending);
        var inactiveUsers = users.Count(x => x.Status == UserStatus.Inactive);
        var suspendedUsers = users.Count(x => x.Status == UserStatus.Suspended);
        var bannedUsers = users.Count(x => x.Status == UserStatus.Banned);
        var verifiedUsers = users.Count(x => x.IsEmailVerified);
        var newUsersToday = users.Count(x => x.CreatedAt >= today);
        var newUsersThisMonth = users.Count(x => x.CreatedAt >= monthStart);
        var dau = users.Count(x => x.LastLoginAt.HasValue && x.LastLoginAt.Value >= today);
        var mau = users.Count(x => x.LastLoginAt.HasValue && x.LastLoginAt.Value >= monthStart);
        var usersLast7Days = users.Count(x => x.LastLoginAt.HasValue && x.LastLoginAt.Value >= weekAgo);

        var totalLessons = lessons.Count;
        var publishedLessons = lessons.Count(x => NormalizeStatus(x.Status) == "published");
        var draftLessons = lessons.Count(x => NormalizeStatus(x.Status) == "draft");
        var archivedLessons = lessons.Count(x => NormalizeStatus(x.Status) == "archived");
        var dictationLessons = lessons.Count(x => NormalizeGroupKey(x.LessonType).Equals("dictation", StringComparison.OrdinalIgnoreCase));
        var shadowingLessons = lessons.Count(x => NormalizeGroupKey(x.LessonType).Equals("shadowing", StringComparison.OrdinalIgnoreCase));
        var premiumLessons = lessons.Count(x => x.IsPremiumOnly);
        var audioLessons = lessons.Count(x => string.IsNullOrWhiteSpace(x.MediaType) || NormalizeGroupKey(x.MediaType).Equals("audio", StringComparison.OrdinalIgnoreCase));
        var youtubeLessons = lessons.Count(x => NormalizeGroupKey(x.MediaType).Equals("youtube", StringComparison.OrdinalIgnoreCase));
        var totalLessonCompletions = lessons.Sum(x => x.CompletionsCount);
        var averageLessonScore = lessons.Count > 0 ? Math.Round(lessons.Average(x => x.AvgScore), 1) : 0;

        var totalExercises = exercises.Count;
        var dictationExercises = exercises.Count(x => x.ExerciseType == ExerciseType.Dictation);
        var shadowingExercises = exercises.Count(x => x.ExerciseType == ExerciseType.Shadowing);
        var perfectScoresCount = exercises.Count(x => x.Score == 100);
        var averageExerciseScore = exercises.Count > 0 ? Math.Round(exercises.Average(x => x.Score), 1) : 0;
        var highestExerciseScore = exercises.Count > 0 ? exercises.Max(x => x.Score) : 0;
        var averageExerciseTimeSpent = exercises.Where(x => x.TimeSpentSeconds.HasValue).Select(x => x.TimeSpentSeconds!.Value).DefaultIfEmpty(0).Average();
        var exercisesToday = exercises.Count(x => x.CompletedAt >= today);
        var exercisesThisMonth = exercises.Count(x => x.CompletedAt >= monthStart);

        var totalVocabulary = vocabularies.Count;
        var dueVocabulary = vocabularies.Count(x => !x.IsMastered && (x.NextReviewAt == null || x.NextReviewAt <= now));
        var overdueVocabulary = vocabularies.Count(x => !x.IsMastered && x.NextReviewAt.HasValue && x.NextReviewAt.Value < now);
        var newVocabulary = vocabularies.Count(x => x.ReviewCount == 0 && !x.IsMastered);
        var masteredVocabulary = vocabularies.Count(x => x.IsMastered);
        var learningVocabulary = Math.Max(0, totalVocabulary - masteredVocabulary - newVocabulary);
        var vocabularyLessons = vocabularies.Select(x => x.LessonId).Distinct().Count();
        var vocabularyTopics = vocabularies.Select(x => NormalizeTopicKey(x.Topic)).Distinct().Count();
        var vocabularyRecentCount = vocabularies.Count(x => x.CreatedAt >= weekAgo);

        var totalPlans = plans.Count;
        var activePlans = plans.Count(x => x.IsActive);
        var freePlans = plans.Count(x => x.Tier == SubscriptionTier.Free);
        var basicPlans = plans.Count(x => x.Tier == SubscriptionTier.Basic);
        var premiumPlans = plans.Count(x => x.Tier == SubscriptionTier.Premium);
        var lifetimePlans = plans.Count(x => x.BillingCycle == BillingCycle.Lifetime);

        var totalSubscriptions = subscriptions.Count;
        var activeSubscriptions = subscriptions.Count(x => x.Status == SubscriptionStatus.Active);
        var pendingSubscriptions = subscriptions.Count(x => x.Status == SubscriptionStatus.PendingPayment);
        var expiredSubscriptions = subscriptions.Count(x => x.Status == SubscriptionStatus.Expired);
        var cancelledSubscriptions = subscriptions.Count(x => x.Status == SubscriptionStatus.Cancelled);
        var autoRenewSubscriptions = subscriptions.Count(x => x.AutoRenew);
        var expiringSoonSubscriptions = subscriptions.Count(x => x.Status == SubscriptionStatus.Active && x.EndDate.HasValue && x.EndDate.Value >= now && x.EndDate.Value <= expiringSoonThreshold);

        var totalPayments = payments.Count;
        var completedPayments = payments.Count(x => x.Status == PaymentStatus.Completed);
        var pendingPayments = payments.Count(x => x.Status == PaymentStatus.Pending);
        var failedPayments = payments.Count(x => x.Status == PaymentStatus.Failed);
        var refundedPayments = payments.Count(x => x.Status == PaymentStatus.Refunded);
        var totalRevenue = payments.Where(x => x.Status == PaymentStatus.Completed).Sum(x => x.Amount);
        var todayRevenue = payments.Where(x => x.Status == PaymentStatus.Completed && x.CreatedAt >= today).Sum(x => x.Amount);
        var monthRevenue = payments.Where(x => x.Status == PaymentStatus.Completed && x.CreatedAt >= monthStart).Sum(x => x.Amount);
        var averagePaymentAmount = payments.Where(x => x.Status == PaymentStatus.Completed).Select(x => x.Amount).DefaultIfEmpty(0).Average();

        var totalBlogs = blogs.Count;
        var publishedBlogs = blogs.Count(x => NormalizeStatus(x.Status) == "published");
        var draftBlogs = blogs.Count(x => NormalizeStatus(x.Status) == "draft");
        var archivedBlogs = blogs.Count(x => NormalizeStatus(x.Status) == "archived");
        var totalBlogViews = blogs.Sum(x => x.ViewCount);
        var averageBlogViews = blogs.Count > 0 ? Math.Round(blogs.Average(x => x.ViewCount), 1) : 0;

        var totalNotifications = notifications.Count;
        var unreadNotifications = notifications.Count(x => !x.IsRead);
        var readNotifications = notifications.Count(x => x.IsRead);
        var notificationsToday = notifications.Count(x => x.CreatedAt >= today);
        var notificationsThisMonth = notifications.Count(x => x.CreatedAt >= monthStart);

        var usersWithProgress = progresses.Count;
        var avgPoints = progresses.Count > 0 ? Math.Round(progresses.Average(x => x.TotalPoints), 1) : 0;
        var avgMinutes = progresses.Count > 0 ? Math.Round(progresses.Average(x => x.TotalMinutes), 1) : 0;
        var avgLessonsCompleted = progresses.Count > 0 ? Math.Round(progresses.Average(x => x.LessonsCompleted), 1) : 0;
        var avgDictationProgressScore = progresses.Count > 0 ? Math.Round(progresses.Average(x => x.AvgDictationScore), 1) : 0;
        var avgShadowingProgressScore = progresses.Count > 0 ? Math.Round(progresses.Average(x => x.AvgShadowingScore), 1) : 0;

        var usersWithStreak = streaks.Count;
        var avgCurrentStreak = streaks.Count > 0 ? Math.Round(streaks.Average(x => x.CurrentStreak), 1) : 0;
        var longestStreak = streaks.Count > 0 ? streaks.Max(x => x.CurrentStreak) : 0;

        var topUsers = analyticsProfiles
            .Select(profile =>
            {
                var user = users.FirstOrDefault(x => x.Id == profile.UserId);
                var streak = streaks.FirstOrDefault(x => x.UserId == profile.UserId);
                var progress = progresses.FirstOrDefault(x => x.UserId == profile.UserId);

                return new TopUserItem
                {
                    UserId = profile.UserId,
                    Username = user?.Username ?? string.Empty,
                    Email = user?.Email ?? string.Empty,
                    CurrentLevel = user?.CurrentLevel.ToString() ?? string.Empty,
                    TotalPoints = profile.TotalPoints,
                    TotalMinutes = profile.TotalLearningMinutes,
                    EngagementScore = profile.EngagementScore,
                    CurrentStreak = profile.CurrentStreak > 0 ? profile.CurrentStreak : streak?.CurrentStreak ?? 0,
                    LongestStreak = profile.LongestStreak > 0 ? profile.LongestStreak : streak?.LongestStreak ?? 0,
                    LastLoginAt = user?.LastLoginAt,
                    LessonsCompleted = profile.TotalLessonsCompleted > 0 ? profile.TotalLessonsCompleted : progress?.LessonsCompleted ?? 0
                };
            })
            .OrderByDescending(x => x.EngagementScore)
            .ThenByDescending(x => x.TotalPoints)
            .Take(5)
            .ToList();

        var popularLessons = lessons
            .Where(x => NormalizeStatus(x.Status) == "published")
            .OrderByDescending(x => x.CompletionsCount)
            .ThenByDescending(x => x.AvgScore)
            .Take(5)
            .Select(x => new LessonSummaryItem
            {
                LessonId = x.Id,
                Title = x.Title,
                Status = x.Status,
                LessonType = x.LessonType,
                Level = x.Level,
                Category = x.Category,
                AvgScore = x.AvgScore,
                CompletionsCount = x.CompletionsCount,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        var difficultLessons = lessons
            .Where(x => NormalizeStatus(x.Status) == "published" && x.CompletionsCount > 0)
            .OrderBy(x => x.AvgScore)
            .ThenByDescending(x => x.CompletionsCount)
            .Take(5)
            .Select(x => new LessonSummaryItem
            {
                LessonId = x.Id,
                Title = x.Title,
                Status = x.Status,
                LessonType = x.LessonType,
                Level = x.Level,
                Category = x.Category,
                AvgScore = x.AvgScore,
                CompletionsCount = x.CompletionsCount,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        var recentLessons = lessons
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new LessonSummaryItem
            {
                LessonId = x.Id,
                Title = x.Title,
                Status = x.Status,
                LessonType = x.LessonType,
                Level = x.Level,
                Category = x.Category,
                AvgScore = x.AvgScore,
                CompletionsCount = x.CompletionsCount,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        var recentPayments = payments
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new PaymentSummaryItem
            {
                PaymentId = x.Id,
                Amount = x.Amount,
                Currency = x.Currency,
                Status = x.Status.ToString(),
                PaymentMethod = x.PaymentMethod,
                PlanName = x.Plan?.Name ?? string.Empty,
                PlanTier = x.Plan?.Tier.ToString() ?? string.Empty,
                CreatedAt = x.CreatedAt,
                CompletedAt = x.CompletedAt
            })
            .ToList();

        var popularBlogs = blogs
            .OrderByDescending(x => x.ViewCount)
            .Take(5)
            .Select(x => new BlogSummaryItem
            {
                BlogId = x.Id,
                Title = x.Title,
                Status = x.Status,
                ViewCount = x.ViewCount,
                CreatedAt = x.CreatedAt,
                AuthorId = x.AuthorId
            })
            .ToList();

        var recentBlogs = blogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new BlogSummaryItem
            {
                BlogId = x.Id,
                Title = x.Title,
                Status = x.Status,
                ViewCount = x.ViewCount,
                CreatedAt = x.CreatedAt,
                AuthorId = x.AuthorId
            })
            .ToList();

        var topVocabularyTopics = vocabularies
            .GroupBy(x => NormalizeTopicKey(x.Topic))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .Take(10)
            .ToList();

        var topVocabularyLessons = vocabularies
            .GroupBy(x => new { x.LessonId, Title = x.Lesson?.Title ?? string.Empty })
            .Select(group => new LessonSummaryItem
            {
                LessonId = group.Key.LessonId,
                Title = group.Key.Title,
                CompletionsCount = group.Count(),
                AvgScore = 0,
                Status = string.Empty,
                LessonType = string.Empty,
                Level = string.Empty,
                Category = string.Empty,
                CreatedAt = group.Max(item => item.CreatedAt)
            })
            .OrderByDescending(x => x.CompletionsCount)
            .ThenBy(x => x.Title)
            .Take(10)
            .ToList();

        var subscriptionStatuses = new List<StatCountItem>
        {
            new() { Key = SubscriptionStatus.Active.ToString(), Count = activeSubscriptions },
            new() { Key = SubscriptionStatus.PendingPayment.ToString(), Count = pendingSubscriptions },
            new() { Key = SubscriptionStatus.Expired.ToString(), Count = expiredSubscriptions },
            new() { Key = SubscriptionStatus.Cancelled.ToString(), Count = cancelledSubscriptions }
        };

        var planTierBreakdown = subscriptions
            .GroupBy(x => x.Plan?.Tier.ToString() ?? SubscriptionTier.Free.ToString())
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var billingCycleBreakdown = plans
            .GroupBy(x => x.BillingCycle.ToString())
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var roleBreakdown = userRoles
            .GroupBy(x => x.Role?.Name ?? "Unassigned")
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var paymentStatuses = new List<StatCountItem>
        {
            new() { Key = PaymentStatus.Completed.ToString(), Count = completedPayments },
            new() { Key = PaymentStatus.Pending.ToString(), Count = pendingPayments },
            new() { Key = PaymentStatus.Failed.ToString(), Count = failedPayments },
            new() { Key = PaymentStatus.Refunded.ToString(), Count = refundedPayments }
        };

        var paymentMethods = payments
            .GroupBy(x => NormalizeGroupKey(x.PaymentMethod))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var revenueByTier = payments
            .Where(x => x.Status == PaymentStatus.Completed)
            .GroupBy(x => x.Plan?.Tier.ToString() ?? SubscriptionTier.Free.ToString())
            .Select(group => new RevenueItem { Key = group.Key, Amount = group.Sum(item => item.Amount) })
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.Key)
            .ToList();

        var revenueByBillingCycle = payments
            .Where(x => x.Status == PaymentStatus.Completed)
            .GroupBy(x => x.Plan?.BillingCycle.ToString() ?? BillingCycle.Lifetime.ToString())
            .Select(group => new RevenueItem { Key = group.Key, Amount = group.Sum(item => item.Amount) })
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.Key)
            .ToList();

        var userStatusBreakdown = new List<StatCountItem>
        {
            new() { Key = UserStatus.Active.ToString(), Count = activeUsers },
            new() { Key = UserStatus.Pending.ToString(), Count = pendingUsers },
            new() { Key = UserStatus.Inactive.ToString(), Count = inactiveUsers },
            new() { Key = UserStatus.Suspended.ToString(), Count = suspendedUsers },
            new() { Key = UserStatus.Banned.ToString(), Count = bannedUsers }
        };

        var authProviderBreakdown = users
            .GroupBy(x => NormalizeGroupKey(x.AuthProvider))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var levelBreakdown = users
            .GroupBy(x => x.CurrentLevel.ToString())
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var countryBreakdown = users
            .Where(x => !string.IsNullOrWhiteSpace(x.Country))
            .GroupBy(x => NormalizeGroupKey(x.Country))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .Take(10)
            .ToList();

        var exerciseTypeBreakdown = exercises
            .GroupBy(x => x.ExerciseType.ToString())
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var lessonTypeBreakdown = lessons
            .GroupBy(x => NormalizeGroupKey(x.LessonType))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var lessonLevelBreakdown = lessons
            .GroupBy(x => NormalizeGroupKey(x.Level))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var lessonCategoryBreakdown = lessons
            .GroupBy(x => NormalizeTopicKey(x.Category))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var mediaTypeBreakdown = lessons
            .GroupBy(x => string.IsNullOrWhiteSpace(x.MediaType) ? "audio" : NormalizeGroupKey(x.MediaType))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var lessonStatusBreakdown = lessons
            .GroupBy(x => NormalizeStatus(x.Status))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var blogStatusBreakdown = blogs
            .GroupBy(x => NormalizeStatus(x.Status))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var notificationTypeBreakdown = notifications
            .GroupBy(x => NormalizeGroupKey(x.Type))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var notificationChannelBreakdown = notifications
            .GroupBy(x => NormalizeGroupKey(x.Channel))
            .Select(group => new StatCountItem { Key = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .ToList();

        var alerts = BuildAlerts(
            draftLessons,
            overdueVocabulary,
            expiringSoonSubscriptions,
            pendingPayments,
            users.Count(x => !x.IsEmailVerified),
            lessons.Count(x => NormalizeStatus(x.Status) == "published" && x.CompletionsCount > 0 && x.AvgScore < 60),
            totalNotifications - unreadNotifications);

        var lessonAccessResult = await _lessonAccessAnalyticsService.ExecuteAsync(cancellationToken);
        var lessonAccessStats = lessonAccessResult.IsSuccess ? lessonAccessResult.Value : new LessonAccessAnalyticsResponse();

        return Result.Success(new AdminAnalyticsResponse
        {
            GeneratedAt = now,
            Summary = new AdminSummaryCards
            {
                TotalUsers = users.Count,
                ActiveUsers = activeUsers,
                NewUsersToday = newUsersToday,
                TotalLessons = totalLessons,
                PublishedLessons = publishedLessons,
                TotalExercises = totalExercises,
                TotalVocabulary = totalVocabulary,
                DueVocabulary = dueVocabulary,
                ActiveSubscriptions = activeSubscriptions,
                ExpiringSubscriptionsSoon = expiringSoonSubscriptions,
                TotalRevenue = totalRevenue,
                PendingPayments = pendingPayments,
                TotalBlogs = totalBlogs
            },
            Users = new UserAnalyticsStats
            {
                TotalUsers = users.Count,
                ActiveUsers = activeUsers,
                PendingUsers = pendingUsers,
                InactiveUsers = inactiveUsers,
                SuspendedUsers = suspendedUsers,
                BannedUsers = bannedUsers,
                VerifiedUsers = verifiedUsers,
                NewUsersToday = newUsersToday,
                NewUsersThisMonth = newUsersThisMonth,
                DailyActiveUsers = dau,
                MonthlyActiveUsers = mau,
                UsersActiveInLast7Days = usersLast7Days,
                ByStatus = userStatusBreakdown,
                ByAuthProvider = authProviderBreakdown,
                ByLevel = levelBreakdown,
                ByCountry = countryBreakdown
            },
            Lessons = new LessonAnalyticsStats
            {
                TotalLessons = totalLessons,
                PublishedLessons = publishedLessons,
                DraftLessons = draftLessons,
                ArchivedLessons = archivedLessons,
                DictationLessons = dictationLessons,
                ShadowingLessons = shadowingLessons,
                PremiumLessons = premiumLessons,
                AudioLessons = audioLessons,
                YoutubeLessons = youtubeLessons,
                TotalCompletions = totalLessonCompletions,
                AverageScore = averageLessonScore,
                ByStatus = lessonStatusBreakdown,
                ByType = lessonTypeBreakdown,
                ByLevel = lessonLevelBreakdown,
                ByCategory = lessonCategoryBreakdown,
                ByMediaType = mediaTypeBreakdown,
                AccessStats = lessonAccessStats,
                PopularLessons = popularLessons,
                DifficultLessons = difficultLessons,
                RecentLessons = recentLessons
            },
            Exercises = new ExerciseAnalyticsStats
            {
                TotalExercises = totalExercises,
                DictationExercises = dictationExercises,
                ShadowingExercises = shadowingExercises,
                AverageScore = (decimal)averageExerciseScore,
                HighestScore = highestExerciseScore,
                PerfectScores = perfectScoresCount,
                AverageTimeSpentSeconds = (decimal)Math.Round(averageExerciseTimeSpent, 1),
                ExercisesToday = exercisesToday,
                ExercisesThisMonth = exercisesThisMonth,
                ByType = exerciseTypeBreakdown
            },
            Vocabulary = new VocabularyAnalyticsStats
            {
                TotalVocabulary = totalVocabulary,
                DueVocabulary = dueVocabulary,
                OverdueVocabulary = overdueVocabulary,
                NewVocabulary = newVocabulary,
                MasteredVocabulary = masteredVocabulary,
                LearningVocabulary = learningVocabulary,
                RecentVocabulary = vocabularyRecentCount,
                VocabularyLessons = vocabularyLessons,
                VocabularyTopics = vocabularyTopics,
                TopTopics = topVocabularyTopics,
                TopLessons = topVocabularyLessons,
                RecentItems = vocabularies
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(10)
                    .Select(x => new VocabularySummaryItem
                    {
                        VocabularyId = x.Id,
                        LessonId = x.LessonId,
                        LessonTitle = x.Lesson?.Title ?? string.Empty,
                        Topic = x.Topic,
                        Word = x.Word,
                        ReviewStatus = x.IsMastered ? "mastered" : x.ReviewCount == 0 ? "new" : (x.NextReviewAt.HasValue && x.NextReviewAt.Value < now ? "overdue" : (x.NextReviewAt.HasValue && x.NextReviewAt.Value <= now ? "due" : "learning")),
                        NextReviewAt = x.NextReviewAt,
                        CreatedAt = x.CreatedAt
                    })
                    .ToList()
            },
            Subscriptions = new SubscriptionAnalyticsStats
            {
                TotalPlans = totalPlans,
                ActivePlans = activePlans,
                FreePlans = freePlans,
                BasicPlans = basicPlans,
                PremiumPlans = premiumPlans,
                LifetimePlans = lifetimePlans,
                TotalSubscriptions = totalSubscriptions,
                ActiveSubscriptions = activeSubscriptions,
                PendingSubscriptions = pendingSubscriptions,
                ExpiredSubscriptions = expiredSubscriptions,
                CancelledSubscriptions = cancelledSubscriptions,
                AutoRenewSubscriptions = autoRenewSubscriptions,
                ExpiringSoonSubscriptions = expiringSoonSubscriptions,
                ByStatus = subscriptionStatuses,
                ByTier = planTierBreakdown,
                ByBillingCycle = billingCycleBreakdown
            },
            Payments = new PaymentAnalyticsStats
            {
                TotalPayments = totalPayments,
                CompletedPayments = completedPayments,
                PendingPayments = pendingPayments,
                FailedPayments = failedPayments,
                RefundedPayments = refundedPayments,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                MonthlyRevenue = monthRevenue,
                AveragePaymentAmount = Math.Round(averagePaymentAmount, 1),
                PaymentsToday = payments.Count(x => x.CreatedAt >= today),
                PaymentsThisMonth = payments.Count(x => x.CreatedAt >= monthStart),
                ByStatus = paymentStatuses,
                ByMethod = paymentMethods,
                RevenueByTier = revenueByTier,
                RevenueByBillingCycle = revenueByBillingCycle,
                RecentPayments = recentPayments
            },
            Blogs = new BlogAnalyticsStats
            {
                TotalBlogs = totalBlogs,
                PublishedBlogs = publishedBlogs,
                DraftBlogs = draftBlogs,
                ArchivedBlogs = archivedBlogs,
                TotalViews = totalBlogViews,
                AverageViews = (decimal)averageBlogViews,
                ByStatus = blogStatusBreakdown,
                PopularBlogs = popularBlogs,
                RecentBlogs = recentBlogs
            },
            Notifications = new NotificationAnalyticsStats
            {
                TotalNotifications = totalNotifications,
                UnreadNotifications = unreadNotifications,
                ReadNotifications = readNotifications,
                NotificationsToday = notificationsToday,
                NotificationsThisMonth = notificationsThisMonth,
                ByType = notificationTypeBreakdown,
                ByChannel = notificationChannelBreakdown
            },
            Engagement = new EngagementAnalyticsStats
            {
                UsersWithProgress = usersWithProgress,
                UsersWithStreak = usersWithStreak,
                UsersWithAnalytics = analyticsProfiles.Count,
                AveragePoints = (decimal)avgPoints,
                AverageMinutes = (decimal)avgMinutes,
                AverageLessonsCompleted = (decimal)avgLessonsCompleted,
                AverageDictationScore = (decimal)avgDictationProgressScore,
                AverageShadowingScore = (decimal)avgShadowingProgressScore,
                AverageCurrentStreak = (decimal)avgCurrentStreak,
                LongestStreak = longestStreak,
                TopUsers = topUsers
            },
            Alerts = alerts,
            TopUsers = topUsers,
            PopularLessons = popularLessons,
            DifficultLessons = difficultLessons,
            RecentLessons = recentLessons,
            RecentPayments = recentPayments
        });
    }

    private static List<AdminAlertItem> BuildAlerts(
        int draftLessons,
        int overdueVocabulary,
        int expiringSoonSubscriptions,
        int pendingPayments,
        int unverifiedUsers,
        int lowScoreLessons,
        int unreadNotifications)
    {
        var alerts = new List<AdminAlertItem>();

        if (draftLessons > 0)
        {
            alerts.Add(new AdminAlertItem
            {
                Code = "draft_lessons",
                Title = "Bài học nháp cần duyệt",
                Message = "Các bài học draft chưa được xuất bản.",
                Count = draftLessons,
                Severity = "warning"
            });
        }

        if (overdueVocabulary > 0)
        {
            alerts.Add(new AdminAlertItem
            {
                Code = "overdue_vocabulary",
                Title = "Từ vựng quá hạn ôn",
                Message = "Người học đang có từ vựng đến hạn hoặc trễ lịch ôn.",
                Count = overdueVocabulary,
                Severity = "info"
            });
        }

        if (expiringSoonSubscriptions > 0)
        {
            alerts.Add(new AdminAlertItem
            {
                Code = "expiring_subscriptions",
                Title = "Subscription sắp hết hạn",
                Message = "Có subscription active sẽ hết hạn trong 30 ngày.",
                Count = expiringSoonSubscriptions,
                Severity = "warning"
            });
        }

        if (pendingPayments > 0)
        {
            alerts.Add(new AdminAlertItem
            {
                Code = "pending_payments",
                Title = "Thanh toán đang chờ xử lý",
                Message = "Có giao dịch chưa hoàn tất cần kiểm tra.",
                Count = pendingPayments,
                Severity = "warning"
            });
        }

        if (unverifiedUsers > 0)
        {
            alerts.Add(new AdminAlertItem
            {
                Code = "unverified_users",
                Title = "Người dùng chưa xác thực email",
                Message = "Tài khoản chưa xác minh email có thể ảnh hưởng onboarding.",
                Count = unverifiedUsers,
                Severity = "info"
            });
        }

        if (lowScoreLessons > 0)
        {
            alerts.Add(new AdminAlertItem
            {
                Code = "low_score_lessons",
                Title = "Bài học có điểm trung bình thấp",
                Message = "Các bài học published nhưng điểm trung bình còn thấp.",
                Count = lowScoreLessons,
                Severity = "critical"
            });
        }

        if (unreadNotifications > 0)
        {
            alerts.Add(new AdminAlertItem
            {
                Code = "unread_notifications",
                Title = "Thông báo chưa đọc",
                Message = "Có thông báo trong inbox người dùng chưa được đọc.",
                Count = unreadNotifications,
                Severity = "info"
            });
        }

        return alerts;
    }

    private static string NormalizeGroupKey(string? value, string fallback = "Unknown")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeTopicKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Uncategorized" : value.Trim();
    }

    private static string NormalizeStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();
    }
}

