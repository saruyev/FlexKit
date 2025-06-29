#!/usr/bin/env pwsh
#  .\benchmarks.ps1 -ProjectPath "FlexKit.Configuration.PerformanceTests" 
param(
    [string]$Filter = "",
    [string]$ProjectPath = "",
    [switch]$Html,
    [switch]$Csv,
    [switch]$Json,
    [switch]$All,
    [switch]$Quick,
    [string]$Output = "BenchmarkResults"
)

Write-Host "FlexKit Performance Benchmarks" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Set working directory if ProjectPath is specified
if ($ProjectPath) {
    if (Test-Path $ProjectPath) {
        Set-Location $ProjectPath
        Write-Host "Changed to project directory: $ProjectPath" -ForegroundColor Cyan
    } else {
        Write-Error "Project path does not exist: $ProjectPath"
        exit 1
    }
}

# Ensure we're in Release mode
Write-Host "Building project in Release mode..." -ForegroundColor Yellow
dotnet build -c Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Please fix compilation errors before running benchmarks."
    exit 1
}

# Prepare benchmark arguments
$benchmarkArgs = @("run", "-c", "Release")

# Add filter if specified
if ($Filter) {
    $benchmarkArgs += "--filter"
    $benchmarkArgs += "*$Filter*"
    Write-Host "Running benchmarks with filter: $Filter" -ForegroundColor Cyan
}

# Add exporters
$exporters = @()
if ($Html) { $exporters += "html" }
if ($Csv) { $exporters += "csv" }
if ($Json) { $exporters += "json" }
if ($All) {
    $exporters += @("html", "csv", "json", "markdown")
}

if ($exporters.Count -gt 0) {
    $benchmarkArgs += "--exporters"
    $benchmarkArgs += ($exporters -join ",")
    Write-Host "Exporting results to: $($exporters -join ", ")" -ForegroundColor Cyan
}

# Quick mode for development
if ($Quick) {
    $benchmarkArgs += "--job"
    $benchmarkArgs += "dry"
    Write-Host "Running in quick mode (dry run)" -ForegroundColor Yellow
}

# Set output directory
if ($Output) {
    $benchmarkArgs += "--artifacts"
    $benchmarkArgs += $Output
}

Write-Host ""
Write-Host "Running benchmarks with arguments: $($benchmarkArgs -join ' ')" -ForegroundColor Cyan
Write-Host ""

# Run the benchmarks
& dotnet @benchmarkArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Benchmarks completed successfully!" -ForegroundColor Green

    if ($exporters.Count -gt 0) {
        Write-Host "Results exported to: $Output/" -ForegroundColor Green

        # List generated files
        if (Test-Path $Output) {
            $files = Get-ChildItem $Output -Filter "*.html", "*.csv", "*.json", "*.md" | Select-Object Name
            if ($files) {
                Write-Host "Generated files:" -ForegroundColor Yellow
                $files | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor White }
            }
        }
    }
} else {
    Write-Error "Benchmarks failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}