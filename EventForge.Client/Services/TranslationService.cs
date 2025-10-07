using Microsoft.JSInterop;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Interface for translation service.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Gets the current language.
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Gets the last missing translation key for debugging purposes.
    /// </summary>
    string? LastMissingKey { get; }

    /// <summary>
    /// Event triggered when language changes.
    /// </summary>
    event EventHandler<string>? LanguageChanged;

    /// <summary>
    /// Sets the current language and persists it.
    /// </summary>
    /// <param name="language">Language code (it, en, es, fr)</param>
    Task SetLanguageAsync(string language);

    /// <summary>
    /// Gets a translation by key, with optional fallback.
    /// </summary>
    /// <param name="key">Translation key (e.g., "common.save")</param>
    /// <param name="fallback">Fallback text if translation is not found</param>
    /// <returns>Translated text or fallback or key</returns>
    string GetTranslation(string key, string? fallback = null);

    /// <summary>
    /// Gets a translation by key with parameters for string formatting.
    /// </summary>
    /// <param name="key">Translation key</param>
    /// <param name="parameters">Parameters for string formatting</param>
    /// <returns>Formatted translated text</returns>
    string GetTranslation(string key, params object[] parameters);

    /// <summary>
    /// Gets a translation with formatted string interpolation using a fallback template.
    /// This method provides the centralized wrapper required by issue #106 for all interpolated strings.
    /// </summary>
    /// <param name="key">Translation key</param>
    /// <param name="defaultTemplate">Default template with placeholders like {0}, {1}, etc.</param>
    /// <param name="args">Arguments to substitute in the template</param>
    /// <returns>Formatted translated text</returns>
    string GetTranslationFormatted(string key, string defaultTemplate, params object[] args);

    /// <summary>
    /// Gets all available languages.
    /// </summary>
    /// <returns>Dictionary of language codes and display names</returns>
    Dictionary<string, string> GetAvailableLanguages();

    /// <summary>
    /// Loads translations from API (for SuperAdmin management).
    /// </summary>
    Task LoadTranslationsFromApiAsync();

    /// <summary>
    /// Checks if a translation key exists.
    /// </summary>
    /// <param name="key">Translation key</param>
    /// <returns>True if the key exists</returns>
    bool HasTranslation(string key);
}

/// <summary>
/// Implementation of translation service with support for JSON files and API loading.
/// 
/// This service follows Blazor and .NET HttpClient best practices:
/// - HttpClient BaseAddress is configured once in Program.cs via DI
/// - No dynamic BaseAddress setting after initialization
/// - Uses pre-configured named HttpClient instances
/// - Provides graceful fallback for missing translations with logging
/// 
/// For future maintainers:
/// - HttpClient configuration is done in Program.cs using AddHttpClient()
/// - StaticClient is pre-configured with BaseAddress for static file access
/// - ApiClient is pre-configured for server API communication
/// - Dynamic language switching is supported without creating new HttpClient instances
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<TranslationService> _logger;

    private Dictionary<string, object> _translations = new();
    private Dictionary<string, object> _defaultLanguageTranslations = new();
    private const string DEFAULT_LANGUAGE = "it";
    private const string LANGUAGE_PREFERENCE_KEY = "eventforge_language";

    private readonly Dictionary<string, string> _availableLanguages = new()
    {
        { "it", "Italiano" },
        { "en", "English" }
    };

    public event EventHandler<string>? LanguageChanged;

    public string CurrentLanguage { get; private set; } = "it";
    public string? LastMissingKey { get; private set; }

    /// <summary>
    /// Initializes the TranslationService with pre-configured HttpClient instances.
    /// HttpClient configuration (including BaseAddress) is handled in Program.cs.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating configured HttpClient instances</param>
    /// <param name="jsRuntime">JavaScript runtime for local storage operations</param>
    /// <param name="logger">Logger for debugging and warning messages</param>
    public TranslationService(
        IHttpClientFactory httpClientFactory,
        IJSRuntime jsRuntime,
        ILogger<TranslationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the translation service and loads the user's preferred language.
    /// Note: HttpClient configuration (including BaseAddress) is handled in Program.cs
    /// following Blazor and .NET best practices.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogDebug("Initializing TranslationService with default language: {DefaultLanguage}", DEFAULT_LANGUAGE);

            // Load default language translations first for fallback
            // This ensures we always have some translations available
            await LoadTranslationsForLanguageAsync(DEFAULT_LANGUAGE, _defaultLanguageTranslations);

            // Try to get saved language preference from browser storage
            var savedLanguage = await GetSavedLanguageAsync();
            if (!string.IsNullOrEmpty(savedLanguage) && _availableLanguages.ContainsKey(savedLanguage))
            {
                CurrentLanguage = savedLanguage;
                _logger.LogDebug("Using saved language preference: {Language}", savedLanguage);
            }
            else
            {
                _logger.LogDebug("No valid saved language found, using default: {Language}", DEFAULT_LANGUAGE);
            }

            // Load translations for current language
            await LoadTranslationsAsync(CurrentLanguage);

            _logger.LogInformation("TranslationService initialized successfully. Current language: {Language}", CurrentLanguage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing translation service");

            // Fallback to default language with error recovery
            CurrentLanguage = DEFAULT_LANGUAGE;
            try
            {
                await LoadTranslationsAsync(CurrentLanguage);
                _logger.LogWarning("Successfully recovered using default language: {Language}", CurrentLanguage);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Critical error: Cannot load fallback language {Language}. Translation service may not work properly.", CurrentLanguage);

                // Create empty translations to prevent further errors
                _translations.Clear();
                _defaultLanguageTranslations.Clear();
            }
        }
    }

    /// <summary>
    /// Sets the current language and loads the corresponding translations.
    /// 
    /// This method demonstrates how dynamic language switching works:
    /// - Uses existing HttpClient instances (no new instance creation needed)
    /// - Maintains HttpClient best practices by not modifying BaseAddress
    /// - Provides graceful error handling with rollback to previous language
    /// - Persists language preference for future sessions
    /// 
    /// For extending dynamic language switching:
    /// - This pattern can be extended to support user-specific languages
    /// - Additional language packs can be loaded on-demand using the same HttpClient instances
    /// - Caching mechanisms can be added without affecting HttpClient configuration
    /// </summary>
    /// <param name="language">Language code (it, en, es, fr)</param>
    public async Task SetLanguageAsync(string language)
    {
        if (!_availableLanguages.ContainsKey(language))
        {
            _logger.LogWarning("Attempt to set unsupported language: {Language}. Available languages: {AvailableLanguages}",
                language, string.Join(", ", _availableLanguages.Keys));
            return;
        }

        var previousLanguage = CurrentLanguage;
        CurrentLanguage = language;

        try
        {
            _logger.LogDebug("Changing language from {PreviousLanguage} to {NewLanguage}", previousLanguage, language);

            // Load new translations using existing HttpClient configuration
            // No need to create new HttpClient instances or modify BaseAddress
            await LoadTranslationsAsync(language);

            // Save preference to browser storage
            await SaveLanguagePreferenceAsync(language);

            // Notify components about language change
            LanguageChanged?.Invoke(this, language);

            _logger.LogInformation("Language successfully changed from {PreviousLanguage} to {NewLanguage}", previousLanguage, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing language from {PreviousLanguage} to {NewLanguage}. Rolling back to previous language.",
                previousLanguage, language);

            // Revert to previous language on error
            CurrentLanguage = previousLanguage;
            throw new InvalidOperationException($"Failed to change language to {language}. Reverted to {previousLanguage}.", ex);
        }
    }

    public string GetTranslation(string key, string? fallback = null)
    {
        try
        {
            // Ensure we never return null or empty string
            if (string.IsNullOrWhiteSpace(key))
            {
                var result = !string.IsNullOrWhiteSpace(fallback) ? fallback : "[EMPTY_KEY]";
                Console.WriteLine($"[TranslationService] Empty or null key provided. Using: {result}");
                return result;
            }

            // Try to get translation from current language
            var translation = GetNestedValue(_translations, key);
            if (translation != null)
            {
                var translationString = translation.ToString();
                if (!string.IsNullOrWhiteSpace(translationString))
                {
                    return translationString;
                }
            }

            // Try fallback to default language if current is not default
            if (CurrentLanguage != DEFAULT_LANGUAGE)
            {
                var defaultTranslation = GetNestedValue(_defaultLanguageTranslations, key);
                if (defaultTranslation != null)
                {
                    var defaultTranslationString = defaultTranslation.ToString();
                    if (!string.IsNullOrWhiteSpace(defaultTranslationString))
                    {
                        LogMissingTranslation(key, CurrentLanguage, "found_in_default");
                        return defaultTranslationString;
                    }
                }
            }

            // No translation found, use fallback or formatted key
            var finalFallback = !string.IsNullOrWhiteSpace(fallback) ? fallback : $"[{key}]";
            LogMissingTranslation(key, CurrentLanguage, "not_found");

            return finalFallback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting translation for key: {Key}", key);
            var errorFallback = !string.IsNullOrWhiteSpace(fallback) ? fallback : $"[{key}]";
            Console.WriteLine($"[TranslationService] ERROR getting translation for key '{key}': {ex.Message}. Using: {errorFallback}");
            return errorFallback;
        }
    }

    /// <summary>
    /// Logs missing translation information with appropriate severity levels.
    /// This method provides debugging information without breaking the application flow.
    /// 
    /// For future maintainers:
    /// - Missing translations are logged as warnings, not errors
    /// - Console output is provided for development debugging
    /// - Structured logging includes relevant context for troubleshooting
    /// </summary>
    /// <param name="key">The missing translation key</param>
    /// <param name="currentLanguage">The language being used</param>
    /// <param name="reason">The reason for the missing translation</param>
    private void LogMissingTranslation(string key, string currentLanguage, string reason)
    {
        LastMissingKey = key;
        var jsonFile = $"i18n/{currentLanguage}.json";

        // Provide clear, actionable messages based on the reason
        var message = reason switch
        {
            "found_in_default" => $"[TranslationService] Translation key '{key}' missing in {currentLanguage}, using default language fallback. " +
                                 $"Consider adding this key to {jsonFile}",
            "not_found" => $"[TranslationService] Translation key '{key}' missing in both {currentLanguage} and default language. " +
                          $"Please add this key to {jsonFile} and i18n/{DEFAULT_LANGUAGE}.json",
            _ => $"[TranslationService] Translation key '{key}' not found for language '{currentLanguage}'. File: {jsonFile}"
        };

        // Console output for development debugging
        Console.WriteLine(message);

        // Structured logging for production monitoring
        _logger.LogWarning("Missing translation key detected. Key: {TranslationKey}, Language: {Language}, " +
                          "File: {TranslationFile}, Reason: {Reason}, Suggestion: {Suggestion}",
            key,
            currentLanguage,
            jsonFile,
            reason,
            reason == "found_in_default"
                ? $"Add key '{key}' to {jsonFile}"
                : $"Add key '{key}' to translation files for all languages"
        );
    }

    public string GetTranslation(string key, params object[] parameters)
    {
        var translation = GetTranslation(key);

        try
        {
            return string.Format(translation, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting translation for key: {Key}", key);
            return translation;
        }
    }

    /// <summary>
    /// Gets a translation with formatted string interpolation using a fallback template.
    /// This method provides the centralized wrapper required by issue #106 for all interpolated strings.
    /// </summary>
    /// <param name="key">Translation key</param>
    /// <param name="defaultTemplate">Default template with placeholders like {0}, {1}, etc.</param>
    /// <param name="args">Arguments to substitute in the template</param>
    /// <returns>Formatted translated text</returns>
    public string GetTranslationFormatted(string key, string defaultTemplate, params object[] args)
    {
        try
        {
            var template = GetTranslation(key, defaultTemplate);
            return string.Format(template, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting translation for key: {Key} with template: {Template}", key, defaultTemplate);
            // Fallback to safe string formatting with default template
            try
            {
                return string.Format(defaultTemplate, args);
            }
            catch
            {
                return $"[FORMAT_ERROR: {key}]";
            }
        }
    }

    public Dictionary<string, string> GetAvailableLanguages()
    {
        return _availableLanguages;
    }

    /// <summary>
    /// Loads translations from the server API for SuperAdmin translation management.
    /// This method uses the pre-configured ApiClient HttpClient instance.
    /// 
    /// For future maintainers:
    /// - This method is used when the SuperAdmin feature manages translations via API
    /// - It gracefully falls back to local files if the API is unavailable
    /// - The ApiClient is pre-configured with BaseAddress in Program.cs
    /// - Dynamic language switching is supported without creating new HttpClient instances
    /// </summary>
    public async Task LoadTranslationsFromApiAsync()
    {
        try
        {
            _logger.LogDebug("Attempting to load translations from API for language: {Language}", CurrentLanguage);

            // Use pre-configured ApiClient with BaseAddress already set in Program.cs
            using var apiClient = _httpClientFactory.CreateClient("ApiClient");

            // Use relative URL since BaseAddress is pre-configured
            var apiEndpoint = $"api/translations/{CurrentLanguage}";
            var response = await apiClient.GetAsync(apiEndpoint);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                // Use JsonDocument to properly handle nested objects
                using var document = JsonDocument.Parse(json);
                var apiTranslations = ConvertJsonElementToDictionary(document.RootElement);

                if (apiTranslations != null && apiTranslations.Count > 0)
                {
                    _translations = apiTranslations;
                    LanguageChanged?.Invoke(this, CurrentLanguage);
                    _logger.LogInformation("Successfully loaded {Count} translation groups from API for language {Language}",
                        apiTranslations.Count, CurrentLanguage);
                }
                else
                {
                    _logger.LogWarning("API returned empty or invalid translations for language {Language}, falling back to local files", CurrentLanguage);
                    await LoadTranslationsAsync(CurrentLanguage);
                }
            }
            else
            {
                _logger.LogWarning("API request failed with status {StatusCode} for language {Language}, falling back to local files",
                    response.StatusCode, CurrentLanguage);
                await LoadTranslationsAsync(CurrentLanguage);
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "Network error loading translations from API for language {Language}, using local files", CurrentLanguage);
            await LoadTranslationsAsync(CurrentLanguage);
        }
        catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        {
            _logger.LogWarning(timeoutEx, "Timeout loading translations from API for language {Language}, using local files", CurrentLanguage);
            await LoadTranslationsAsync(CurrentLanguage);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "JSON parsing error for API translations in language {Language}, using local files", CurrentLanguage);
            await LoadTranslationsAsync(CurrentLanguage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error loading translations from API for language {Language}, using local files", CurrentLanguage);
            await LoadTranslationsAsync(CurrentLanguage);
        }
    }

    public bool HasTranslation(string key)
    {
        return GetNestedValue(_translations, key) != null;
    }

    /// <summary>
    /// Loads translations from local JSON files.
    /// </summary>
    private async Task LoadTranslationsAsync(string language)
    {
        await LoadTranslationsForLanguageAsync(language, _translations);
    }

    /// <summary>
    /// Loads translations from local JSON files into a specific dictionary using pre-configured HttpClient.
    /// The StaticClient HttpClient is already configured with the correct BaseAddress in Program.cs.
    /// 
    /// This method follows Blazor best practices:
    /// - Uses pre-configured HttpClient with BaseAddress set at startup
    /// - Uses relative URLs for translation files
    /// - Provides graceful fallback to default language
    /// - Logs warnings for missing translation files without breaking the app
    /// - Properly deserializes nested JSON objects as dictionaries
    /// </summary>
    /// <param name="language">Language code (it, en, es, fr)</param>
    /// <param name="targetDictionary">Dictionary to load translations into</param>
    private async Task LoadTranslationsForLanguageAsync(string language, Dictionary<string, object> targetDictionary)
    {
        try
        {
            // Use pre-configured StaticClient with BaseAddress already set
            using var staticClient = _httpClientFactory.CreateClient("StaticClient");

            // Use relative URL - BaseAddress is already configured in Program.cs
            var translationUrl = $"i18n/{language}.json";

            _logger.LogDebug("Loading translations for language {Language} from: {Url}", language, translationUrl);

            var response = await staticClient.GetAsync(translationUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                // Use JsonDocument to properly handle nested objects
                using var document = JsonDocument.Parse(json);
                var translations = ConvertJsonElementToDictionary(document.RootElement);

                if (translations != null && translations.Count > 0)
                {
                    // Clear and update the target dictionary
                    targetDictionary.Clear();
                    foreach (var kvp in translations)
                    {
                        targetDictionary[kvp.Key] = kvp.Value;
                    }
                    _logger.LogDebug("Successfully loaded {Count} translation groups for language {Language}", translations.Count, language);
                }
                else
                {
                    _logger.LogWarning("Translation file for language {Language} is empty or invalid JSON format", language);
                    await HandleTranslationLoadError(language, targetDictionary, "Empty or invalid JSON");
                }
            }
            else
            {
                _logger.LogWarning("Failed to load translations for language {Language}: HTTP {StatusCode} {ReasonPhrase}",
                    language, response.StatusCode, response.ReasonPhrase);
                await HandleTranslationLoadError(language, targetDictionary, $"HTTP {response.StatusCode}");
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Network error loading translations for language {Language}", language);
            await HandleTranslationLoadError(language, targetDictionary, "Network error");
        }
        catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
        {
            _logger.LogError(timeoutEx, "Timeout loading translations for language {Language}", language);
            await HandleTranslationLoadError(language, targetDictionary, "Timeout");
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON parsing error for translations in language {Language}", language);
            await HandleTranslationLoadError(language, targetDictionary, "JSON parsing error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading translations for language {Language}", language);
            await HandleTranslationLoadError(language, targetDictionary, "Unexpected error");
        }
    }

    /// <summary>
    /// Converts a JsonElement to a proper Dictionary structure for translation lookups.
    /// This ensures nested JSON objects are converted to nested dictionaries instead of JsonElements.
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>A dictionary representation of the JSON element</returns>
    private Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object>();

        if (element.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in element.EnumerateObject())
        {
            var value = ConvertJsonElementToObject(property.Value);
            result[property.Name] = value;
        }

        return result;
    }

    /// <summary>
    /// Converts a JsonElement to the appropriate object type.
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>The converted object</returns>
    private object ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToObject).ToArray(),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            _ => element.ToString()
        };
    }

    /// <summary>
    /// Handles translation loading errors with graceful fallback to default language.
    /// This method ensures the app continues to work even when translation files are missing.
    /// </summary>
    /// <param name="language">Language that failed to load</param>
    /// <param name="targetDictionary">Dictionary that was being populated</param>
    /// <param name="errorReason">Reason for the failure</param>
    private async Task HandleTranslationLoadError(string language, Dictionary<string, object> targetDictionary, string errorReason)
    {
        // Only attempt fallback to default language if:
        // 1. We're not already trying to load the default language
        // 2. We're loading into the main translations dictionary (not the default language dictionary)
        if (language != DEFAULT_LANGUAGE && targetDictionary == _translations)
        {
            _logger.LogWarning("Attempting fallback to default language {DefaultLanguage} due to error loading {Language}: {Error}",
                DEFAULT_LANGUAGE, language, errorReason);

            try
            {
                await LoadTranslationsForLanguageAsync(DEFAULT_LANGUAGE, _translations);
                CurrentLanguage = DEFAULT_LANGUAGE; // Update current language to reflect the fallback
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Critical error: Fallback to default language {DefaultLanguage} also failed", DEFAULT_LANGUAGE);
            }
        }
    }

    /// <summary>
    /// Gets a nested value from a dictionary using dot notation.
    /// Now simplified since we ensure proper dictionary structure during deserialization.
    /// </summary>
    private object? GetNestedValue(Dictionary<string, object> dictionary, string key)
    {
        var keys = key.Split('.');
        object current = dictionary;

        foreach (var k in keys)
        {
            if (current is Dictionary<string, object> dict && dict.ContainsKey(k))
            {
                current = dict[k];
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Saves language preference to local storage.
    /// </summary>
    private async Task SaveLanguagePreferenceAsync(string language)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LANGUAGE_PREFERENCE_KEY, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving language preference");
        }
    }

    /// <summary>
    /// Gets saved language preference from local storage.
    /// </summary>
    private async Task<string?> GetSavedLanguageAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", LANGUAGE_PREFERENCE_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved language preference");
            return null;
        }
    }
}