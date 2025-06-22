# FlexKit Test Project Management Scripts
# PowerShell script for managing test projects across the solution

param(
    [Parameter(Position=0)]
    [ValidateSet("create", "run", "coverage", "clean", "list")]
    [string]$Action = "list",

    [Parameter(Position=1)]
    [string]$ProjectName = "",

    [Parameter(Position=2)]
    [ValidateSet("unit", "integration", "performance")]
    [string]$TestType = "unit",

    [switch]$All,
    [switch]$Parallel,
    [string]$Filter = ""
)

$SolutionRoot = Split-Path -Parent $PSScriptRoot
$TestsRoot = Join-Path $SolutionRoot "tests"
$SrcRoot = Join-Path $SolutionRoot "src"

function Write-Header($message) {
    Write-Host "`n🔧 $message" -ForegroundColor Cyan
    Write-Host ("=" * ($message.Length + 3))
}

function Write-Success($message) {
    Write-Host "✅ $message" -ForegroundColor Green
}

function Write-Error($message) {
    Write-Host "❌ $message" -ForegroundColor Red
}

function Write-Info($message) {
    Write-Host "ℹ️  $message" -ForegroundColor Yellow
}

function Get-TestProjects {
    if (!(Test-Path $TestsRoot)) {
        return @()
    }

    return Get-ChildItem -Path $TestsRoot -Filter "*.csproj" -Recurse | ForEach-Object {
        $projectPath = $_.FullName
        $projectName = $_.BaseName
        $relativeDir = Split-Path -Parent ($_.FullName -replace [regex]::Escape($TestsRoot), "")

        $testType = "unit"
        if ($projectName -match "\.Integration\.Tests$") { $testType = "integration" }
        elseif ($projectName -match "\.Performance\.Tests$") { $testType = "performance" }

        [PSCustomObject]@{
            Name = $projectName
            Type = $testType
            Path = $projectPath
            Directory = $relativeDir
            SourceProject = $projectName -replace "\.Tests$|\.Integration\.Tests$|\.Performance\.Tests$", ""
        }
    }
}

function Create-TestProject {
    param($Name, $Type)

    Write-Header "Creating $Type test project: $Name"

    # Determine project name and path
    $suffix = switch ($Type) {
        "unit" { ".Tests" }
        "integration" { ".Integration.Tests" }
        "performance" { ".Performance.Tests" }
    }

    $projectName = if ($Name.EndsWith($suffix)) { $Name } else { "$Name$suffix" }
    $projectDir = Join-Path $TestsRoot $projectName
    $projectFile = Join-Path $projectDir "$projectName.csproj"

    # Check if project already exists
    if (Test-Path $projectFile) {
        Write-Error "Test project '$projectName' already exists"
        return
    }

    # Create directory
    New-Item -ItemType Directory -Path $projectDir -Force | Out-Null

    # Copy template
    $templateFile = Join-Path $TestsRoot "ProjectTemplates" "$Type-test-template.csproj"
    if (Test-Path $templateFile) {
        Copy-Item $templateFile $projectFile
        Write-Success "Created project file: $projectFile"
    } else {
        # Create minimal project file
        $content = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- $Type test specific settings -->
    <CoverageThreshold>100</CoverageThreshold>
  </PropertyGroup>

</Project>
"@
        Set-Content -Path $projectFile -Value $content
        Write-Success "Created minimal project file: $projectFile"
    }

    # Create basic test class
    $testClassName = "$($Name -replace '\..*$', '')Tests"
    $testFile = Join-Path $projectDir "$testClassName.cs"

    $testContent = @"
using FluentAssertions;
using Xunit;

namespace $projectName;

/// <summary>
/// $Type tests for $($Name -replace '\..*$', '').
/// </summary>
public class $testClassName
{
    [Fact]
    public void Placeholder_Test_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected);
    }
}
"@

    Set-Content -Path $testFile -Value $testContent
    Write-Success "Created test class: $testFile"

    # Add to solution if dotnet-sln is available
    try {
        Push-Location $SolutionRoot
        dotnet sln add $projectFile 2>$null
        Write-Success "Added to solution"
    } catch {
        Write-Info "Could not add to solution automatically"
    } finally {
        Pop-Location
    }

    Write-Success "Test project '$projectName' created successfully!"
}

function Run-Tests {
    param($Projects, $ParallelExecution, $FilterExpression)

    Write-Header "Running tests"

    if ($Projects.Count -eq 0) {
        Write-Error "No test projects found"
        return
    }

    $testParams = @(
        "--configuration", "Release",
        "--logger", "trx",
        "--collect:XPlat Code Coverage"
    )

    if ($FilterExpression) {
        $testParams += "--filter", $FilterExpression
    }

    if ($ParallelExecution) {
        $testParams += "--parallel"
    }

    foreach ($project in $Projects) {
        Write-Info "Running tests for $($project.Name) ($($project.Type))"

        Push-Location (Split-Path $project.Path)
        try {
            dotnet test @testParams
            if ($LASTEXITCODE -eq 0) {
                Write-Success "$($project.Name) tests passed"
            } else {
                Write-Error "$($project.Name) tests failed"
            }
        } finally {
            Pop-Location
        }
    }
}

function Generate-CoverageReport {
    param($Projects)

    Write-Header "Generating coverage report"

    $testResultsPath = Join-Path $SolutionRoot "TestResults"
    if (!(Test-Path $testResultsPath)) {
        Write-Error "TestResults directory not found. Run tests first."
        return
    }

    $coverageFiles = Get-ChildItem -Path $testResultsPath -Filter "coverage.cobertura.xml" -Recurse

    if ($coverageFiles.Count -eq 0) {
        Write-Error "No coverage files found. Run tests first."
        return
    }

    Write-Info "Found $($coverageFiles.Count) coverage files"

    # Create coverage report directory
    $reportDir = Join-Path $SolutionRoot "TestResults" "CoverageReport"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    # Display coverage files
    Write-Info "Coverage files located at:"
    $coverageFiles | ForEach-Object {
        Write-Host "  $($_.FullName)" -ForegroundColor Gray
    }

    # Check if ReportGenerator is installed
    $reportGeneratorInstalled = $false
    try {
        $reportGenVersion = dotnet tool list -g | Select-String "dotnet-reportgenerator-globaltool"
        if ($reportGenVersion) {
            $reportGeneratorInstalled = $true
            Write-Success "ReportGenerator is already installed"
        }
    }
    catch {
        # Tool not found
    }

    if (!$reportGeneratorInstalled) {
        Write-Info "Installing ReportGenerator..."
        try {
            dotnet tool install -g dotnet-reportgenerator-globaltool 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "ReportGenerator installed successfully"
                $reportGeneratorInstalled = $true
            }
            else {
                Write-Info "Could not install ReportGenerator automatically"
            }
        }
        catch {
            Write-Info "Could not install ReportGenerator automatically"
        }
    }

    if ($reportGeneratorInstalled) {
        # Generate HTML report
        Write-Info "Generating HTML coverage report..."

        $reportFiles = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
        $reportCommand = "reportgenerator"
        $reportArgs = @(
            "-reports:`"$reportFiles`"",
            "-targetdir:`"$reportDir`"",
            "-reporttypes:Html;HtmlSummary;Badges;TextSummary",
            "-historydir:`"$reportDir\history`"",
            "-title:`"FlexKit Test Coverage Report`""
        )

        try {
            & $reportCommand $reportArgs 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Coverage report generated successfully!"
                $indexFile = Join-Path $reportDir "index.html"
                if (Test-Path $indexFile) {
                    Write-Info "Report location: $indexFile"

                    # Try to open the report in default browser
                    try {
                        Start-Process $indexFile
                        Write-Success "Opened coverage report in browser"
                    }
                    catch {
                        Write-Info "To view the report, open: $indexFile"
                    }
                }
            }
            else {
                Write-Error "Failed to generate coverage report"
            }
        }
        catch {
            Write-Error "Error running ReportGenerator: $($_.Exception.Message)"
        }
    }
    else {
        # Provide manual instructions
        Write-Info "To generate HTML report manually, install ReportGenerator and run:"
        Write-Host "dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Gray

        $reportFiles = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
        Write-Host "reportgenerator -reports:`"$reportFiles`" -targetdir:`"$reportDir`" -reporttypes:Html" -ForegroundColor Gray
    }

    # Display coverage summary if available
    $summaryFiles = Get-ChildItem -Path $testResultsPath -Filter "*.txt" -Recurse | Where-Object { $_.Name -like "*summary*" }
    if ($summaryFiles.Count -gt 0) {
        Write-Info "Coverage Summary:"
        $summaryFiles | ForEach-Object {
            if (Test-Path $_.FullName) {
                $summaryContent = Get-Content $_.FullName -Raw
                Write-Host $summaryContent -ForegroundColor Cyan
            }
        }
    }

    # Calculate and display quick stats
    Write-Info "Quick Coverage Statistics:"
    $totalProjects = $Projects.Count
    $projectsWithCoverage = $coverageFiles.Count
    $coveragePercentage = if ($totalProjects -gt 0) { [math]::Round(($projectsWithCoverage / $totalProjects) * 100, 2) } else { 0 }

    Write-Host "  Total test projects: $totalProjects" -ForegroundColor White
    Write-Host "  Projects with coverage: $projectsWithCoverage" -ForegroundColor White
    Write-Host "  Coverage file generation: $coveragePercentage%" -ForegroundColor White

    if ($reportGeneratorInstalled) {
        Write-Host "  HTML Report: $reportDir\index.html" -ForegroundColor Green
    }
}

function Clean-TestResults {
    Write-Header "Cleaning test results"

    $testResultsPath = Join-Path $SolutionRoot "TestResults"
    if (Test-Path $testResultsPath) {
        Remove-Item $testResultsPath -Recurse -Force
        Write-Success "Cleaned test results directory"
    } else {
        Write-Info "No test results to clean"
    }

    # Clean bin/obj in test projects
    $projects = Get-TestProjects
    foreach ($project in $projects) {
        $projectDir = Split-Path $project.Path

        $binPath = Join-Path $projectDir "bin"
        $objPath = Join-Path $projectDir "obj"

        if (Test-Path $binPath) {
            Remove-Item $binPath -Recurse -Force
        }
        if (Test-Path $objPath) {
            Remove-Item $objPath -Recurse -Force
        }
    }

    Write-Success "Cleaned all test project build artifacts"
}

function List-TestProjects {
    Write-Header "FlexKit Test Projects"

    $projects = Get-TestProjects

    if ($projects.Count -eq 0) {
        Write-Info "No test projects found in $TestsRoot"
        return
    }

    $grouped = $projects | Group-Object Type

    foreach ($group in $grouped) {
        Write-Host "`n📁 $($group.Name.ToUpper()) TESTS:" -ForegroundColor Yellow
        foreach ($project in $group.Group) {
            $sourceExists = Test-Path (Join-Path $SrcRoot "$($project.SourceProject)/$($project.SourceProject).csproj")
            $sourceStatus = if ($sourceExists) { "✅" } else { "❌" }

            Write-Host "  $sourceStatus $($project.Name)" -NoNewline
            if ($project.Directory) {
                Write-Host " (in $($project.Directory))" -ForegroundColor Gray
            } else {
                Write-Host ""
            }
        }
    }

    Write-Host "`n📊 Summary:"
    Write-Host "  Total test projects: $($projects.Count)"
    $grouped | ForEach-Object {
        Write-Host "  $($_.Name): $($_.Count)"
    }
}

function Create-TestProject {
    param($Name, $Type)

    Write-Header "Creating $Type test project: $Name"

    # Determine project name and path
    $suffix = switch ($Type) {
        "unit" { ".Tests" }
        "integration" { ".Integration.Tests" }
        "performance" { ".Performance.Tests" }
        default { throw "Unknown test type: $Type" }
    }

    $projectName = if ($Name.EndsWith($suffix)) { $Name } else { "$Name$suffix" }
    $projectDir = Join-Path $TestsRoot $projectName
    $projectFile = Join-Path $projectDir "$projectName.csproj"

    # Check if project already exists
    if (Test-Path $projectFile) {
        Write-Error "Test project '$projectName' already exists"
        return
    }

    # Create directory
    New-Item -ItemType Directory -Path $projectDir -Force | Out-Null

    # Copy template or create minimal project file
    $templateFile = Join-Path $TestsRoot "ProjectTemplates" "$Type-test-template.csproj"
    if (Test-Path $templateFile) {
        Copy-Item $templateFile $projectFile
        Write-Success "Created project file from template: $projectFile"
    } else {
        $content = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- $Type test specific settings -->
    <CoverageThreshold>100</CoverageThreshold>
  </PropertyGroup>

</Project>
"@
        Set-Content -Path $projectFile -Value $content
        Write-Success "Created minimal project file: $projectFile"
    }

    # Create basic test class
    $testClassName = "$($Name -replace '\..*$', '')Tests"
    $testFile = Join-Path $projectDir "$testClassName.cs"

    $testContent = @"
using FluentAssertions;
using Xunit;

namespace $projectName;

/// <summary>
/// $Type tests for $($Name -replace '\..*$', '').
/// </summary>
public class $testClassName
{
    [Fact]
    public void Placeholder_Test_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected);
    }
}
"@

    Set-Content -Path $testFile -Value $testContent
    Write-Success "Created test class: $testFile"

    # Create TestData directory if it doesn't exist
    $testDataDir = Join-Path $projectDir "TestData"
    if (!(Test-Path $testDataDir)) {
        New-Item -ItemType Directory -Path $testDataDir -Force | Out-Null

        # Create a sample test data file
        $sampleDataFile = Join-Path $testDataDir "sample.json"
        $sampleContent = @"
{
  "testData": {
    "value": "sample",
    "number": 42,
    "flag": true
  }
}
"@
        Set-Content -Path $sampleDataFile -Value $sampleContent
        Write-Success "Created TestData directory with sample files"
    }

    # Add to solution if dotnet-sln is available
    try {
        Push-Location $SolutionRoot
        $addResult = dotnet sln add $projectFile 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Added to solution"
        } else {
            Write-Info "Could not add to solution automatically: $addResult"
        }
    }
    catch {
        Write-Info "Could not add to solution automatically: $($_.Exception.Message)"
    }
    finally {
        Pop-Location
    }

    Write-Success "Test project '$projectName' created successfully!"
    Write-Info "Next steps:"
    Write-Host "  1. Add project-specific package references if needed" -ForegroundColor Gray
    Write-Host "  2. Implement your test cases" -ForegroundColor Gray
    Write-Host "  3. Run tests: .\scripts\test-management.ps1 run --project=$($Name -replace '\..*$', '')" -ForegroundColor Gray
}

function Show-Usage {
    Write-Host "FlexKit Test Project Management Script" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\test-management.ps1 <action> [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "Actions:" -ForegroundColor Yellow
    Write-Host "  create <name> [type]        Create a new test project" -ForegroundColor White
    Write-Host "  run [options]               Run tests" -ForegroundColor White
    Write-Host "  coverage                    Generate coverage report" -ForegroundColor White
    Write-Host "  clean                       Clean test results" -ForegroundColor White
    Write-Host "  list                        List all test projects" -ForegroundColor White
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -ProjectName <name>         Project name (for create/run)" -ForegroundColor White
    Write-Host "  -TestType <type>            Test project type (unit|integration|performance)" -ForegroundColor White
    Write-Host "  -Filter <expression>        Test filter expression" -ForegroundColor White
    Write-Host "  -All                        Include all test types" -ForegroundColor White
    Write-Host "  -Parallel                   Run tests in parallel" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\test-management.ps1 create MyProject.Feature unit" -ForegroundColor Gray
    Write-Host "  .\test-management.ps1 run -TestType unit -Parallel" -ForegroundColor Gray
    Write-Host "  .\test-management.ps1 run -ProjectName Configuration -Filter `"Category=Unit`"" -ForegroundColor Gray
    Write-Host "  .\test-management.ps1 list" -ForegroundColor Gray
    Write-Host ""
}

# Validate parameters
if ($Action -eq "" -or $Action -eq "help" -or $Action -eq "-h" -or $Action -eq "--help") {
    Show-Usage
    exit 0
}

if ($Action -eq "create" -and $ProjectName -eq "") {
    Write-Error "Project name is required for create action"
    Show-Usage
    exit 1
}

# Create TestsRoot directory if it doesn't exist
if (!(Test-Path $TestsRoot)) {
    Write-Info "Creating tests directory: $TestsRoot"
    New-Item -ItemType Directory -Path $TestsRoot -Force | Out-Null
}

# Create ProjectTemplates directory if it doesn't exist
$templatesDir = Join-Path $TestsRoot "ProjectTemplates"
if (!(Test-Path $templatesDir)) {
    Write-Info "Creating project templates directory: $templatesDir"
    New-Item -ItemType Directory -Path $templatesDir -Force | Out-Null
}

# Main script execution
try {
    switch ($Action.ToLower()) {
        "create" {
            Create-TestProject -Name $ProjectName -Type $TestType
        }

        "run" {
            $projects = Get-TestProjects

            if ($ProjectName) {
                $projects = $projects | Where-Object { $_.Name -like "*$ProjectName*" }
            }

            if (!$All -and $TestType) {
                $projects = $projects | Where-Object { $_.Type -eq $TestType }
            }

            if ($projects.Count -eq 0) {
                Write-Error "No matching test projects found"
                exit 1
            }

            Run-Tests -Projects $projects -ParallelExecution:$Parallel -FilterExpression $Filter
        }

        "coverage" {
            $projects = Get-TestProjects
            Generate-CoverageReport -Projects $projects
        }

        "clean" {
            Clean-TestResults
        }

        "list" {
            List-TestProjects
        }

        default {
            Write-Error "Unknown action: $Action"
            Write-Host "Available actions: create, run, coverage, clean, list" -ForegroundColor Yellow
            Show-Usage
            exit 1
        }
    }
}
catch {
    Write-Error "Script execution failed: $($_.Exception.Message)"
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 Script completed successfully!" -ForegroundColor Green
