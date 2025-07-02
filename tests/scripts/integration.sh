#!/bin/bash

# integration.sh - Simple integration test runner for FlexKit projects
# chmod +x tests/scripts/integration.sh
# Usage: ./integration.sh [options]
# Examples:
#   ./integration.sh
#   ./integration.sh --filter "Category=Database" --verbosity detailed
#   ./integration.sh --configuration Release --collect-coverage

set -euo pipefail

# Default values
TEST_FILTER=""
CONFIGURATION="Debug"
VERBOSITY="normal"
COLLECT_COVERAGE=false
OUTPUT_DIR="./TestResults"
PROJECT_PATTERN="*.IntegrationTests.csproj"
PROJECT_PATH="."

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --filter)
            TEST_FILTER="$2"
            shift 2
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --verbosity)
            VERBOSITY="$2"
            shift 2
            ;;
        --collect-coverage)
            COLLECT_COVERAGE=true
            shift
            ;;
        --output-dir)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --project-pattern)
            PROJECT_PATTERN="$2"
            shift 2
            ;;
        --project-path)
            PROJECT_PATH="$2"
            shift 2
            ;;
        --help)
            cat << EOF
Usage: $0 [options]

Options:
  --filter FILTER         Filter for specific tests (e.g., "Category=Integration")
  --configuration CONFIG  Build configuration (Debug/Release). Default: Debug
  --verbosity LEVEL       Test verbosity (quiet/minimal/normal/detailed/diagnostic). Default: normal
  --collect-coverage      Collect code coverage
  --output-dir DIR        Output directory for test results. Default: ./TestResults
  --project-pattern PATTERN Pattern to match test projects. Default: "*.IntegrationTests.csproj"
  --project-path PATH     Path to search for projects. Default: current directory
  --help                  Show this help message

Examples:
  $0
  $0 --filter "Category=Database" --verbosity detailed
  $0 --configuration Release --collect-coverage
EOF
            exit 0
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

write_header() {
    echo
    printf "${CYAN}============================================================${NC}\n"
    printf "${CYAN}  %s${NC}\n" "$1"
    printf "${CYAN}============================================================${NC}\n"
}

find_test_projects() {
    write_header "Finding Integration Test Projects"
    
    local projects=()
    while IFS= read -r -d '' project; do
        projects+=("$project")
    done < <(find "$PROJECT_PATH" -name "$PROJECT_PATTERN" -type f -print0)
    
    if [[ ${#projects[@]} -eq 0 ]]; then
        printf "${RED}No integration test projects found matching: %s${NC}\n" "$PROJECT_PATTERN"
        exit 1
    fi
    
    printf "${GREEN}Found %d test project(s):${NC}\n" "${#projects[@]}"
    for project in "${projects[@]}"; do
        printf "${WHITE}  • %s${NC}\n" "$(basename "$project")"
    done
    
    printf "%s\n" "${projects[@]}"
}

run_tests() {
    local projects=("$@")
    write_header "Running Integration Tests"
    
    # Create output directory
    mkdir -p "$OUTPUT_DIR"
    
    local all_passed=true
    local total_projects=${#projects[@]}
    local passed_projects=0
    
    for project in "${projects[@]}"; do
        echo
        printf "${YELLOW}Running tests for: %s${NC}\n" "$(basename "$project")"
        printf "${GRAY}Project path: %s${NC}\n" "$project"
        
        # Build test arguments
        local test_args=(
            "test"
            "$project"
            "--configuration" "$CONFIGURATION"
            "--verbosity" "$VERBOSITY"
            "--logger" "trx"
            "--results-directory" "$OUTPUT_DIR"
        )
        
        if [[ -n "$TEST_FILTER" ]]; then
            test_args+=("--filter" "$TEST_FILTER")
        fi
        
        if [[ "$COLLECT_COVERAGE" == true ]]; then
            test_args+=("--collect" "XPlat Code Coverage")
        fi
        
        # Run the test
        printf "${GRAY}Command: dotnet %s${NC}\n" "${test_args[*]}"
        
        if dotnet "${test_args[@]}"; then
            printf "${GREEN}✓ Tests PASSED for %s${NC}\n" "$(basename "$project")"
            ((passed_projects++))
        else
            printf "${RED}✗ Tests FAILED for %s${NC}\n" "$(basename "$project")"
            all_passed=false
        fi
    done
    
    echo "$all_passed,$total_projects,$passed_projects"
}

generate_html_report() {
    write_header "HTML Report Generation Skipped"
    printf "${YELLOW}HTML report generation has been disabled.${NC}\n"
    printf "${WHITE}TRX files are available in: %s${NC}\n" "$OUTPUT_DIR"
}

show_summary() {
    local all_passed=$1
    local total_projects=$2
    local passed_projects=$3
    local failed_projects=$((total_projects - passed_projects))
    
    write_header "Test Results Summary"
    
    printf "${WHITE}Total Projects:  %d${NC}\n" "$total_projects"
    printf "${GREEN}Passed:          %d${NC}\n" "$passed_projects"
    if [[ $failed_projects -eq 0 ]]; then
        printf "${GREEN}Failed:          %d${NC}\n" "$failed_projects"
    else
        printf "${RED}Failed:          %d${NC}\n" "$failed_projects"
    fi
    
    echo
    if [[ "$all_passed" == true ]]; then
        printf "${GREEN}🎉 ALL TESTS PASSED! 🎉${NC}\n"
    else
        printf "${RED}❌ SOME TESTS FAILED ❌${NC}\n"
    fi
    
    echo
    printf "${GRAY}Test results saved to: %s${NC}\n" "$OUTPUT_DIR"
}

main() {
    local start_time=$(date +%s)
    
    write_header "FlexKit Integration Test Runner"
    printf "${WHITE}Configuration: %s${NC}\n" "$CONFIGURATION"
    printf "${WHITE}Verbosity:     %s${NC}\n" "$VERBOSITY"
    printf "${WHITE}Output Dir:    %s${NC}\n" "$OUTPUT_DIR"
    printf "${WHITE}Pattern:       %s${NC}\n" "$PROJECT_PATTERN"
    printf "${WHITE}Search Path:   %s${NC}\n" "$PROJECT_PATH"
    if [[ -n "$TEST_FILTER" ]]; then
        printf "${WHITE}Filter:        %s${NC}\n" "$TEST_FILTER"
    fi
    if [[ "$COLLECT_COVERAGE" == true ]]; then
        printf "${WHITE}Coverage:      Enabled${NC}\n"
    fi
    
    local projects
    projects=$(find_test_projects)
    readarray -t project_array <<< "$projects"
    
    local results
    results=$(run_tests "${project_array[@]}")
    IFS=',' read -r all_passed total_projects passed_projects <<< "$results"
    
    generate_html_report
    show_summary "$all_passed" "$total_projects" "$passed_projects"
    
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    local hours=$((duration / 3600))
    local minutes=$(((duration % 3600) / 60))
    local seconds=$((duration % 60))
    printf "${GRAY}Total execution time: %02d:%02d:%02d${NC}\n" "$hours" "$minutes" "$seconds"
    
    if [[ "$all_passed" == true ]]; then
        exit 0
    else
        exit 1
    fi
}

# Run main function with error handling
if ! main "$@"; then
    printf "${RED}Script failed${NC}\n" >&2
    exit 1
fi