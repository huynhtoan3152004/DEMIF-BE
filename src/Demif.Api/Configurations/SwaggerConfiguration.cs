using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Demif.Api.Configurations;

/// <summary>
/// Cấu hình Swagger/OpenAPI cho DEMIF API
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Thêm cấu hình Swagger với JWT Bearer Authorization và XML Documentation
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // Thông tin API
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DEMIF API",
                Version = "v1",
                Description = "API cho ứng dụng học tiếng Anh DEMIF - Dictation, English Mastery, Improving Fluency",
                Contact = new OpenApiContact
                {
                    Name = "DEMIF Team",
                    Email = "support@demif.app"
                }
            });

            // JWT Bearer Authorization
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Nhập JWT token để xác thực. Ví dụ: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Thêm XML Documentation Comments
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // Thêm XML từ Application layer (nếu có)
            var applicationXml = Path.Combine(AppContext.BaseDirectory, "Demif.Application.xml");
            if (File.Exists(applicationXml))
            {
                options.IncludeXmlComments(applicationXml);
            }

            // Group endpoints theo tag
            options.TagActionsBy(api =>
            {
                if (api.GroupName != null) return new[] { api.GroupName };
                if (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller))
                {
                    return new[] { controller };
                }
                return new[] { "Other" };
            });

            // Sắp xếp theo tên tag
            options.OrderActionsBy(api => api.RelativePath);
        });

        return services;
    }

    /// <summary>
    /// Cấu hình Swagger UI middleware
    /// </summary>
    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "DEMIF API v1");
            options.DocumentTitle = "DEMIF API Documentation";
            options.DefaultModelsExpandDepth(-1); // Ẩn models mặc định
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
        });

        return app;
    }
}
