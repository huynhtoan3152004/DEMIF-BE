# ==============================================
# Dockerfile for Demif-BE .NET 8 API
# Optimized for Coolify deployment
# ==============================================

# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY src/Demif.Domain/Demif.Domain.csproj ./Demif.Domain/
COPY src/Demif.Application/Demif.Application.csproj ./Demif.Application/
COPY src/Demif.Infrastructure/Demif.Infrastructure.csproj ./Demif.Infrastructure/
COPY src/Demif.Api/Demif.Api.csproj ./Demif.Api/

RUN dotnet restore ./Demif.Api/Demif.Api.csproj

# Copy all source code
COPY src/ .

# Build and publish
RUN dotnet publish ./Demif.Api/Demif.Api.csproj -c Release -o /app/publish --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=build /app/publish .

# Expose port (Coolify will handle port mapping)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Demif.Api.dll"]
