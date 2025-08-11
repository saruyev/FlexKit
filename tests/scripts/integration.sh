#!/bin/bash

# integration.sh - Simple integration test runner with LivingDoc reporting
# Usage: ./integration.sh [project_path]

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m'

# Get project path from argument or use current directory
PROJECT_PATH="${1:-.}"

# Convert to absolute path
PROJECT_PATH=$(cd "$PROJECT_PATH" && pwd)

echo
echo "================================================"
echo "  FlexKit Integration Test Runner"
echo "================================================"
echo "Project Path: $PROJECT_PATH"
echo

# Check if project path exists
if [[ ! -d "$PROJECT_PATH" ]]; then
    echo -e "${RED}Error: Project path does not exist: $PROJECT_PATH${NC}"
    exit 1
fi

# Change to project directory
cd "$PROJECT_PATH"

# Find .csproj file
CSPROJ_FILE=$(find . -maxdepth 1 -name "*.csproj" | head -n 1)

if [[ -z "$CSPROJ_FILE" ]]; then
    echo -e "${RED}Error: No .csproj file found in $PROJECT_PATH${NC}"
    exit 1
fi

echo -e "${GREEN}Found project: $(basename "$CSPROJ_FILE")${NC}"
echo

# Build the project
echo -e "${CYAN}Building project...${NC}"
if dotnet build "$CSPROJ_FILE" --configuration Debug; then
    echo -e "${GREEN}✓ Build successful${NC}"
else
    echo -e "${RED}✗ Build failed${NC}"
    exit 1
fi

echo

# Run tests
echo -e "${CYAN}Running tests...${NC}"
if dotnet test "$CSPROJ_FILE" --configuration Debug --no-build --verbosity normal --results-directory "./TestResults" --logger "trx;LogFileName=TestResults.trx"; then
    echo -e "${GREEN}✓ Tests completed${NC}"
    TEST_RESULT="PASSED"
else
    echo -e "${YELLOW}⚠ Some tests may have failed${NC}"
    TEST_RESULT="COMPLETED"
fi

echo

# Generate LivingDoc report
echo -e "${CYAN}Generating LivingDoc report...${NC}"

# Check if livingdoc command is available
if ! command -v livingdoc >/dev/null 2>&1; then
    echo -e "${RED}Error: livingdoc command not found${NC}"
    echo -e "${WHITE}Install with: dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI${NC}"
    exit 1
fi

# Look for TestExecution.json
TEST_EXECUTION_FILE="./bin/Debug/net9.0/TestExecution.json"

if [[ -f "$TEST_EXECUTION_FILE" ]]; then
    echo -e "${GREEN}Found TestExecution.json${NC}"
    livingdoc feature-folder . --output-type HTML -t "$TEST_EXECUTION_FILE"
else
    echo -e "${YELLOW}TestExecution.json not found, generating report without test results${NC}"
    livingdoc feature-folder . --output-type HTML
fi

# Check if report was generated
REPORT_FILE="./LivingDoc.html"

if [[ -f "$REPORT_FILE" ]]; then
    echo -e "${GREEN}✓ LivingDoc report generated: $REPORT_FILE${NC}"
    
    # Open the report
    echo -e "${CYAN}Opening report...${NC}"
    if command -v open >/dev/null 2>&1; then
        # macOS
        open "$REPORT_FILE"
    elif command -v xdg-open >/dev/null 2>&1; then
        # Linux
        xdg-open "$REPORT_FILE"
    elif command -v start >/dev/null 2>&1; then
        # Windows
        start "$REPORT_FILE"
    else
        echo -e "${YELLOW}Cannot auto-open report. Please open manually: $REPORT_FILE${NC}"
    fi
else
    echo -e "${RED}✗ LivingDoc report generation failed${NC}"
    exit 1
fi

echo
echo "================================================"
echo -e "  ${GREEN}Integration Test Summary${NC}"
echo "================================================"
echo "Project: $(basename "$CSPROJ_FILE")"
echo "Status: $TEST_RESULT"
echo "Report: $REPORT_FILE"
echo "================================================"
