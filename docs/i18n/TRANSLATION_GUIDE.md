# EventForge Translation Guide

This guide explains how to work with translations in EventForge and maintain translation completeness across all supported languages.

## Overview

EventForge uses JSON-based i18n (internationalization) for managing translations. All translation files are located in:

```
EventForge.Client/wwwroot/i18n/
├── en.json  (English - base language)
├── it.json  (Italian)
└── [other languages].json
```

## Translation File Structure

Translation files use a nested JSON structure:

```json
{
  "common": {
    "save": "Save",
    "cancel": "Cancel",
    "delete": "Delete"
  },
  "product": {
    "title": "Products",
    "create": "Create Product",
    "edit": "Edit Product"
  }
}
```

Keys are referenced in code using dot notation: `common.save`, `product.title`, etc.

## Tools

EventForge provides two CLI tools for managing translations:

### 1. TranslationValidator

Validates that all translation files have the same keys.

**Usage:**
```bash
# Validate all translations
dotnet run --project scripts/TranslationValidator

# Generate a report
dotnet run --project scripts/TranslationValidator -- --output report.json

# Use different base language
dotnet run --project scripts/TranslationValidator -- --base-language it
```

**Exit Codes:**
- `0`: All translations are complete
- `1`: Missing or extra keys detected

### 2. TranslationKeyGenerator

Generates missing translation keys with placeholders.

**Usage:**
```bash
# Preview what would be generated (dry-run)
dotnet run --project scripts/TranslationKeyGenerator -- --dry-run

# Generate missing keys for all languages
dotnet run --project scripts/TranslationKeyGenerator

# Generate for specific language only
dotnet run --project scripts/TranslationKeyGenerator -- --target-language it

# Use custom placeholder
dotnet run --project scripts/TranslationKeyGenerator -- --placeholder "[TODO]"
```

## Workflow

### Adding New Translation Keys

1. **Add the key to the base language** (en.json):
   ```json
   {
     "newFeature": {
       "title": "New Feature Title",
       "description": "Description of the new feature"
     }
   }
   ```

2. **Run the generator** to create placeholders in other languages:
   ```bash
   dotnet run --project scripts/TranslationKeyGenerator
   ```

3. **Translate the placeholders**:
   - Open each language file
   - Search for `[NEEDS TRANSLATION]`
   - Replace with proper translation

4. **Validate completeness**:
   ```bash
   dotnet run --project scripts/TranslationValidator
   ```

### Quick Fix Script

Use the automated fix script for a guided workflow:

```bash
./scripts/fix-missing-translations.sh
```

This script will:
1. Show current validation status
2. Preview changes (dry-run)
3. Ask for confirmation
4. Generate missing keys
5. Validate the results

## CI/CD Integration

### Automated Validation

The CI/CD pipeline automatically validates translations on:
- Pull requests that modify translation files
- Pushes to main/develop branches

**Workflow:** `.github/workflows/translation-validation.yml`

### PR Comments

If validation fails, the CI will post a comment on the PR with:
- List of missing keys per language
- List of extra keys per language
- Instructions on how to fix

### Blocking Merges

PRs with incomplete translations will fail the build and cannot be merged until fixed.

## Best Practices

### For Developers

1. **Always use the base language** (en.json) as the source of truth
2. **Never delete keys** without updating all language files
3. **Use descriptive key names** following the nested structure
4. **Add context comments** for complex translations in the base language
5. **Run validator before committing** translation changes

### For Translators

1. **Never remove the key structure** - only translate values
2. **Preserve placeholders** like `{0}`, `{1}` for dynamic content
3. **Maintain HTML tags** if present in the original text
4. **Keep translations concise** - UI space is limited
5. **Ask for context** if the meaning is unclear

### Key Naming Conventions

- Use lowercase with camelCase for leaf keys
- Group related translations under common prefixes
- Keep key names descriptive but concise

**Good:**
```json
{
  "product": {
    "createSuccess": "Product created successfully",
    "updateSuccess": "Product updated successfully",
    "deleteConfirm": "Are you sure you want to delete this product?"
  }
}
```

**Avoid:**
```json
{
  "msg1": "Product created successfully",
  "msg2": "Product updated successfully",
  "q1": "Are you sure you want to delete this product?"
}
```

## Troubleshooting

### Missing Keys

**Problem:** Validator reports missing keys in a language file.

**Solution:**
```bash
# Generate missing keys
dotnet run --project scripts/TranslationKeyGenerator -- --target-language it

# Or use the quick fix script
./scripts/fix-missing-translations.sh
```

### Extra Keys

**Problem:** Validator reports extra keys (keys that exist in a language but not in base).

**Solution:**
1. Check if the key should exist in the base language
2. If yes, add it to en.json
3. If no, remove it from the language file

### Merge Conflicts

**Problem:** Git merge conflicts in translation files.

**Solution:**
1. Resolve conflicts in base language file first
2. Run the generator to update other languages
3. Manually review and fix any translation conflicts

### Format Issues

**Problem:** JSON syntax errors or formatting inconsistencies.

**Solution:**
- Use a JSON validator/formatter
- The generator preserves formatting when adding keys
- Always use UTF-8 encoding

## Reference

### Supported Languages

Current languages:
- **en** (English) - Base language
- **it** (Italian)

To add a new language:
1. Create `[language-code].json` in `EventForge.Client/wwwroot/i18n/`
2. Run the generator to populate it
3. Translate all placeholders
4. Update this documentation

### Dynamic Content

Use numbered placeholders for dynamic content:

```json
{
  "greeting": "Hello, {0}!",
  "itemsCount": "You have {0} items in your cart"
}
```

In code:
```csharp
Localizer["greeting", userName]
Localizer["itemsCount", itemCount]
```

### Pluralization

For plural forms, create separate keys:

```json
{
  "itemSingular": "{0} item",
  "itemPlural": "{0} items"
}
```

### HTML Content

Avoid HTML in translations when possible. If needed, use simple tags:

```json
{
  "richMessage": "Click <strong>here</strong> to continue"
}
```

## Additional Resources

- [TranslationValidator README](../../scripts/TranslationValidator/README.md)
- [TranslationKeyGenerator README](../../scripts/TranslationKeyGenerator/README.md)
- [CI Workflow](../../.github/workflows/translation-validation.yml)

## Support

For questions or issues with translations:
1. Check this guide first
2. Run the validator to identify specific problems
3. Open an issue on GitHub with the validator output
