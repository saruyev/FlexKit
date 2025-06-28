#!/usr/bin/env pwsh

<#
..\scripts\integration.ps1 -ProjectPath "."
.SYNOPSIS
    Simple integration test runner for FlexKit projects
.DESCRIPTION
    Discovers and runs integration test projects using dotnet test command
.PARAMETER TestFilter
    Filter for specific tests (e.g., "Category=Integration")
.PARAMETER Configuration
    Build configuration (Debug/Release). Default: Debug
.PARAMETER Verbosity
    Test verbosity (quiet/minimal/normal/detailed/diagnostic). Default: normal
.PARAMETER CollectCoverage
    Collect code coverage. Default: false
.PARAMETER OutputDir
    Output directory for test results. Default: ./TestResults
.PARAMETER ProjectPattern
    Pattern to match test projects. Default: "*.IntegrationTests.csproj"
.PARAMETER ProjectPath
    Path to search for projects. Default: current directory
.EXAMPLE
    .\Run-IntegrationTests.ps1
.EXAMPLE
    .\Run-IntegrationTests.ps1 -TestFilter "Category=Database" -Verbosity detailed
.EXAMPLE
    .\Run-IntegrationTests.ps1 -Configuration Release -CollectCoverage
#>

[CmdletBinding()]
param(
    [string]$TestFilter = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$Verbosity = "normal",
    [switch]$CollectCoverage,
    [string]$OutputDir = "./TestResults",
    [string]$ProjectPattern = "*.IntegrationTests.csproj",
    [string]$ProjectPath = "."
)

function Write-Header($message) {
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host "  $message" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

function Find-TestProjects {
    Write-Header "Finding Integration Test Projects"

    $projects = Get-ChildItem -Path $ProjectPath -Recurse -Filter $ProjectPattern

    if ($projects.Count -eq 0) {
        Write-Host "No integration test projects found matching: $ProjectPattern" -ForegroundColor Red
        exit 1
    }

    Write-Host "Found $($projects.Count) test project(s):" -ForegroundColor Green
    foreach ($project in $projects) {
        Write-Host "  • $($project.Name)" -ForegroundColor White
    }

    return $projects
}

function Run-Tests($projects) {
    Write-Header "Running Integration Tests"

    # Create output directory
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    $allPassed = $true
    $totalProjects = $projects.Count
    $passedProjects = 0

    foreach ($project in $projects) {
        Write-Host ""
        Write-Host "Running tests for: $($project.Name)" -ForegroundColor Yellow
        Write-Host "Project path: $($project.FullName)" -ForegroundColor Gray

        # Build test arguments
        $testArgs = @(
            "test"
            $project.FullName
            "--configuration", $Configuration
            "--verbosity", $Verbosity
            "--logger", "trx"
            "--results-directory", $OutputDir
        )

        if ($TestFilter) {
            $testArgs += "--filter", $TestFilter
        }

        if ($CollectCoverage) {
            $testArgs += "--collect", "XPlat Code Coverage"
        }

        # Run the test
        Write-Host "Command: dotnet $($testArgs -join ' ')" -ForegroundColor Gray

        $exitCode = 0
        try {
            & dotnet @testArgs
            $exitCode = $LASTEXITCODE
        }
        catch {
            Write-Host "Error running tests: $_" -ForegroundColor Red
            $exitCode = 1
        }

        if ($exitCode -eq 0) {
            Write-Host "✓ Tests PASSED for $($project.Name)" -ForegroundColor Green
            $passedProjects++
        } else {
            Write-Host "✗ Tests FAILED for $($project.Name) (Exit code: $exitCode)" -ForegroundColor Red
            $allPassed = $false
        }
    }

    return @{
        AllPassed = $allPassed
        TotalProjects = $totalProjects
        PassedProjects = $passedProjects
        FailedProjects = ($totalProjects - $passedProjects)
    }
}

function Generate-HtmlReport($outputDir) {
    Write-Header "HTML Report Generation Skipped"
    Write-Host "HTML report generation has been disabled." -ForegroundColor Yellow
    Write-Host "TRX files are available in: $outputDir" -ForegroundColor White
}

function Show-Summary($results) {
    Write-Header "Test Results Summary"

    Write-Host "Total Projects:  $($results.TotalProjects)" -ForegroundColor White
    Write-Host "Passed:          $($results.PassedProjects)" -ForegroundColor Green
    Write-Host "Failed:          $($results.FailedProjects)" -ForegroundColor $(if ($results.FailedProjects -eq 0) { "Green" } else { "Red" })

    if ($results.AllPassed) {
        Write-Host ""
        Write-Host "🎉 ALL TESTS PASSED! 🎉" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ SOME TESTS FAILED ❌" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "Test results saved to: $OutputDir" -ForegroundColor Gray
}

# Main execution
try {
    $startTime = Get-Date

    Write-Header "FlexKit Integration Test Runner"
    Write-Host "Configuration: $Configuration" -ForegroundColor White
    Write-Host "Verbosity:     $Verbosity" -ForegroundColor White
    Write-Host "Output Dir:    $OutputDir" -ForegroundColor White
    Write-Host "Pattern:       $ProjectPattern" -ForegroundColor White
    Write-Host "Search Path:   $ProjectPath" -ForegroundColor White
    if ($TestFilter) {
        Write-Host "Filter:        $TestFilter" -ForegroundColor White
    }
    if ($CollectCoverage) {
        Write-Host "Coverage:      Enabled" -ForegroundColor White
    }

    $projects = Find-TestProjects
    $results = Run-Tests -projects $projects
    Generate-HtmlReport -outputDir $OutputDir
    Show-Summary -results $results

    $endTime = Get-Date
    $duration = $endTime - $startTime
    Write-Host "Total execution time: $($duration.ToString('hh\:mm\:ss'))" -ForegroundColor Gray

    if ($results.AllPassed) {
        exit 0
    } else {
        exit 1
    }
}
catch {
    Write-Host "Script failed: $_" -ForegroundColor Red
    exit 1
}