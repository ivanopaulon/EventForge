#!/bin/bash
# coverage-report.sh - Local Coverage Report Generator for Onda 4
# Runs tests with coverage, generates reports, checks thresholds, and opens browser

set -e

echo "═══════════════════════════════════════════════════════"
echo "  EventForge Coverage Report - Onda 4"
echo "═══════════════════════════════════════════════════════"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Get script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TEST_RESULTS="$PROJECT_ROOT/TestResults"
COVERAGE_DIR="$TEST_RESULTS/coverage"

echo -e "${BLUE}→${NC} Project root: $PROJECT_ROOT"
echo ""

# Step 1: Clean previous results
echo -e "${BLUE}→${NC} Cleaning previous test results..."
if [ -d "$TEST_RESULTS" ]; then
    rm -rf "$TEST_RESULTS"
    echo -e "${GREEN}✓${NC} Previous results cleaned"
else
    echo -e "${YELLOW}⚠${NC} No previous results to clean"
fi
echo ""

# Step 2: Run tests with coverage
echo -e "${BLUE}→${NC} Running tests with coverage collection..."
cd "$PROJECT_ROOT"
dotnet test EventForge.sln \
    --configuration Release \
    --collect:"XPlat Code Coverage" \
    --results-directory "$TEST_RESULTS" \
    --verbosity normal

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✓${NC} Tests completed successfully"
else
    echo ""
    echo -e "${RED}✗${NC} Tests failed"
    exit 1
fi
echo ""

# Step 3: Check if ReportGenerator is installed
echo -e "${BLUE}→${NC} Checking ReportGenerator installation..."
if ! command -v reportgenerator &> /dev/null; then
    echo -e "${YELLOW}⚠${NC} ReportGenerator not found. Installing..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
    echo -e "${GREEN}✓${NC} ReportGenerator installed"
else
    echo -e "${GREEN}✓${NC} ReportGenerator already installed"
fi
echo ""

# Step 4: Generate coverage reports
echo -e "${BLUE}→${NC} Generating coverage reports..."
reportgenerator \
    -reports:"$TEST_RESULTS/**/coverage.cobertura.xml" \
    -targetdir:"$COVERAGE_DIR" \
    -reporttypes:"Html;JsonSummary;TextSummary;Cobertura"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓${NC} Coverage reports generated"
else
    echo -e "${RED}✗${NC} Failed to generate coverage reports"
    exit 1
fi
echo ""

# Step 5: Display text summary
if [ -f "$COVERAGE_DIR/Summary.txt" ]; then
    echo -e "${BLUE}→${NC} Coverage Summary:"
    echo ""
    cat "$COVERAGE_DIR/Summary.txt"
    echo ""
fi

# Step 6: Check coverage thresholds
echo -e "${BLUE}→${NC} Checking coverage thresholds (Onda 4 requirements)..."
echo ""

cd "$PROJECT_ROOT"
dotnet run --project scripts/CoverageChecker/CoverageChecker.csproj -- \
    --file "$COVERAGE_DIR/Summary.json" \
    --min-line-coverage 80 \
    --min-branch-coverage 75 \
    --max-missing 10

THRESHOLD_EXIT_CODE=$?
echo ""

# Step 7: Open HTML report in browser
if [ -f "$COVERAGE_DIR/index.html" ]; then
    echo -e "${BLUE}→${NC} Opening coverage report in browser..."
    
    # Detect OS and open browser accordingly
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        if command -v xdg-open &> /dev/null; then
            xdg-open "$COVERAGE_DIR/index.html" &> /dev/null &
        elif command -v sensible-browser &> /dev/null; then
            sensible-browser "$COVERAGE_DIR/index.html" &> /dev/null &
        else
            echo -e "${YELLOW}⚠${NC} Could not detect browser. Report location:"
            echo "   file://$COVERAGE_DIR/index.html"
        fi
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        open "$COVERAGE_DIR/index.html"
    elif [[ "$OSTYPE" == "cygwin" ]] || [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
        start "$COVERAGE_DIR/index.html"
    else
        echo -e "${YELLOW}⚠${NC} Could not detect OS. Report location:"
        echo "   file://$COVERAGE_DIR/index.html"
    fi
    
    echo -e "${GREEN}✓${NC} Report available at: file://$COVERAGE_DIR/index.html"
else
    echo -e "${YELLOW}⚠${NC} HTML report not found"
fi
echo ""

# Step 8: Display final result
echo "═══════════════════════════════════════════════════════"
if [ $THRESHOLD_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✓ Coverage report complete - All thresholds passed${NC}"
else
    echo -e "${RED}✗ Coverage report complete - Thresholds not met${NC}"
fi
echo "═══════════════════════════════════════════════════════"
echo ""
echo "Report location: $COVERAGE_DIR"
echo ""

exit $THRESHOLD_EXIT_CODE
