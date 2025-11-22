# Translation Key Generator

A CLI tool for generating missing translation keys with placeholders.

## Purpose

This tool automatically generates missing translation keys in target languages by comparing them against a base language. It creates placeholder entries that need to be translated by human translators.

## Features

- Compares all translation files against a base language
- Generates missing keys with placeholder text
- Includes base language text as reference for translators
- Supports dry-run mode to preview changes
- Can target specific languages or update all at once

## Usage

### Dry Run (Preview Changes)

```bash
dotnet run --project scripts/TranslationKeyGenerator -- --dry-run
```

This shows what keys would be added without modifying any files.

### Generate Missing Keys

```bash
# Update all languages
dotnet run --project scripts/TranslationKeyGenerator

# Update specific language only
dotnet run --project scripts/TranslationKeyGenerator -- --target-language it

# Use custom placeholder
dotnet run --project scripts/TranslationKeyGenerator -- --placeholder "[TODO]"
```

### Advanced Options

```bash
# Specify custom directory
dotnet run --project scripts/TranslationKeyGenerator -- --directory path/to/translations

# Use different base language
dotnet run --project scripts/TranslationKeyGenerator -- --base-language it
```

## Output Format

Generated keys use this format:
```
"[NEEDS TRANSLATION] <base language text>"
```

For example:
```json
{
  "accessibility": {
    "skipToContent": "[NEEDS TRANSLATION] Skip to main content"
  }
}
```

This makes it easy for translators to:
1. Identify keys that need translation
2. See the base language text as reference
3. Replace the placeholder with proper translation

## Example Output

```
═══════════════════════════════════════════════════════
  Translation Key Generator
═══════════════════════════════════════════════════════

  Base Language: en
  Base Keys: 1853
  Placeholder: [NEEDS TRANSLATION]

  Language: it
    Missing Keys: 292
    ✓ Added 292 keys

═══════════════════════════════════════════════════════

✓ Successfully generated 292 missing translation keys!
```

## Workflow

1. **Run dry-run** to preview changes
   ```bash
   dotnet run --project scripts/TranslationKeyGenerator -- --dry-run
   ```

2. **Generate keys** if preview looks correct
   ```bash
   dotnet run --project scripts/TranslationKeyGenerator
   ```

3. **Review generated keys** in translation files
   - Search for `[NEEDS TRANSLATION]` placeholders
   - Replace with proper translations

4. **Validate** completeness with TranslationValidator
   ```bash
   dotnet run --project scripts/TranslationValidator
   ```

## Integration with TranslationValidator

This tool works together with TranslationValidator:

1. **TranslationValidator** identifies missing keys
2. **TranslationKeyGenerator** creates placeholder entries
3. Translators replace placeholders with real translations
4. **TranslationValidator** confirms completeness

## Notes

- Always run in dry-run mode first to review changes
- The tool preserves existing translations and only adds missing keys
- Generated files maintain JSON formatting and structure
- Placeholder text can be customized for your workflow
