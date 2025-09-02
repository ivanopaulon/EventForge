# Translation Guide - EventForge

This guide explains how to add or update translations for the EventForge Blazor client application.

## Overview

EventForge uses a custom translation service (`TranslationService`) that loads translations from JSON files located in `EventForge.Client/wwwroot/i18n/`.

## Supported Languages

Currently supported languages:
- **Italian (it)** - Default language
- **English (en)**
- **Spanish (es)**
- **French (fr)**

## Translation File Structure

Translation files are located in: `EventForge.Client/wwwroot/i18n/`

Each language has its own JSON file:
- `it.json` - Italian (default)
- `en.json` - English
- `es.json` - Spanish  
- `fr.json` - French

### JSON Structure

```json
{
  "common": {
    "yes": "Sì",
    "no": "No",
    "save": "Salva",
    "cancel": "Annulla"
  },
  "navigation": {
    "home": "Home",
    "dashboard": "Dashboard",
    "users": "Utenti"
  },
  "auth": {
    "login": "Accedi",
    "username": "Nome utente",
    "password": "Password"
  }
}
```

## Adding a New Language

1. **Create a new JSON file** in `EventForge.Client/wwwroot/i18n/` with the language code (e.g., `de.json` for German)

2. **Copy the structure** from an existing file (e.g., `en.json`) and translate all values

3. **Update the TranslationService** to include the new language:
   ```csharp
   // In EventForge.Client/Services/TranslationService.cs
   private readonly Dictionary<string, string> _availableLanguages = new()
   {
       { "it", "Italiano" },
       { "en", "English" },
       { "es", "Español" },
       { "fr", "Français" },
       { "de", "Deutsch" } // Add new language here
   };
   ```

4. **Test the new language** by running the application and selecting it from the language selector

## Updating Existing Translations

1. **Edit the appropriate JSON file** in `EventForge.Client/wwwroot/i18n/`

2. **Maintain the same structure** - only change the translation values, not the keys

3. **Test your changes** by running the application:
   ```bash
   cd EventForge.Client
   dotnet run
   ```

## Translation Keys

### Usage in Components

Use the translation service in Blazor components:

```csharp
@inject ITranslationService TranslationService

<MudButton>@TranslationService.GetTranslation("common.save")</MudButton>
```

### Nested Keys

Access nested translations using dot notation:
- `common.save` → `"Salva"`
- `auth.login` → `"Accedi"`
- `navigation.dashboard` → `"Dashboard"`

### Fallback Mechanism

The system automatically falls back to English if:
1. A translation key is missing in the current language
2. The language file cannot be loaded
3. There's an error in the JSON structure

## Validation

### JSON Validation

Always validate your JSON files before committing:

```bash
# Linux/Mac
python3 -m json.tool EventForge.Client/wwwroot/i18n/your-file.json

# Windows
python -m json.tool EventForge.Client/wwwroot/i18n/your-file.json
```

### Testing Translations

1. Run the application: `cd EventForge.Client && dotnet run`
2. Open browser at `http://localhost:5048`
3. Test language switching if available
4. Check browser console for any translation warnings

## Best Practices

1. **Keep keys consistent** across all language files
2. **Use descriptive key names** (e.g., `validation.required` instead of `val1`)
3. **Group related translations** (e.g., all form-related keys under `form.*`)
4. **Test thoroughly** after adding new translations
5. **Use placeholders** for dynamic content: `"Hello {0}"` with `GetTranslation("greeting", userName)`

## Troubleshooting

### Translation Not Loading
- Check browser console for errors
- Verify JSON syntax is valid
- Ensure the file is in the correct location
- Check that the language code matches exactly

### Fallback Not Working
- Verify English translations exist for all keys
- Check that the fallback mechanism is implemented correctly
- Ensure the TranslationService is properly initialized

## File Locations

- **Translation files**: `EventForge.Client/wwwroot/i18n/*.json`
- **Translation service**: `EventForge.Client/Services/TranslationService.cs`
- **Language selector**: `EventForge.Client/Shared/Components/LanguageSelector.razor`

## Future Enhancements

The translation system is designed to support future features such as:
- API-based translation management for SuperAdmin users
- Runtime translation updates
- Translation export/import functionality
- Pluralization support