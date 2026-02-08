using Demif.Application.Features.Auth.ChangePassword;
using Demif.Application.Features.Auth.Login;
using Demif.Application.Features.Auth.Logout;
using Demif.Application.Features.Auth.FirebaseLogin;
using Demif.Application.Features.Auth.RefreshToken;
using Demif.Application.Features.Auth.Register;
using Demif.Application.Features.Lessons.Admin;
using Demif.Application.Features.Lessons.GetLessonById;
using Demif.Application.Features.Lessons.GetLessons;
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
        services.AddScoped<FirebaseLoginService>();
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
        services.AddScoped<AdminLessonService>();

        // Payment Services
        services.AddScoped<SePayWebhookService>();

        return services;
    }
}


