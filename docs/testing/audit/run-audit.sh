#!/bin/bash

# EventForge Audit Script
# This script runs the automated audit tool and generates a comprehensive report

echo "EventForge Backend Refactoring Audit"
echo "===================================="
echo ""

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "Project Root: $PROJECT_ROOT"
echo "Audit Tool: $SCRIPT_DIR"
echo ""

# Build the audit tool
echo "Building audit tool..."
cd "$SCRIPT_DIR"
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Failed to build audit tool"
    exit 1
fi

echo "✅ Audit tool built successfully"
echo ""

# Run the audit
echo "Running automated audit..."
dotnet run --configuration Release -- "$PROJECT_ROOT"

if [ $? -ne 0 ]; then
    echo "❌ Audit failed"
    exit 1
fi

echo ""
echo "✅ Audit completed successfully!"
echo ""
echo "📄 Report Location: $PROJECT_ROOT/audit/AUDIT_REPORT.md"
echo ""
echo "Next steps:"
echo "1. Review the generated audit report"
echo "2. Address critical and high priority issues first"
echo "3. Work through the actionable checklist"
echo "4. Re-run the audit after making changes"