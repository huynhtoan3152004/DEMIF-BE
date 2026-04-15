using Demif.Domain.Enums;
using Demif.Application.Features.Admin.Analytics.Lessons.Access;

namespace Demif.Application.Features.Admin.Analytics;

public class AdminAnalyticsResponse
{
    public DateTime GeneratedAt { get; set; }
    public AdminSummaryCards Summary { get; set; } = new();
    public UserAnalyticsStats Users { get; set; } = new();
    public LessonAnalyticsStats Lessons { get; set; } = new();
    public ExerciseAnalyticsStats Exercises { get; set; } = new();
    public VocabularyAnalyticsStats Vocabulary { get; set; } = new();
    public SubscriptionAnalyticsStats Subscriptions { get; set; } = new();
    public PaymentAnalyticsStats Payments { get; set; } = new();
    public BlogAnalyticsStats Blogs { get; set; } = new();
    public NotificationAnalyticsStats Notifications { get; set; } = new();
    public EngagementAnalyticsStats Engagement { get; set; } = new();
    public List<AdminAlertItem> Alerts { get; set; } = new();
    public List<TopUserItem> TopUsers { get; set; } = new();
    public List<LessonSummaryItem> PopularLessons { get; set; } = new();
    public List<LessonSummaryItem> DifficultLessons { get; set; } = new();
    public List<LessonSummaryItem> RecentLessons { get; set; } = new();
    public List<PaymentSummaryItem> RecentPayments { get; set; } = new();
}

public class AdminOverviewResponse
{
    public DateTime GeneratedAt { get; set; }
    public AdminSummaryCards Summary { get; set; } = new();
    public List<AdminAlertItem> Alerts { get; set; } = new();
    public List<TopUserItem> TopUsers { get; set; } = new();
    public List<LessonSummaryItem> PopularLessons { get; set; } = new();
    public List<LessonSummaryItem> DifficultLessons { get; set; } = new();
    public List<PaymentSummaryItem> RecentPayments { get; set; } = new();
}

public class AdminContentAnalyticsResponse
{
    public BlogAnalyticsStats Blogs { get; set; } = new();
    public NotificationAnalyticsStats Notifications { get; set; } = new();
    public EngagementAnalyticsStats Engagement { get; set; } = new();
}

public class AdminSummaryCards
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int TotalLessons { get; set; }
    public int PublishedLessons { get; set; }
    public int TotalExercises { get; set; }
    public int TotalVocabulary { get; set; }
    public int DueVocabulary { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ExpiringSubscriptionsSoon { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingPayments { get; set; }
    public int TotalBlogs { get; set; }
}

public class UserAnalyticsStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int PendingUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int BannedUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int DailyActiveUsers { get; set; }
    public int MonthlyActiveUsers { get; set; }
    public int UsersActiveInLast7Days { get; set; }
    public List<StatCountItem> ByStatus { get; set; } = new();
    public List<StatCountItem> ByAuthProvider { get; set; } = new();
    public List<StatCountItem> ByLevel { get; set; } = new();
    public List<StatCountItem> ByCountry { get; set; } = new();
}

public class LessonAnalyticsStats
{
    public int TotalLessons { get; set; }
    public int PublishedLessons { get; set; }
    public int DraftLessons { get; set; }
    public int ArchivedLessons { get; set; }
    public int DictationLessons { get; set; }
    public int ShadowingLessons { get; set; }
    public int PremiumLessons { get; set; }
    public int AudioLessons { get; set; }
    public int YoutubeLessons { get; set; }
    public int TotalCompletions { get; set; }
    public decimal AverageScore { get; set; }
    public List<StatCountItem> ByStatus { get; set; } = new();
    public List<StatCountItem> ByType { get; set; } = new();
    public List<StatCountItem> ByLevel { get; set; } = new();
    public List<StatCountItem> ByCategory { get; set; } = new();
    public List<StatCountItem> ByMediaType { get; set; } = new();
    public LessonAccessAnalyticsResponse AccessStats { get; set; } = new();
    public List<LessonSummaryItem> PopularLessons { get; set; } = new();
    public List<LessonSummaryItem> DifficultLessons { get; set; } = new();
    public List<LessonSummaryItem> RecentLessons { get; set; } = new();
}

public class ExerciseAnalyticsStats
{
    public int TotalExercises { get; set; }
    public int DictationExercises { get; set; }
    public int ShadowingExercises { get; set; }
    public decimal AverageScore { get; set; }
    public int HighestScore { get; set; }
    public int PerfectScores { get; set; }
    public decimal AverageTimeSpentSeconds { get; set; }
    public int ExercisesToday { get; set; }
    public int ExercisesThisMonth { get; set; }
    public List<StatCountItem> ByType { get; set; } = new();
}

public class VocabularyAnalyticsStats
{
    public int TotalVocabulary { get; set; }
    public int DueVocabulary { get; set; }
    public int OverdueVocabulary { get; set; }
    public int NewVocabulary { get; set; }
    public int MasteredVocabulary { get; set; }
    public int LearningVocabulary { get; set; }
    public int RecentVocabulary { get; set; }
    public int VocabularyLessons { get; set; }
    public int VocabularyTopics { get; set; }
    public List<StatCountItem> TopTopics { get; set; } = new();
    public List<LessonSummaryItem> TopLessons { get; set; } = new();
    public List<VocabularySummaryItem> RecentItems { get; set; } = new();
}

public class SubscriptionAnalyticsStats
{
    public int TotalPlans { get; set; }
    public int ActivePlans { get; set; }
    public int FreePlans { get; set; }
    public int BasicPlans { get; set; }
    public int PremiumPlans { get; set; }
    public int LifetimePlans { get; set; }
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int PendingSubscriptions { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
    public int AutoRenewSubscriptions { get; set; }
    public int ExpiringSoonSubscriptions { get; set; }
    public List<StatCountItem> ByStatus { get; set; } = new();
    public List<StatCountItem> ByTier { get; set; } = new();
    public List<StatCountItem> ByBillingCycle { get; set; } = new();
}

public class PaymentAnalyticsStats
{
    public int TotalPayments { get; set; }
    public int CompletedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int FailedPayments { get; set; }
    public int RefundedPayments { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal AveragePaymentAmount { get; set; }
    public int PaymentsToday { get; set; }
    public int PaymentsThisMonth { get; set; }
    public List<StatCountItem> ByStatus { get; set; } = new();
    public List<StatCountItem> ByMethod { get; set; } = new();
    public List<RevenueItem> RevenueByTier { get; set; } = new();
    public List<RevenueItem> RevenueByBillingCycle { get; set; } = new();
    public List<PaymentSummaryItem> RecentPayments { get; set; } = new();
}

public class BlogAnalyticsStats
{
    public int TotalBlogs { get; set; }
    public int PublishedBlogs { get; set; }
    public int DraftBlogs { get; set; }
    public int ArchivedBlogs { get; set; }
    public int TotalViews { get; set; }
    public decimal AverageViews { get; set; }
    public List<StatCountItem> ByStatus { get; set; } = new();
    public List<BlogSummaryItem> PopularBlogs { get; set; } = new();
    public List<BlogSummaryItem> RecentBlogs { get; set; } = new();
}

public class NotificationAnalyticsStats
{
    public int TotalNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public int ReadNotifications { get; set; }
    public int NotificationsToday { get; set; }
    public int NotificationsThisMonth { get; set; }
    public List<StatCountItem> ByType { get; set; } = new();
    public List<StatCountItem> ByChannel { get; set; } = new();
}

public class EngagementAnalyticsStats
{
    public int UsersWithProgress { get; set; }
    public int UsersWithStreak { get; set; }
    public int UsersWithAnalytics { get; set; }
    public decimal AveragePoints { get; set; }
    public decimal AverageMinutes { get; set; }
    public decimal AverageLessonsCompleted { get; set; }
    public decimal AverageDictationScore { get; set; }
    public decimal AverageShadowingScore { get; set; }
    public decimal AverageCurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public List<TopUserItem> TopUsers { get; set; } = new();
}

public class AdminAlertItem
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Severity { get; set; } = string.Empty;
}

public class StatCountItem
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RevenueItem
{
    public string Key { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TopUserItem
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int TotalMinutes { get; set; }
    public int EngagementScore { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int LessonsCompleted { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class LessonSummaryItem
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LessonType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal AvgScore { get; set; }
    public int CompletionsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? AuthorId { get; set; }
}

public class PaymentSummaryItem
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string PlanTier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BlogSummaryItem
{
    public Guid BlogId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid AuthorId { get; set; }
}

public class VocabularySummaryItem
{
    public Guid VocabularyId { get; set; }
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public DateTime? NextReviewAt { get; set; }
    public DateTime CreatedAt { get; set; }
}