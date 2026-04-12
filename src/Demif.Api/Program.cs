using Demif.Api.Configurations;
using Demif.Application;
using Demif.Infrastructure;
using Demif.Infrastructure.Persistence;
using Demif.Api.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services — Enable enum string support: FE can send "Beginner" OR 0
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Accept enum as string ("Beginner") or integer (0) in both request and response
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger với JWT Bearer Authorization và XML Documentation
builder.Services.AddSwaggerConfiguration();

// Add Application & Infrastructure (Clean Architecture DI)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// CORS - Allow Frontend to access API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Rate Limiting - protect API from abuse
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General API limit: 100 req/minute per sliding window
    opts.AddSlidingWindowLimiter("general", o =>
    {
        o.PermitLimit = 100;
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 10;
    });

    // Stricter limit for auth endpoints: 10 req/minute
    opts.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
});

var app = builder.Build();

// Apply EF Core migrations on startup so deployed environments stay in sync with the current model.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

    if (pendingMigrations.Any())
    {
        Log.Information("EF Core pending migrations: {PendingMigrations}", string.Join(", ", pendingMigrations));
    }
    else
    {
        Log.Information("EF Core has no pending migrations.");
    }

    await dbContext.Database.MigrateAsync();
    Log.Information("EF Core migrations applied successfully.");
}

// Enable CORS
app.UseCors("AllowAll");

// Exception Middleware - catch all errors and return JSON with CORS headers
app.UseExceptionMiddleware();

// Rate Limiting
app.UseRateLimiter();

// Configure pipeline - Enable Swagger in all environments
app.UseSwaggerConfiguration();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

Log.Information("Starting DEMIF API...");
app.Run();
