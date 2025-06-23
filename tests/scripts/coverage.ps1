# powershell -ExecutionPolicy Bypass -File ../scripts/coverage.ps1
# Clean previous results
if (Test-Path "./TestResults") {
    Remove-Item -Recurse -Force ./TestResults
}

# Run tests with coverage
dotnet test

# Check if coverage files were generated
$coverageFiles = Get-ChildItem -Path "./TestResults" -Filter "*.cobertura.xml" -Recurse

if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found. Trying XPlat Code Coverage..." -ForegroundColor Yellow
    dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
    $coverageFiles = Get-ChildItem -Path "./TestResults" -Filter "coverage.cobertura.xml" -Recurse
}

if ($coverageFiles.Count -gt 0) {
    $coverageFile = $coverageFiles[0].FullName
    Write-Host "Found coverage file: $coverageFile" -ForegroundColor Green

    # Use the correct command for the global tool
    try {
        Write-Host "Generating HTML coverage report..." -ForegroundColor Green
        reportgenerator -reports:"$coverageFile" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
    }
    catch {
        Write-Host "Error running reportgenerator: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Trying alternative command..." -ForegroundColor Yellow

        # Alternative: use dotnet tool run
        dotnet tool run reportgenerator -- -reports:"$coverageFile" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
    }

    # Open report
    $reportPath = "./TestResults/CoverageReport/index.html"
    if (Test-Path $reportPath) {
        Write-Host "Opening coverage report..." -ForegroundColor Green
        Start-Process $reportPath
    } else {
        Write-Host "Report generation may have failed. Report not found at: $reportPath" -ForegroundColor Red
    }
} else {
    Write-Host "No coverage files found!" -ForegroundColor Red
}