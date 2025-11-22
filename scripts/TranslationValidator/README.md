# Translation Validator

A CLI tool for validating that all translation files have the same keys.

## Purpose

This tool ensures translation completeness across all supported languages in EventForge. It compares all translation JSON files against a base language and reports:

- Missing keys (keys in base language but not in target language)
- Extra keys (keys in target language but not in base language)
- Total key counts per language

## Usage

### Basic Usage

```bash
dotnet run --project scripts/TranslationValidator
```

This validates all translation files in the default directory (`EventForge.Client/wwwroot/i18n`) using English (`en`) as the base language.

### Advanced Options

```bash
# Specify custom directory
dotnet run --project scripts/TranslationValidator -- --directory path/to/translations

# Use different base language
dotnet run --project scripts/TranslationValidator -- --base-language it

# Generate JSON report
dotnet run --project scripts/TranslationValidator -- --output missing-keys-report.json
```

## Exit Codes

- `0`: All translations are complete and consistent
- `1`: Missing or extra keys detected, or error occurred

## CI Integration

This tool is designed to be used in CI/CD pipelines. A non-zero exit code will fail the build, preventing incomplete translations from being merged.

## Example Output

```
═══════════════════════════════════════════════════════
  Translation Validation Report
═══════════════════════════════════════════════════════

  Base Language: en
  Base Keys: 1853
  Files Found: 2

  Language: en
    Total Keys: 1853
    ✓ Missing Keys: 0
    ✓ Extra Keys: 0

  Language: it
    Total Keys: 2144
    ✓ Missing Keys: 0
    ⚠ Extra Keys: 291
      + brand.confirmDelete
      + brand.createNew
      ... and 289 more

═══════════════════════════════════════════════════════

✗ Translation validation failed!
  Some files have missing or extra keys.
```

## JSON Report Format

When using `--output`, the tool generates a JSON report with the following structure:

```json
{
  "it": {
    "Language": "it",
    "TotalKeys": 2144,
    "MissingKeys": [],
    "ExtraKeys": [
      "brand.confirmDelete",
      "brand.createNew"
    ]
  }
}
```
