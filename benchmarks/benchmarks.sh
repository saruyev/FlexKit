#!/bin/bash

# benchmarks.sh - FlexKit Performance Benchmarks runner
# chmod +x benchmarks/benchmarks.sh
# Usage: ./benchmarks.sh [options]
# Examples:
#   ./benchmarks.sh
#   ./benchmarks.sh --project-path "FlexKit.Configuration.PerformanceTests"
#   ./benchmarks.sh --filter "MyBenchmark" --html --csv

set -euo pipefail

# Default values
FILTER=""
PROJECT_PATH=""
HTML=false
CSV=false
JSON=false
ALL=false
QUICK=false
OUTPUT="BenchmarkResults"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --filter)
            FILTER="$2"
            shift 2
            ;;
        --project-path)
            PROJECT_PATH="$2"
            shift 2
            ;;
        --html)
            HTML=true
            shift
            ;;
        --csv)
            CSV=true
            shift
            ;;
        --json)
            JSON=true
            shift
            ;;
        --all)
            ALL=true
            shift
            ;;
        --quick)
            QUICK=true
            shift
            ;;
        --output)
            OUTPUT="$2"
            shift 2
            ;;
        --help)
            cat << EOF
Usage: $0 [options]

Options:
  --filter FILTER         Filter for specific benchmarks
  --project-path PATH     Path to benchmark project directory
  --html                  Export results as HTML
  --csv                   Export results as CSV
  --json                  Export results as JSON
  --all                   Export all formats (HTML, CSV, JSON, Markdown)
  --quick                 Run in quick mode (dry run)
  --output DIR            Output directory for results. Default: BenchmarkResults
  --help                  Show this help message

Examples:
  $0
  $0 --project-path "FlexKit.Configuration.PerformanceTests"
  $0 --filter "MyBenchmark" --html --csv
  $0 --all --output "MyResults"
EOF
            exit 0
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

printf "${GREEN}FlexKit Performance Benchmarks${NC}\n"
printf "${GREEN}=============================================${NC}\n"

# Set working directory if ProjectPath is specified
if [[ -n "$PROJECT_PATH" ]]; then
    if [[ -d "$PROJECT_PATH" ]]; then
        cd "$PROJECT_PATH"
        printf "${CYAN}Changed to project directory: %s${NC}\n" "$PROJECT_PATH"
    else
        printf "${RED}Error: Project path does not exist: %s${NC}\n" "$PROJECT_PATH" >&2
        exit 1
    fi
fi

# Ensure we're in Release mode
printf "${YELLOW}Building project in Release mode...${NC}\n"
if ! dotnet build -c Release --no-restore; then
    printf "${RED}Error: Build failed. Please fix compilation errors before running benchmarks.${NC}\n" >&2
    exit 1
fi

# Prepare benchmark arguments
benchmark_args=("run" "-c" "Release")

# Add filter if specified
if [[ -n "$FILTER" ]]; then
    benchmark_args+=("--filter" "*$FILTER*")
    printf "${CYAN}Running benchmarks with filter: %s${NC}\n" "$FILTER"
fi

# Add exporters
exporters=()
if [[ "$HTML" == true ]]; then
    exporters+=("html")
fi
if [[ "$CSV" == true ]]; then
    exporters+=("csv")
fi
if [[ "$JSON" == true ]]; then
    exporters+=("json")
fi
if [[ "$ALL" == true ]]; then
    exporters+=("html" "csv" "json" "markdown")
fi

if [[ ${#exporters[@]} -gt 0 ]]; then
    # Join array elements with comma
    exporters_str=$(printf "%s," "${exporters[@]}")
    exporters_str=${exporters_str%,}  # Remove trailing comma
    
    benchmark_args+=("--exporters" "$exporters_str")
    printf "${CYAN}Exporting results to: %s${NC}\n" "$exporters_str"
fi

# Quick mode for development
if [[ "$QUICK" == true ]]; then
    benchmark_args+=("--job" "dry")
    printf "${YELLOW}Running in quick mode (dry run)${NC}\n"
fi

# Set output directory
if [[ -n "$OUTPUT" ]]; then
    benchmark_args+=("--artifacts" "$OUTPUT")
fi

echo
printf "${CYAN}Running benchmarks with arguments: %s${NC}\n" "${benchmark_args[*]}"
echo

# Run the benchmarks
if dotnet "${benchmark_args[@]}"; then
    echo
    printf "${GREEN}Benchmarks completed successfully!${NC}\n"
    
    if [[ ${#exporters[@]} -gt 0 ]]; then
        printf "${GREEN}Results exported to: %s/${NC}\n" "$OUTPUT"
        
        # List generated files
        if [[ -d "$OUTPUT" ]]; then
            printf "${YELLOW}Generated files:${NC}\n"
            find "$OUTPUT" -type f \( -name "*.html" -o -name "*.csv" -o -name "*.json" -o -name "*.md" \) -exec basename {} \; | sort | while read -r file; do
                printf "${WHITE}  - %s${NC}\n" "$file"
            done
        fi
    fi
else
    exit_code=$?
    printf "${RED}Error: Benchmarks failed with exit code %d${NC}\n" "$exit_code" >&2
    exit $exit_code
fi