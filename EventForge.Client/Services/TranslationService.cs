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
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly HttpClient _apiHttpClient;
    private readonly HttpClient _staticHttpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<TranslationService> _logger;

    private Dictionary<string, object> _translations = new();
    private Dictionary<string, object> _defaultLanguageTranslations = new();
    private string _currentLanguage = "it"; // Default to Italian
    private const string DEFAULT_LANGUAGE = "it";
    private const string LANGUAGE_PREFERENCE_KEY = "eventforge_language";

    private readonly Dictionary<string, string> _availableLanguages = new()
    {
        { "it", "Italiano" },
        { "en", "English" },
        { "es", "Español" },
        { "fr", "Français" }
    };

    public event EventHandler<string>? LanguageChanged;

    public string CurrentLanguage => _currentLanguage;
    public string? LastMissingKey { get; private set; }

    public TranslationService(
        HttpClient httpClient,
        IJSRuntime jsRuntime,
        ILogger<TranslationService> logger)
    {
        _apiHttpClient = httpClient; // This one has the API base URL
        _jsRuntime = jsRuntime;
        _logger = logger;

        // Create a separate HttpClient for static files (local relative URLs)
        // We'll set the base address in InitializeAsync after we can access JS runtime
        _staticHttpClient = new HttpClient();
    }

    /// <summary>
    /// Initializes the service and loads the user's preferred language.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Set the base address for the static client to the current page's base URL
            var baseUri = await _jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
            _staticHttpClient.BaseAddress = new Uri(baseUri);

            // Load default language translations first for fallback
            await LoadTranslationsForLanguageAsync(DEFAULT_LANGUAGE, _defaultLanguageTranslations);

            // Try to get saved language preference
            var savedLanguage = await GetSavedLanguageAsync();
            if (!string.IsNullOrEmpty(savedLanguage) && _availableLanguages.ContainsKey(savedLanguage))
            {
                _currentLanguage = savedLanguage;
            }

            // Load translations for current language
            await LoadTranslationsAsync(_currentLanguage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing translation service");
            // Fallback to default language
            _currentLanguage = DEFAULT_LANGUAGE;
            try
            {
                await LoadTranslationsAsync(_currentLanguage);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Error loading fallback language {Language}", _currentLanguage);
            }
        }
    }

    public async Task SetLanguageAsync(string language)
    {
        if (!_availableLanguages.ContainsKey(language))
        {
            _logger.LogWarning("Unsupported language: {Language}", language);
            return;
        }

        var previousLanguage = _currentLanguage;
        _currentLanguage = language;

        try
        {
            // Load new translations
            await LoadTranslationsAsync(language);

            // Save preference
            await SaveLanguagePreferenceAsync(language);

            // Notify language change
            LanguageChanged?.Invoke(this, language);

            _logger.LogInformation("Language changed from {PreviousLanguage} to {NewLanguage}", previousLanguage, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting language to {Language}", language);
            // Revert to previous language
            _currentLanguage = previousLanguage;
            throw;
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
            if (_currentLanguage != DEFAULT_LANGUAGE)
            {
                var defaultTranslation = GetNestedValue(_defaultLanguageTranslations, key);
                if (defaultTranslation != null)
                {
                    var defaultTranslationString = defaultTranslation.ToString();
                    if (!string.IsNullOrWhiteSpace(defaultTranslationString))
                    {
                        LogMissingTranslation(key, _currentLanguage, "found_in_default");
                        return defaultTranslationString;
                    }
                }
            }

            // No translation found, use fallback or formatted key
            var finalFallback = !string.IsNullOrWhiteSpace(fallback) ? fallback : $"[{key}]";
            LogMissingTranslation(key, _currentLanguage, "not_found");
            
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
    /// Logs missing translation information to console for debugging.
    /// </summary>
    private void LogMissingTranslation(string key, string currentLanguage, string reason)
    {
        LastMissingKey = key;
        var jsonFile = $"i18n/{currentLanguage}.json";
        var message = reason == "found_in_default" 
            ? $"[TranslationService] Key '{key}' missing in {currentLanguage}, using default language fallback. File: {jsonFile}"
            : $"[TranslationService] Key '{key}' missing in language '{currentLanguage}' and default language. File: {jsonFile}";
        
        Console.WriteLine(message);
        _logger.LogWarning("Translation key '{Key}' missing. Language: {Language}, File: {File}, Reason: {Reason}", 
            key, currentLanguage, jsonFile, reason);
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

    public Dictionary<string, string> GetAvailableLanguages()
    {
        return _availableLanguages;
    }

    public async Task LoadTranslationsFromApiAsync()
    {
        try
        {
            // TODO: Implement API loading for SuperAdmin translation management
            // This will be used when the SuperAdmin feature is complete
            var response = await _apiHttpClient.GetAsync($"/api/translations/{_currentLanguage}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiTranslations = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (apiTranslations != null)
                {
                    _translations = apiTranslations;
                    LanguageChanged?.Invoke(this, _currentLanguage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load translations from API, using local files");
            // Fallback to loading from local files
            await LoadTranslationsAsync(_currentLanguage);
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
    /// Loads translations from local JSON files into a specific dictionary.
    /// </summary>
    private async Task LoadTranslationsForLanguageAsync(string language, Dictionary<string, object> targetDictionary)
    {
        try
        {
            var response = await _staticHttpClient.GetAsync($"i18n/{language}.json");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var translations = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (translations != null)
                {
                    // Clear and update the target dictionary
                    targetDictionary.Clear();
                    foreach (var kvp in translations)
                    {
                        targetDictionary[kvp.Key] = kvp.Value;
                    }
                    _logger.LogDebug("Loaded {Count} translation groups for language {Language}", translations.Count, language);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize translations for language {Language}", language);
                }
            }
            else
            {
                _logger.LogWarning("Failed to load translations for language {Language}: {StatusCode}", language, response.StatusCode);

                // Fallback to default language if not already trying it
                if (language != DEFAULT_LANGUAGE && targetDictionary == _translations)
                {
                    await LoadTranslationsAsync(DEFAULT_LANGUAGE);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading translations for language {Language}", language);

            // Fallback to default language if not already trying it
            if (language != DEFAULT_LANGUAGE && targetDictionary == _translations)
            {
                await LoadTranslationsAsync(DEFAULT_LANGUAGE);
            }
        }
    }

    /// <summary>
    /// Gets a nested value from a dictionary using dot notation.
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
            else if (current is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(k, out var property))
                {
                    current = property;
                }
                else
                {
                    return null;
                }
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