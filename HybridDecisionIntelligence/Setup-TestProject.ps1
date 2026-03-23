#!/usr/bin/env powershell
<#
.SYNOPSIS
    Setup script for HybridDecisionIntelligence.Tests XUnit test project
    
.DESCRIPTION
    Initializes test project, adds dependencies, and runs initial test suite
    
.EXAMPLE
    .\Setup-TestProject.ps1
#>

param(
    [switch]$SkipBuild = $false,
    [switch]$RunTests = $true,
    [switch]$GenerateCoverage = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "HybridDecisionIntelligence Test Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$solutionRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProjectPath = Join-Path $solutionRoot "HybridDecisionIntelligence.Tests"
$testProjFile = Join-Path $testProjectPath "HybridDecisionIntelligence.Tests.csproj"

# Step 1: Validate test project exists
Write-Host "[1/5] Validating test project..." -ForegroundColor Yellow
if (-not (Test-Path $testProjFile)) {
    Write-Host "[ERROR] Test project file not found: $testProjFile" -ForegroundColor Red
    exit 1
}
Write-Host "    ✓ Test project found" -ForegroundColor Green

# Step 2: Restore dependencies
Write-Host "[2/5] Restoring NuGet packages..." -ForegroundColor Yellow
Push-Location $solutionRoot

try {
    dotnet restore
    Write-Host "    ✓ Packages restored successfully" -ForegroundColor Green
}
catch {
    Write-Host "    ✗ Failed to restore packages: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Build solution
if (-not $SkipBuild) {
    Write-Host "[3/5] Building solution..." -ForegroundColor Yellow
    
    try {
        dotnet build -c Debug
        Write-Host "    ✓ Solution built successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "    ✗ Build failed: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "[3/5] Skipping build" -ForegroundColor Yellow
}

# Step 4: Run tests
if ($RunTests) {
    Write-Host "[4/5] Running test suite..." -ForegroundColor Yellow
    Write-Host ""
    
    try {
        dotnet test --verbosity normal --logger "console;verbosity=detailed"
        Write-Host ""
        Write-Host "    ✓ Test execution completed" -ForegroundColor Green
    }
    catch {
        Write-Host "    ✗ Tests failed: $_" -ForegroundColor Red
        # Don't exit, allow user to see results
    }
}
else {
    Write-Host "[4/5] Skipping test execution" -ForegroundColor Yellow
}

# Step 5: Generate coverage report (optional)
if ($GenerateCoverage) {
    Write-Host "[5/5] Generating code coverage report..." -ForegroundColor Yellow
    
    # First ensure coverlet is installed
    dotnet add package coverlet.collector --version 6.0.0
    
    try {
        dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
        Write-Host "    ✓ Coverage report generated" -ForegroundColor Green
    }
    catch {
        Write-Host "    ✗ Coverage generation failed: $_" -ForegroundColor Red
    }
}
else {
    Write-Host "[5/5] Skipping coverage report" -ForegroundColor Yellow
}

Pop-Location

# Summary
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Green
Write-Host "  1. Run all tests:"
Write-Host "     dotnet test" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Run specific test class:"
Write-Host "     dotnet test --filter 'FullyQualifiedName~DecisionLogicTests'" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Run specific scenario:"
Write-Host "     dotnet test --filter 'Name~Scenario_A'" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. Generate coverage report:"
Write-Host "     dotnet test /p:CollectCoverage=true" -ForegroundColor Gray
Write-Host ""
Write-Host "  5. View test documentation:"
Write-Host "     .\HybridDecisionIntelligence.Tests\README.md" -ForegroundColor Gray
Write-Host ""
