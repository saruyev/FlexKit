#!/bin/bash
# FlexKit Test Project Management Scripts
# Bash script for managing test projects across the solution

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Script directory and solution paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_ROOT="$(dirname "$SCRIPT_DIR")"
TESTS_ROOT="$SOLUTION_ROOT/tests"
SRC_ROOT="$SOLUTION_ROOT/src"

# Functions for colored output
write_header() {
    echo -e "\n${CYAN}🔧 $1${NC}"
    echo "$(printf '=%.0s' $(seq 1 $((${#1} + 3))))"
}

write_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

write_error() {
    echo -e "${RED}❌ $1${NC}"
}

write_info() {
    echo -e "${YELLOW}ℹ️  $1${NC}"
}

# Display usage information
show_usage() {
    echo "Usage: $0 <action> [options]"
    echo ""
    echo "Actions:"
    echo "  create <name> [type]    Create a new test project"
    echo "  run [options]           Run tests"
    echo "  coverage                Generate coverage report"
    echo "  clean                   Clean test results"
    echo "  list                    List all test projects"
    echo ""
    echo "Options:"
    echo "  --type=<unit|integration|performance>  Test project type (default: unit)"
    echo "  --project=<name>                       Run tests for specific project"
    echo "  --filter=<expression>                  Test filter expression"
    echo "  --parallel                             Run tests in parallel"
    echo "  --all                                  Include all test types"
    echo ""
    echo "Examples:"
    echo "  $0 create MyProject.Feature unit"
    echo "  $0 run --type=unit --parallel"
    echo "  $0 run --project=Configuration --filter=\"Category=Unit\""
    echo "  $0 list"
}

# Get all test projects
get_test_projects() {
    if [[ ! -d "$TESTS_ROOT" ]]; then
        return
    fi

    find "$TESTS_ROOT" -name "*.csproj" -type f | while read -r project_file; do
        project_name=$(basename "$project_file" .csproj)
        project_dir=$(dirname "$project_file")
        relative_dir=${project_dir#$TESTS_ROOT/}

        test_type="unit"
        if [[ "$project_name" =~ \.Integration\.Tests$ ]]; then
            test_type="integration"
        elif [[ "$project_name" =~ \.Performance\.Tests$ ]]; then
            test_type="performance"
        fi

        source_project=$(echo "$project_name" | sed -E 's/\.(Tests|Integration\.Tests|Performance\.Tests)$//')

        echo "$project_name|$test_type|$project_file|$relative_dir|$source_project"
    done
}

# Create a new test project
create_test_project() {
    local name="$1"
    local type="${2:-unit}"

    write_header "Creating $type test project: $name"

    # Determine project name and path
    local suffix
    case "$type" in
        unit) suffix=".Tests" ;;
        integration) suffix=".Integration.Tests" ;;
        performance) suffix=".Performance.Tests" ;;
        *) write_error "Unknown test type: $type"; return 1 ;;
    esac

    local project_name="$name"
    if [[ ! "$name" =~ $suffix$ ]]; then
        project_name="$name$suffix"
    fi

    local project_dir="$TESTS_ROOT/$project_name"
    local project_file="$project_dir/$project_name.csproj"

    # Check if project already exists
    if [[ -f "$project_file" ]]; then
        write_error "Test project '$project_name' already exists"
        return 1
    fi

    # Create directory
    mkdir -p "$project_dir"

    # Copy template or create minimal project file
    local template_file="$TESTS_ROOT/ProjectTemplates/$type-test-template.csproj"
    if [[ -f "$template_file" ]]; then
        cp "$template_file" "$project_file"
        write_success "Created project file from template: $project_file"
    else
        cat > "$project_file" << EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- $type test specific settings -->
    <CoverageThreshold>100</CoverageThreshold>
  </PropertyGroup>

</Project>
EOF
        write_success "Created minimal project file: $project_file"
    fi

    # Create basic test class
    local test_class_name="${name%.*}Tests"
    local test_file="$project_dir/$test_class_name.cs"

    cat > "$test_file" << EOF
using FluentAssertions;
using Xunit;

namespace $project_name;

/// <summary>
/// $type tests for ${name%.*}.
/// </summary>
public class $test_class_name
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
EOF

    write_success "Created test class: $test_file"

    # Add to solution if possible
    if command -v dotnet >/dev/null 2>&1; then
        cd "$SOLUTION_ROOT"
        if dotnet sln add "$project_file" 2>/dev/null; then
            write_success "Added to solution"
        else
            write_info "Could not add to solution automatically"
        fi
    fi

    write_success "Test project '$project_name' created successfully!"
}

# Run tests
run_tests() {
    local project_filter="$1"
    local type_filter="$2"
    local test_filter="$3"
    local parallel="$4"

    write_header "Running tests"

    local test_projects
    test_projects=$(get_test_projects)

    if [[ -z "$test_projects" ]]; then
        write_error "No test projects found"
        return 1
    fi

    # Build test parameters
    local test_params=(
        "--configuration" "Release"
        "--logger" "trx"
        "--collect:XPlat Code Coverage"
    )

    if [[ -n "$test_filter" ]]; then
        test_params+=("--filter" "$test_filter")
    fi

    if [[ "$parallel" == "true" ]]; then
        test_params+=("--parallel")
    fi

    # Filter and run tests
    echo "$test_projects" | while IFS='|' read -r name type path dir source; do
        # Apply filters
        if [[ -n "$project_filter" && ! "$name" =~ $project_filter ]]; then
            continue
        fi

        if [[ -n "$type_filter" && "$type" != "$type_filter" ]]; then
            continue
        fi

        write_info "Running tests for $name ($type)"

        cd "$(dirname "$path")"
        if dotnet test "${test_params[@]}"; then
            write_success "$name tests passed"
        else
            write_error "$name tests failed"
        fi
    done
}

# Generate coverage report
generate_coverage_report() {
    write_header "Generating coverage report"

    local coverage_files
    coverage_files=$(find "$SOLUTION_ROOT/TestResults" -name "coverage.cobertura.xml" 2>/dev/null || true)

    if [[ -z "$coverage_files" ]]; then
        write_error "No coverage files found. Run tests first."
        return 1
    fi

    local file_count
    file_count=$(echo "$coverage_files" | wc -l)
    write_info "Found $file_count coverage files"

    write_info "Coverage files located at:"
    echo "$coverage_files" | while read -r file; do
        echo "  $file"
    done

    write_info "To generate HTML report, install ReportGenerator and run:"
    echo "dotnet tool install -g dotnet-reportgenerator-globaltool"
    echo "reportgenerator -reports:\"$(echo "$coverage_files" | tr '\n' ';' | sed 's/;$//')\" -targetdir:\"$SOLUTION_ROOT/TestResults/CoverageReport\" -reporttypes:Html"
}

# Clean test results
clean_test_results() {
    write_header "Cleaning test results"

    local test_results_path="$SOLUTION_ROOT/TestResults"
    if [[ -d "$test_results_path" ]]; then
        rm -rf "$test_results_path"
        write_success "Cleaned test results directory"
    else
        write_info "No test results to clean"
    fi

    # Clean bin/obj in test projects
    get_test_projects | while IFS='|' read -r name type path dir source; do
        local project_dir
        project_dir=$(dirname "$path")

        rm -rf "$project_dir/bin" "$project_dir/obj"
    done

    write_success "Cleaned all test project build artifacts"
}

# List all test projects
list_test_projects() {
    write_header "FlexKit Test Projects"

    local test_projects
    test_projects=$(get_test_projects)

    if [[ -z "$test_projects" ]]; then
        write_info "No test projects found in $TESTS_ROOT"
        return
    fi

    # Group by type
    local unit_tests integration_tests performance_tests
    unit_tests=$(echo "$test_projects" | grep "|unit|" || true)
    integration_tests=$(echo "$test_projects" | grep "|integration|" || true)
    performance_tests=$(echo "$test_projects" | grep "|performance|" || true)

    # Display each group
    if [[ -n "$unit_tests" ]]; then
        echo -e "\n${YELLOW}📁 UNIT TESTS:${NC}"
        echo "$unit_tests" | while IFS='|' read -r name type path dir source; do
            if [[ -f "$SRC_ROOT/$source/$source.csproj" ]]; then
                echo "  ✅ $name"
            else
                echo "  ❌ $name"
            fi
        done
    fi

    if [[ -n "$integration_tests" ]]; then
        echo -e "\n${YELLOW}📁 INTEGRATION TESTS:${NC}"
        echo "$integration_tests" | while IFS='|' read -r name type path dir source; do
            if [[ -f "$SRC_ROOT/$source/$source.csproj" ]]; then
                echo "  ✅ $name"
            else
                echo "  ❌ $name"
            fi
        done
    fi

    if [[ -n "$performance_tests" ]]; then
        echo -e "\n${YELLOW}📁 PERFORMANCE TESTS:${NC}"
        echo "$performance_tests" | while IFS='|' read -r name type path dir source; do
            if [[ -f "$SRC_ROOT/$source/$source.csproj" ]]; then
                echo "  ✅ $name"
            else
                echo "  ❌ $name"
            fi
        done
    fi

    # Summary
    local total_count unit_count integration_count performance_count
    total_count=$(echo "$test_projects" | wc -l)
    unit_count=$(echo "$unit_tests" | grep -c "." || echo "0")
    integration_count=$(echo "$integration_tests" | grep -c "." || echo "0")
    performance_count=$(echo "$performance_tests" | grep -c "." || echo "0")

    echo -e "\n📊 Summary:"
    echo "  Total test projects: $total_count"
    echo "  unit: $unit_count"
    echo "  integration: $integration_count"
    echo "  performance: $performance_count"
}

# Parse command line arguments
ACTION=""
PROJECT_NAME=""
TEST_TYPE="unit"
PROJECT_FILTER=""
TEST_FILTER=""
PARALLEL="false"
ALL="false"

while [[ $# -gt 0 ]]; do
    case $1 in
        create|run|coverage|clean|list)
            ACTION="$1"
            shift
            ;;
        --type=*)
            TEST_TYPE="${1#*=}"
            shift
            ;;
        --project=*)
            PROJECT_FILTER="${1#*=}"
            shift
            ;;
        --filter=*)
            TEST_FILTER="${1#*=}"
            shift
            ;;
        --parallel)
            PARALLEL="true"
            shift
            ;;
        --all)
            ALL="true"
            shift
            ;;
        --help|-h)
            show_usage
            exit 0
            ;;
        *)
            if [[ -z "$ACTION" ]]; then
                ACTION="$1"
            elif [[ "$ACTION" == "create" && -z "$PROJECT_NAME" ]]; then
                PROJECT_NAME="$1"
            elif [[ "$ACTION" == "create" && -n "$PROJECT_NAME" ]]; then
                TEST_TYPE="$1"
            fi
            shift
            ;;
    esac
done

# Execute action
case "$ACTION" in
    create)
        if [[ -z "$PROJECT_NAME" ]]; then
            write_error "Project name is required for create action"
            show_usage
            exit 1
        fi
        create_test_project "$PROJECT_NAME" "$TEST_TYPE"
        ;;
    run)
        if [[ "$ALL" == "true" ]]; then
            TEST_TYPE=""
        fi
        run_tests "$PROJECT_FILTER" "$TEST_TYPE" "$TEST_FILTER" "$PARALLEL"
        ;;
    coverage)
        generate_coverage_report
        ;;
    clean)
        clean_test_results
        ;;
    list)
        list_test_projects
        ;;
    "")
        list_test_projects
        ;;
    *)
        write_error "Unknown action: $ACTION"
        show_usage
        exit 1
        ;;
esac

# Check if the last command was successful
if [[ $? -eq 0 ]]; then
    write_success "Script completed successfully! 🎉"
else
    write_error "Script completed with errors"
    exit 1
fi

# Final exit with success
exit 0
