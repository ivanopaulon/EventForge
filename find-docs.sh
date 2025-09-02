#!/bin/bash

# EventForge Documentation Reorganization Helper
# This script helps locate moved documentation files

echo "📚 EventForge Documentation has been reorganized!"
echo "================================================"
echo ""
echo "All documentation files have been moved to organized directories under docs/"
echo ""
echo "🔍 Quick Navigation:"
echo "  📋 Main Index: docs/README.md"
echo "  🚀 Getting Started: docs/core/getting-started.md"
echo "  🏗️ Backend Docs: docs/backend/"
echo "  🎨 Frontend Docs: docs/frontend/"
echo "  🧪 Testing Docs: docs/testing/"
echo "  🚀 Deployment: docs/deployment/"
echo "  🔧 Features: docs/features/"
echo "  📊 Migration Reports: docs/migration/"
echo ""
echo "📄 File Mapping: docs/FILE_MAPPING.md"
echo ""

# If user provides a filename, help them find it
if [ $# -eq 1 ]; then
    filename="$1"
    echo "🔍 Looking for: $filename"
    echo ""
    
    # Search in docs directory
    found_files=$(find docs/ -name "*$filename*" -type f 2>/dev/null)
    
    if [ -n "$found_files" ]; then
        echo "✅ Found in new location(s):"
        echo "$found_files"
    else
        echo "❌ File not found. Check docs/FILE_MAPPING.md for complete mapping."
    fi
    echo ""
fi

echo "💡 Tip: Use 'docs/README.md' as your starting point for navigation!"