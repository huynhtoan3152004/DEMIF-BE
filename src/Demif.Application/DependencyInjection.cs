using Demif.Application.Features.Auth.Login;
using Demif.Application.Features.Auth.FirebaseLogin;
using FluentValidation;
using Demif.Application.Features.Blog;
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

        // Register Services (Feature services)
        services.AddScoped<LoginService>();
        services.AddScoped<FirebaseLoginService>();
        services.AddScoped<BlogService>();
        // services.AddScoped<RegisterService>();
        // services.AddScoped<GetLessonsService>();
        // ... thêm các service khác

        return services;
    }
}
