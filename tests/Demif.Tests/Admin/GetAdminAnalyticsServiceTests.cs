using Demif.Application.Features.Admin.Analytics;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Demif.Tests.Admin;

public class GetAdminAnalyticsServiceTests
{
    private static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_WithSeededData_ReturnsComprehensiveMetrics()
    {
        var context = CreateDbContext();
        var now = DateTime.UtcNow;
        var today = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "learner.one",
            Email = "learner1@example.com",
            Status = UserStatus.Active,
            IsEmailVerified = true,
            AuthProvider = "email",
            Country = "Vietnam",
            CurrentLevel = Level.Intermediate,
            CreatedAt = monthStart.AddDays(1),
            LastLoginAt = today.AddHours(10)
        };

        var pendingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "learner.two",
            Email = "learner2@example.com",
            Status = UserStatus.Pending,
            IsEmailVerified = false,
            AuthProvider = "google",
            Country = "Vietnam",
            CurrentLevel = Level.Beginner,
            CreatedAt = today,
            LastLoginAt = today.AddHours(12)
        };

        var suspendedUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "learner.three",
            Email = "learner3@example.com",
            Status = UserStatus.Suspended,
            IsEmailVerified = true,
            AuthProvider = "email",
            Country = "United States",
            CurrentLevel = Level.Advanced,
            CreatedAt = monthStart,
            LastLoginAt = today.AddDays(-1)
        };

        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin", IsDefault = false, IsActive = true };
        var userRole = new Role { Id = Guid.NewGuid(), Name = "User", IsDefault = true, IsActive = true };

        var lesson1 = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Travel Basics",
            LessonType = "Dictation",
            Level = "Beginner",
            Category = "travel",
            MediaType = "audio",
            Status = "published",
            CompletionsCount = 20,
            AvgScore = 80,
            CreatedAt = monthStart.AddDays(2)
        };

        var lesson2 = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Business Fluency",
            LessonType = "Shadowing",
            Level = "Advanced",
            Category = "business",
            MediaType = "youtube",
            Status = "published",
            CompletionsCount = 8,
            AvgScore = 55,
            CreatedAt = monthStart.AddDays(3)
        };

        var lesson3 = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Draft Lesson",
            LessonType = "Dictation",
            Level = "Intermediate",
            Category = "academic",
            MediaType = "audio",
            Status = "draft",
            CompletionsCount = 0,
            AvgScore = 0,
            CreatedAt = today
        };

        var lesson4 = new Lesson
        {
            Id = Guid.NewGuid(),
            Title = "Archived Lesson",
            LessonType = "Dictation",
            Level = "Beginner",
            Category = "travel",
            MediaType = "audio",
            Status = "archived",
            CompletionsCount = 2,
            AvgScore = 40,
            CreatedAt = monthStart
        };

        var exercise1 = new UserExercise
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            LessonId = lesson1.Id,
            User = activeUser,
            Lesson = lesson1,
            ExerciseType = ExerciseType.Dictation,
            Score = 90,
            TimeSpentSeconds = 120,
            Attempts = 1,
            PlaysUsed = 1,
            CompletedAt = today.AddHours(10)
        };

        var exercise2 = new UserExercise
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            LessonId = lesson2.Id,
            User = activeUser,
            Lesson = lesson2,
            ExerciseType = ExerciseType.Shadowing,
            Score = 100,
            TimeSpentSeconds = 180,
            Attempts = 1,
            PlaysUsed = 1,
            CompletedAt = today.AddHours(11)
        };

        var exercise3 = new UserExercise
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUser.Id,
            LessonId = lesson1.Id,
            User = suspendedUser,
            Lesson = lesson1,
            ExerciseType = ExerciseType.Dictation,
            Score = 70,
            TimeSpentSeconds = 90,
            Attempts = 2,
            PlaysUsed = 1,
            CompletedAt = monthStart.AddDays(2)
        };

        var planFree = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Free", Tier = SubscriptionTier.Free, BillingCycle = BillingCycle.Monthly, Price = 0, IsActive = true };
        var planBasic = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Basic Monthly", Tier = SubscriptionTier.Basic, BillingCycle = BillingCycle.Monthly, Price = 100000, IsActive = true };
        var planPremium = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Premium Monthly", Tier = SubscriptionTier.Premium, BillingCycle = BillingCycle.Monthly, Price = 200000, IsActive = true };
        var planLifetime = new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Premium Lifetime", Tier = SubscriptionTier.Premium, BillingCycle = BillingCycle.Lifetime, Price = 500000, IsActive = true };

        var activeSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            PlanId = planPremium.Id,
            StartDate = monthStart,
            EndDate = now.AddDays(10),
            Status = SubscriptionStatus.Active,
            AutoRenew = true,
            Plan = planPremium,
            User = activeUser
        };

        var pendingSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = pendingUser.Id,
            PlanId = planBasic.Id,
            StartDate = monthStart,
            EndDate = now.AddDays(20),
            Status = SubscriptionStatus.PendingPayment,
            AutoRenew = false,
            Plan = planBasic,
            User = pendingUser
        };

        var expiredSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUser.Id,
            PlanId = planPremium.Id,
            StartDate = monthStart.AddMonths(-1),
            EndDate = now.AddDays(-1),
            Status = SubscriptionStatus.Expired,
            AutoRenew = false,
            Plan = planPremium,
            User = suspendedUser
        };

        var cancelledSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            PlanId = planLifetime.Id,
            StartDate = monthStart,
            EndDate = null,
            Status = SubscriptionStatus.Cancelled,
            AutoRenew = false,
            Plan = planLifetime,
            User = activeUser
        };

        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            PlanId = planPremium.Id,
            SubscriptionId = activeSubscription.Id,
            Amount = 200000,
            Currency = "VND",
            PaymentMethod = "sepay_bank",
            PaymentReference = "PAY-001",
            Status = PaymentStatus.Completed,
            CompletedAt = today.AddHours(-2),
            CreatedAt = today.AddHours(-2),
            Plan = planPremium,
            Subscription = activeSubscription,
            User = activeUser
        };

        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUser.Id,
            PlanId = planBasic.Id,
            SubscriptionId = pendingSubscription.Id,
            Amount = 100000,
            Currency = "VND",
            PaymentMethod = "momo",
            PaymentReference = "PAY-002",
            Status = PaymentStatus.Completed,
            CompletedAt = monthStart.AddDays(2),
            CreatedAt = monthStart.AddDays(2),
            Plan = planBasic,
            Subscription = pendingSubscription,
            User = suspendedUser
        };

        var pendingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = pendingUser.Id,
            PlanId = planBasic.Id,
            SubscriptionId = pendingSubscription.Id,
            Amount = 100000,
            Currency = "VND",
            PaymentMethod = "zalopay",
            PaymentReference = "PAY-003",
            Status = PaymentStatus.Pending,
            CreatedAt = today,
            Plan = planBasic,
            Subscription = pendingSubscription,
            User = pendingUser
        };

        var blog1 = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "Learning Tips",
            Content = "content",
            Status = "published",
            ViewCount = 150,
            AuthorId = activeUser.Id,
            CreatedAt = monthStart.AddDays(1),
            Author = activeUser
        };

        var blog2 = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "Draft Blog",
            Content = "draft",
            Status = "draft",
            ViewCount = 20,
            AuthorId = suspendedUser.Id,
            CreatedAt = today,
            Author = suspendedUser
        };

        var notification1 = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            Type = "welcome",
            Title = "Welcome",
            Message = "Welcome to DEMIF",
            Channel = "email",
            IsRead = false,
            CreatedAt = today,
            User = activeUser
        };

        var notification2 = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = pendingUser.Id,
            Type = "reminder",
            Title = "Reminder",
            Message = "Keep practicing",
            Channel = "push",
            IsRead = true,
            ReadAt = today,
            CreatedAt = today,
            User = pendingUser
        };

        var notification3 = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUser.Id,
            Type = "system_announcement",
            Title = "System",
            Message = "Maintenance",
            Channel = "in_app",
            IsRead = false,
            CreatedAt = monthStart,
            User = suspendedUser
        };

        var vocabularyDue = new UserVocabulary
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            LessonId = lesson1.Id,
            Lesson = lesson1,
            Topic = "travel",
            Word = "airport",
            NormalizedWord = "airport",
            ReviewCount = 2,
            CorrectReviews = 2,
            NextReviewAt = now.AddHours(-1),
            CreatedAt = monthStart.AddDays(1)
        };

        var vocabularyOverdue = new UserVocabulary
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            LessonId = lesson1.Id,
            Lesson = lesson1,
            Topic = "travel",
            Word = "hotel",
            NormalizedWord = "hotel",
            ReviewCount = 1,
            CorrectReviews = 1,
            NextReviewAt = now.AddDays(-2),
            CreatedAt = monthStart.AddDays(2)
        };

        var vocabularyMastered = new UserVocabulary
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            LessonId = lesson2.Id,
            Lesson = lesson2,
            Topic = "business",
            Word = "revenue",
            NormalizedWord = "revenue",
            ReviewCount = 3,
            CorrectReviews = 3,
            IsMastered = true,
            NextReviewAt = now.AddDays(5),
            MasteredAt = today,
            CreatedAt = monthStart.AddDays(3)
        };

        var vocabularyNew = new UserVocabulary
        {
            Id = Guid.NewGuid(),
            UserId = pendingUser.Id,
            LessonId = lesson3.Id,
            Lesson = lesson3,
            Topic = "academic",
            Word = "analysis",
            NormalizedWord = "analysis",
            ReviewCount = 0,
            CorrectReviews = 0,
            NextReviewAt = now.AddDays(3),
            CreatedAt = today
        };

        var progress1 = new UserProgress
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            TotalPoints = 800,
            TotalMinutes = 120,
            LessonsCompleted = 10,
            DictationCompleted = 8,
            ShadowingCompleted = 2,
            AvgDictationScore = 82.5m,
            AvgShadowingScore = 70m,
            CurrentLevel = Level.Intermediate,
            LevelProgress = 40,
            UpdatedAt = now,
            User = activeUser
        };

        var progress2 = new UserProgress
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUser.Id,
            TotalPoints = 300,
            TotalMinutes = 45,
            LessonsCompleted = 4,
            DictationCompleted = 3,
            ShadowingCompleted = 1,
            AvgDictationScore = 65m,
            AvgShadowingScore = 60m,
            CurrentLevel = Level.Advanced,
            LevelProgress = 20,
            UpdatedAt = now,
            User = suspendedUser
        };

        var streak1 = new UserStreak
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            CurrentStreak = 7,
            LongestStreak = 12,
            LastActiveDate = today,
            TotalActiveDays = 40,
            FreezeCount = 1,
            FreezesAvailable = 1,
            UpdatedAt = now,
            User = activeUser
        };

        var streak2 = new UserStreak
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUser.Id,
            CurrentStreak = 2,
            LongestStreak = 5,
            LastActiveDate = today.AddDays(-1),
            TotalActiveDays = 18,
            FreezeCount = 0,
            FreezesAvailable = 1,
            UpdatedAt = now,
            User = suspendedUser
        };

        var tracker1 = new UserLessonTracker
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            LessonId = lesson1.Id,
            Status = LessonProgressStatus.Completed,
            LastSegmentIndex = 4,
            StartedAt = monthStart.AddDays(2),
            CompletedAt = today.AddHours(9),
            User = activeUser,
            Lesson = lesson1
        };

        var tracker2 = new UserLessonTracker
        {
            Id = Guid.NewGuid(),
            UserId = pendingUser.Id,
            LessonId = lesson1.Id,
            Status = LessonProgressStatus.InProgress,
            LastSegmentIndex = 2,
            StartedAt = today.AddHours(8),
            User = pendingUser,
            Lesson = lesson1
        };

        var tracker3 = new UserLessonTracker
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUser.Id,
            LessonId = lesson2.Id,
            Status = LessonProgressStatus.Started,
            LastSegmentIndex = 0,
            StartedAt = monthStart.AddDays(3),
            User = suspendedUser,
            Lesson = lesson2
        };

        var analytics1 = new UserAnalytics
        {
            Id = Guid.NewGuid(),
            UserId = activeUser.Id,
            TotalExercisesCompleted = 12,
            TotalLessonsCompleted = 10,
            TotalLearningMinutes = 120,
            TotalPoints = 800,
            AvgDictationScore = 82.5m,
            AvgShadowingScore = 70m,
            HighestScore = 100,
            PerfectScoresCount = 2,
            FirstActivityDate = monthStart,
            LastActivityDate = today,
            TotalActiveDays = 40,
            CurrentStreak = 7,
            LongestStreak = 12,
            AvgSessionsPerWeek = 4.5m,
            CurrentSubscriptionTier = "Premium",
            SubscriptionEndDate = now.AddDays(10),
            TotalAmountPaid = 200000,
            SuccessfulPaymentsCount = 1,
            LastPaymentDate = today.AddHours(-2),
            TotalLogins = 18,
            LastLoginDate = today.AddHours(10),
            BlogViewsCount = 5,
            EngagementScore = 95,
            UpdatedAt = now
        };

        var analytics2 = new UserAnalytics
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUser.Id,
            TotalExercisesCompleted = 5,
            TotalLessonsCompleted = 4,
            TotalLearningMinutes = 45,
            TotalPoints = 300,
            AvgDictationScore = 65m,
            AvgShadowingScore = 60m,
            HighestScore = 88,
            PerfectScoresCount = 0,
            FirstActivityDate = monthStart,
            LastActivityDate = today.AddDays(-1),
            TotalActiveDays = 18,
            CurrentStreak = 2,
            LongestStreak = 5,
            AvgSessionsPerWeek = 2m,
            CurrentSubscriptionTier = "Free",
            TotalAmountPaid = 100000,
            SuccessfulPaymentsCount = 1,
            LastPaymentDate = monthStart.AddDays(2),
            TotalLogins = 8,
            LastLoginDate = today.AddDays(-1),
            BlogViewsCount = 2,
            EngagementScore = 42,
            UpdatedAt = now
        };

        context.Users.AddRange(activeUser, pendingUser, suspendedUser);
        context.Roles.AddRange(adminRole, userRole);
        context.UserRoles.AddRange(
            new UserRole { UserId = activeUser.Id, RoleId = adminRole.Id, User = activeUser, Role = adminRole },
            new UserRole { UserId = pendingUser.Id, RoleId = userRole.Id, User = pendingUser, Role = userRole },
            new UserRole { UserId = suspendedUser.Id, RoleId = userRole.Id, User = suspendedUser, Role = userRole });

        context.Lessons.AddRange(lesson1, lesson2, lesson3, lesson4);
        context.UserExercises.AddRange(exercise1, exercise2, exercise3);
        context.SubscriptionPlans.AddRange(planFree, planBasic, planPremium, planLifetime);
        context.UserSubscriptions.AddRange(activeSubscription, pendingSubscription, expiredSubscription, cancelledSubscription);
        context.Payments.AddRange(payment1, payment2, pendingPayment);
        context.Blogs.AddRange(blog1, blog2);
        context.UserNotifications.AddRange(notification1, notification2, notification3);
        context.UserVocabularies.AddRange(vocabularyDue, vocabularyOverdue, vocabularyMastered, vocabularyNew);
        context.UserProgresses.AddRange(progress1, progress2);
        context.UserStreaks.AddRange(streak1, streak2);
        context.UserLessonTrackers.AddRange(tracker1, tracker2, tracker3);
        context.LessonAccessEvents.AddRange(
            new LessonAccessEvent
            {
                Id = Guid.NewGuid(),
                UserId = activeUser.Id,
                LessonId = lesson1.Id,
                AccessType = "detail",
                AccessedAt = monthStart.AddDays(2).AddHours(1)
            },
            new LessonAccessEvent
            {
                Id = Guid.NewGuid(),
                UserId = pendingUser.Id,
                LessonId = lesson1.Id,
                AccessType = "segments",
                AccessedAt = monthStart.AddDays(2).AddHours(2)
            },
            new LessonAccessEvent
            {
                Id = Guid.NewGuid(),
                UserId = suspendedUser.Id,
                LessonId = lesson2.Id,
                AccessType = "detail",
                AccessedAt = monthStart.AddDays(3).AddHours(1)
            });
        context.UserAnalytics.AddRange(analytics1, analytics2);

        await context.SaveChangesAsync();

        var service = new GetAdminAnalyticsService(context);
        var result = await service.ExecuteAsync();

        Assert.True(result.IsSuccess);

        var analytics = result.Value;

        Assert.Equal(3, analytics.Summary.TotalUsers);
        Assert.Equal(1, analytics.Summary.ActiveUsers);
        Assert.Equal(1, analytics.Summary.NewUsersToday);
        Assert.Equal(4, analytics.Summary.TotalLessons);
        Assert.Equal(2, analytics.Summary.PublishedLessons);
        Assert.Equal(4, analytics.Summary.TotalVocabulary);
        Assert.Equal(2, analytics.Summary.DueVocabulary);
        Assert.Equal(1, analytics.Summary.ActiveSubscriptions);
        Assert.Equal(1, analytics.Summary.ExpiringSubscriptionsSoon);
        Assert.Equal(300000m, analytics.Summary.TotalRevenue);
        Assert.Equal(1, analytics.Summary.PendingPayments);
        Assert.Equal(2, analytics.Summary.TotalBlogs);

        Assert.Equal(1, analytics.Users.ByStatus.Single(x => x.Key == UserStatus.Active.ToString()).Count);
        Assert.Equal(1, analytics.Users.ByStatus.Single(x => x.Key == UserStatus.Pending.ToString()).Count);
        Assert.Equal(1, analytics.Users.ByStatus.Single(x => x.Key == UserStatus.Suspended.ToString()).Count);
        Assert.Equal(2, analytics.Users.DailyActiveUsers);
        Assert.Equal(3, analytics.Users.MonthlyActiveUsers);

        Assert.Equal(1, analytics.Lessons.ByStatus.Single(x => x.Key == "draft").Count);
        Assert.Equal(1, analytics.Lessons.ByStatus.Single(x => x.Key == "archived").Count);
        Assert.Equal(3, analytics.Lessons.ByType.Single(x => x.Key == "Dictation").Count);
        Assert.Equal(1, analytics.Lessons.ByType.Single(x => x.Key == "Shadowing").Count);
        Assert.Equal(lesson2.Id, analytics.Lessons.DifficultLessons.First().LessonId);
        Assert.Equal(3, analytics.Lessons.AccessStats.TotalAccessEvents);
        Assert.Equal(2, analytics.Lessons.AccessStats.TotalTrackedLessons);
        Assert.Equal(1, analytics.Lessons.AccessStats.CompletedTrackers);
        Assert.Equal(1, analytics.Lessons.AccessStats.InProgressTrackers);
        Assert.Equal(1, analytics.Lessons.AccessStats.StartedTrackers);
        Assert.Equal(2, analytics.Lessons.AccessStats.TopAccessedLessons.First().AccessCount);

        Assert.Equal(3, analytics.Exercises.TotalExercises);
        Assert.Equal(100, analytics.Exercises.HighestScore);
        Assert.Equal(2, analytics.Exercises.ByType.Single(x => x.Key == ExerciseType.Dictation.ToString()).Count);
        Assert.Equal(1, analytics.Exercises.ByType.Single(x => x.Key == ExerciseType.Shadowing.ToString()).Count);

        Assert.Equal(2, analytics.Vocabulary.TopTopics.Single(x => x.Key == "travel").Count);
        Assert.Equal(2, analytics.Vocabulary.TopLessons.First(x => x.LessonId == lesson1.Id).CompletionsCount);
        Assert.Contains(analytics.Vocabulary.RecentItems, item => item.ReviewStatus == "mastered");

        Assert.Equal(4, analytics.Subscriptions.TotalSubscriptions);
        Assert.Equal(1, analytics.Subscriptions.ByStatus.Single(x => x.Key == SubscriptionStatus.Active.ToString()).Count);
        Assert.Equal(3, analytics.Subscriptions.ByTier.Single(x => x.Key == SubscriptionTier.Premium.ToString()).Count);
        Assert.Equal(1, analytics.Subscriptions.ByBillingCycle.Single(x => x.Key == BillingCycle.Lifetime.ToString()).Count);

        Assert.Equal(3, analytics.Payments.TotalPayments);
        Assert.Equal(2, analytics.Payments.CompletedPayments);
        Assert.Equal(300000m, analytics.Payments.TotalRevenue);
        Assert.Equal(200000m, analytics.Payments.RevenueByTier.Single(x => x.Key == SubscriptionTier.Premium.ToString()).Amount);
        Assert.Equal(100000m, analytics.Payments.RevenueByTier.Single(x => x.Key == SubscriptionTier.Basic.ToString()).Amount);
        Assert.Equal(3, analytics.Payments.ByMethod.Count);

        Assert.Equal(2, analytics.Blogs.TotalBlogs);
        Assert.Equal(1, analytics.Blogs.ByStatus.Single(x => x.Key == "published").Count);
        Assert.Equal(1, analytics.Blogs.ByStatus.Single(x => x.Key == "draft").Count);

        Assert.Equal(3, analytics.Notifications.TotalNotifications);
        Assert.Equal(2, analytics.Notifications.UnreadNotifications);
        Assert.Equal(1, analytics.Notifications.ByChannel.Single(x => x.Key == "email").Count);

        Assert.Equal(2, analytics.Engagement.UsersWithAnalytics);
        Assert.Equal(95, analytics.TopUsers.First().EngagementScore);
        Assert.Equal("learner.one", analytics.TopUsers.First().Username);

        Assert.Contains(analytics.Alerts, alert => alert.Code == "draft_lessons");
        Assert.Contains(analytics.Alerts, alert => alert.Code == "overdue_vocabulary");
        Assert.Contains(analytics.Alerts, alert => alert.Code == "expiring_subscriptions");
        Assert.Contains(analytics.Alerts, alert => alert.Code == "pending_payments");
        Assert.Contains(analytics.Alerts, alert => alert.Code == "unverified_users");
        Assert.Contains(analytics.Alerts, alert => alert.Code == "low_score_lessons");
        Assert.Contains(analytics.Alerts, alert => alert.Code == "unread_notifications");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyDatabase_ReturnsZeros()
    {
        var context = CreateDbContext();
        var service = new GetAdminAnalyticsService(context);

        var result = await service.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Summary.TotalUsers);
        Assert.Equal(0, result.Value.Summary.TotalLessons);
        Assert.Equal(0, result.Value.Summary.TotalRevenue);
        Assert.Empty(result.Value.Alerts);
        Assert.Empty(result.Value.TopUsers);
        Assert.Empty(result.Value.Payments.RecentPayments);
    }
}