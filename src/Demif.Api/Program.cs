using Demif.Api.Configurations;
using Demif.Application;
using Demif.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
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

var app = builder.Build();

// Enable CORS
app.UseCors("AllowAll");

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
