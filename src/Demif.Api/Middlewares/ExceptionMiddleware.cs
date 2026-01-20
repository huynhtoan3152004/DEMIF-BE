using System.Net;
using System.Text.Json;
using Demif.Application.Common.Exceptions;
using Demif.Domain.Exceptions;

namespace Demif.Api.Middlewares;

/// <summary>
/// Middleware xử lý exception toàn cục
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { errors = validationEx.Errors })
            ),
            ForbiddenException => (
                HttpStatusCode.Forbidden,
                JsonSerializer.Serialize(new { error = exception.Message })
            ),
            EntityNotFoundException => (
                HttpStatusCode.NotFound,
                JsonSerializer.Serialize(new { error = exception.Message })
            ),
            DomainException => (
                HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(new { error = exception.Message })
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                JsonSerializer.Serialize(new { error = "An internal server error occurred." })
            )
        };

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(message);
    }
}

/// <summary>
/// Extension để đăng ký middleware
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}
