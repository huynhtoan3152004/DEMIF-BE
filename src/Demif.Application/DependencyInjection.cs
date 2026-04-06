using Demif.Application.Features.Auth.ChangePassword;
using Demif.Application.Features.Auth.Login;
using Demif.Application.Features.Auth.Logout;
using Demif.Application.Features.Auth.GoogleLogin;
using Demif.Application.Features.Auth.VerifyEmail;
using Demif.Application.Features.Auth.RefreshToken;
using Demif.Application.Features.Auth.Register;
using Demif.Application.Features.Lessons.Admin;
using Demif.Application.Features.Lessons.CheckSegment;
using Demif.Application.Features.Lessons.CheckShadowing;
using Demif.Application.Features.Lessons.GetDictationExercise;
using Demif.Application.Features.Lessons.GetLessonById;
using Demif.Application.Features.Lessons.GetLessonSegments;
using Demif.Application.Features.Lessons.SubmitDictation;
using Demif.Application.Features.Lessons.GetLessons;
using Demif.Application.Features.Payments.GetHistory;
using Demif.Application.Features.Payments.GetInfo;
using Demif.Application.Features.Payments.GetStatus;
using Demif.Application.Features.Payments.Webhook;
using Demif.Application.Features.Profile.GetMyProfile;
using Demif.Application.Features.Profile.UpdateMyProfile;
using Demif.Application.Features.Subscriptions.Admin;
using Demif.Application.Features.Subscriptions.CancelSubscription;
using Demif.Application.Features.Subscriptions.GetMySubscription;
using Demif.Application.Features.Subscriptions.GetPlans;
using Demif.Application.Features.Subscriptions.Subscribe;
using Demif.Application.Features.Users.AssignRole;
using Demif.Application.Features.Users.CreateUser;
using Demif.Application.Features.Users.DeleteUser;
using Demif.Application.Features.Users.GetUserById;
using Demif.Application.Features.Users.GetUsers;
using Demif.Application.Features.Users.RemoveRole;
using Demif.Application.Features.Users.UpdateUser;
using Demif.Application.Features.Users.UpdateUserStatus;
using Demif.Application.Features.Blogs.CreateBlog;
using Demif.Application.Features.Blogs.GetBlogs;
using Demif.Application.Features.Blogs.GetBlogById;
using Demif.Application.Features.Blogs.UpdateBlog;
using Demif.Application.Features.Blogs.DeleteBlog;
using Demif.Application.Features.Me.GetProgress;
using Demif.Application.Features.Me.GetStreak;
using Demif.Application.Features.Me.RecordActivity;
using Demif.Application.Features.Me.GetUserAnalytics;
using Demif.Application.Features.Me.Stats;
using Demif.Application.Features.Admin.UserSubscriptions;
using Demif.Application.Features.Admin.Payments;
using Demif.Application.Features.Subscriptions.ExpiryCleanup;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Demif.Application;

/// <summary>
/// Đăng ký DI cho Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all validators from this assembly
        services.AddValidatorsFromAssemblyContaining<LoginValidator>();

        // Auth Services
        services.AddScoped<LoginService>();
        services.AddScoped<GoogleLoginService>();
        services.AddScoped<VerifyEmailService>();
        services.AddScoped<RegisterService>();
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<LogoutService>();
        services.AddScoped<ChangePasswordService>();

        // User Management Services (Admin)
        services.AddScoped<GetUsersService>();
        services.AddScoped<GetUserByIdService>();
        services.AddScoped<CreateUserService>();
        services.AddScoped<UpdateUserService>();
        services.AddScoped<UpdateUserStatusService>();
        services.AddScoped<DeleteUserService>();
        services.AddScoped<AssignRoleService>();
        services.AddScoped<RemoveRoleService>();

        // Profile Services (Self-service)
        services.AddScoped<GetMyProfileService>();
        services.AddScoped<UpdateMyProfileService>();

        // Subscription Services
        services.AddScoped<GetPlansService>();
        services.AddScoped<SubscribeService>();
        services.AddScoped<GetMySubscriptionService>();
        services.AddScoped<CancelSubscriptionService>();
        services.AddScoped<AdminSubscriptionPlanService>();

        // Lesson Services
        services.AddScoped<GetLessonsService>();
        services.AddScoped<GetLessonByIdService>();
        services.AddScoped<GetDictationExerciseService>();
        services.AddScoped<SubmitDictationService>();
        services.AddScoped<AdminLessonService>();
        services.AddScoped<YouTubeLessonService>();
        services.AddScoped<GetLessonSegmentsService>();
        services.AddScoped<CheckSegmentService>();
        services.AddScoped<CheckShadowingService>();
        services.AddScoped<AdminTranscriptService>();

        // Payment Services
        services.AddScoped<SePayWebhookService>();
        services.AddScoped<GetPaymentInfoService>();
        services.AddScoped<GetPaymentStatusService>();
        services.AddScoped<GetPaymentHistoryService>();
        services.AddScoped<Demif.Application.Features.Payments.CancelPayment.CancelPaymentService>();

        //Blog Services
        services.AddScoped<ICreateBlogService, CreateBlogService>();
        services.AddScoped<IGetBlogsService, GetBlogsService>();
        services.AddScoped<IGetBlogByIdService, GetBlogByIdService>();
        services.AddScoped<IUpdateBlogService, UpdateBlogService>();
        services.AddScoped<IDeleteBlogService, DeleteBlogService>();

        // Me / Progress / Streak / Analytics
        services.AddScoped<GetProgressService>();
        services.AddScoped<GetStreakService>();
        services.AddScoped<RecordActivityService>();
        services.AddScoped<GetUserAnalyticsService>();

        // Stats endpoints (Thống kê)
        services.AddScoped<GetStatsSummaryService>();
        services.AddScoped<GetActivityHeatmapService>();
        services.AddScoped<GetDailyPracticeService>();

        // Admin User Subscriptions
        services.AddScoped<AdminUserSubscriptionService>();
        
        // Admin Payments
        services.AddScoped<AdminPaymentService>();

        // Background Job Services
        services.AddScoped<SubscriptionExpiryService>();

        return services;
    }
}


