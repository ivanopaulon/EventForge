#!/bin/bash
# Script to fix missing translation keys using TranslationKeyGenerator

set -e

echo "╔═══════════════════════════════════════════════════════╗"
echo "║   Fix Missing Translation Keys                       ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo ""

# Check if we're in the project root
if [ ! -f "EventForge.sln" ]; then
    echo "❌ Error: Please run this script from the project root directory"
    exit 1
fi

# Step 1: Run validator to see current state
echo "Step 1: Running validator to check current state..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
dotnet run --project scripts/TranslationValidator || true
echo ""

# Step 2: Show what will be generated (dry-run)
echo "Step 2: Preview changes (dry-run)..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
dotnet run --project scripts/TranslationKeyGenerator -- --dry-run
echo ""

# Step 3: Ask for confirmation
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
read -p "Do you want to generate these missing keys? (y/N) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Cancelled by user"
    exit 1
fi
echo ""

# Step 4: Generate missing keys
echo "Step 4: Generating missing translation keys..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
dotnet run --project scripts/TranslationKeyGenerator
echo ""

# Step 5: Validate again to confirm
echo "Step 5: Validating translations after generation..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if dotnet run --project scripts/TranslationValidator; then
    echo ""
    echo "╔═══════════════════════════════════════════════════════╗"
    echo "║   ✓ Success!                                          ║"
    echo "╚═══════════════════════════════════════════════════════╝"
    echo ""
    echo "Next steps:"
    echo "1. Review the generated keys in EventForge.Client/wwwroot/i18n/"
    echo "2. Search for '[NEEDS TRANSLATION]' placeholders"
    echo "3. Replace placeholders with proper translations"
    echo "4. Run validator again to confirm: dotnet run --project scripts/TranslationValidator"
    echo ""
else
    echo ""
    echo "⚠️  Translation validation still has issues."
    echo "Please review the output above and fix any remaining problems."
    echo ""
    exit 1
fi
