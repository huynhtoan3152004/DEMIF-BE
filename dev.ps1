# Local build, test, migrate, and docker script for Windows
# Usage: .\dev.ps1 [command]
# Commands: build, test, migrate, docker, all

param(
    [Parameter(Position=0)]
    [ValidateSet("build", "test", "migrate", "docker", "all")]
    [string]$Command = "all"
)

$ErrorActionPreference = "Stop"

function Write-Success { param($msg) Write-Host "✓ $msg" -ForegroundColor Green }
function Write-Step { param($msg) Write-Host "→ $msg" -ForegroundColor Cyan }

function Invoke-Build {
    Write-Step "Building..."
    dotnet build --configuration Release
    if ($LASTEXITCODE -eq 0) { Write-Success "Build completed" }
    else { throw "Build failed" }
}

function Invoke-Test {
    Invoke-Build
    Write-Step "Running tests..."
    dotnet test --no-build --configuration Release --verbosity normal
    if ($LASTEXITCODE -eq 0) { Write-Success "Tests passed" }
    else { throw "Tests failed" }
}

function Invoke-Migrate {
    Write-Step "Restoring dotnet-ef tool..."
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed" }

    Write-Step "Applying EF Core migrations..."
    dotnet ef database update --project src/Demif.Infrastructure --startup-project src/Demif.Api
    if ($LASTEXITCODE -eq 0) { Write-Success "Database migrations applied" }
    else { throw "Migration failed" }
}

function Invoke-Docker {
    Write-Step "Building Docker image..."
    docker build -t demif-be:latest .
    if ($LASTEXITCODE -eq 0) { Write-Success "Docker image built: demif-be:latest" }
    else { throw "Docker build failed" }
}

function Invoke-All {
    Invoke-Build
    # Invoke-Test  # Uncomment khi có tests
    Invoke-Docker
    Write-Success "All steps completed!"
}

switch ($Command) {
    "build" { Invoke-Build }
    "test" { Invoke-Test }
    "migrate" { Invoke-Migrate }
    "docker" { Invoke-Docker }
    "all" { Invoke-All }
}
