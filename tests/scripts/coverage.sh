#!/bin/bash

# coverage.sh - Test coverage reporter for FlexKit projects
# chmod +x tests/scripts/coverage.sh
# Usage: ./coverage.sh

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "Running test coverage analysis..."

# Clean previous results
if [[ -d "./TestResults" ]]; then
    echo "Cleaning previous test results..."
    rm -rf ./TestResults
fi

# Run tests with coverage
echo "Running tests..."
dotnet test

# Check if coverage files were generated
echo "Looking for coverage files..."
coverage_files=$(find ./TestResults -name "*.cobertura.xml" -type f 2>/dev/null || true)

if [[ -z "$coverage_files" ]]; then
    printf "${YELLOW}No coverage files found. Trying XPlat Code Coverage...${NC}\n"
    dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
    coverage_files=$(find ./TestResults -name "coverage.cobertura.xml" -type f 2>/dev/null || true)
fi

if [[ -n "$coverage_files" ]]; then
    # Get the first coverage file
    coverage_file=$(echo "$coverage_files" | head -n1)
    printf "${GREEN}Found coverage file: %s${NC}\n" "$coverage_file"
    
    # Use the correct command for the global tool
    printf "${GREEN}Generating HTML coverage report...${NC}\n"
    
    if command -v reportgenerator >/dev/null 2>&1; then
        # Try global tool first
        if ! reportgenerator -reports:"$coverage_file" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html; then
            printf "${YELLOW}Global reportgenerator failed, trying dotnet tool...${NC}\n"
            dotnet tool run reportgenerator -- -reports:"$coverage_file" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
        fi
    else
        # Try dotnet tool run
        printf "${YELLOW}reportgenerator not found globally, trying dotnet tool...${NC}\n"
        dotnet tool run reportgenerator -- -reports:"$coverage_file" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
    fi
    
    # Check if report was generated
    report_path="./TestResults/CoverageReport/index.html"
    if [[ -f "$report_path" ]]; then
        printf "${GREEN}Coverage report generated successfully!${NC}\n"
        printf "${GREEN}Report location: %s${NC}\n" "$report_path"
        
        # Try to open report (different commands for different platforms)
        if command -v open >/dev/null 2>&1; then
            # macOS
            printf "${GREEN}Opening coverage report...${NC}\n"
            open "$report_path"
        elif command -v xdg-open >/dev/null 2>&1; then
            # Linux
            printf "${GREEN}Opening coverage report...${NC}\n"
            xdg-open "$report_path"
        elif command -v start >/dev/null 2>&1; then
            # Windows (WSL)
            printf "${GREEN}Opening coverage report...${NC}\n"
            start "$report_path"
        else
            printf "${YELLOW}Cannot auto-open report. Please open manually: %s${NC}\n" "$report_path"
        fi
    else
        printf "${RED}Report generation may have failed. Report not found at: %s${NC}\n" "$report_path"
    fi
else
    printf "${RED}No coverage files found!${NC}\n"
    exit 1
fi