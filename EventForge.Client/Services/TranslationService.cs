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
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<TranslationService> _logger;

    private Dictionary<string, object> _translations = new();
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

    public TranslationService(
        HttpClient httpClient,
        IJSRuntime jsRuntime,
        ILogger<TranslationService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the service and loads the user's preferred language.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
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
            await LoadTranslationsAsync(_currentLanguage);
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
            var translation = GetNestedValue(_translations, key);
            if (translation != null)
            {
                return translation.ToString() ?? fallback ?? key;
            }

            // Try fallback to default language if current is not default
            if (_currentLanguage != DEFAULT_LANGUAGE)
            {
                var defaultTranslation = GetNestedValue(_translations, key);
                if (defaultTranslation != null)
                {
                    return defaultTranslation.ToString() ?? fallback ?? key;
                }
            }

            _logger.LogWarning("Translation not found for key: {Key}", key);
            return fallback ?? key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting translation for key: {Key}", key);
            return fallback ?? key;
        }
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
            var response = await _httpClient.GetAsync($"/api/translations/{_currentLanguage}");
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
        try
        {
            var response = await _httpClient.GetAsync($"i18n/{language}.json");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var translations = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (translations != null)
                {
                    _translations = translations;
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
                if (language != DEFAULT_LANGUAGE)
                {
                    await LoadTranslationsAsync(DEFAULT_LANGUAGE);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading translations for language {Language}", language);

            // Fallback to default language if not already trying it
            if (language != DEFAULT_LANGUAGE)
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