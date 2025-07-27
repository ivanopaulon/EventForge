using Microsoft.JSInterop;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing application theme (dark/light mode).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme mode.
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Event triggered when theme changes.
    /// </summary>
    event Action? OnThemeChanged;

    /// <summary>
    /// Toggles between dark and light mode.
    /// </summary>
    Task ToggleThemeAsync();

    /// <summary>
    /// Sets the theme mode.
    /// </summary>
    /// <param name="isDarkMode">True for dark mode, false for light mode.</param>
    Task SetThemeAsync(bool isDarkMode);

    /// <summary>
    /// Initializes the theme from stored preference.
    /// </summary>
    Task InitializeAsync();
}

/// <summary>
/// Implementation of theme service using localStorage for persistence.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private const string ThemeKey = "eventforge-theme";
    private bool _isDarkMode = false;

    public bool IsDarkMode => _isDarkMode;

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var storedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemeKey);
            _isDarkMode = storedTheme == "dark";

            // Apply theme to document
            await ApplyThemeToDocumentAsync();
        }
        catch
        {
            // Default to light mode if unable to read from localStorage
            _isDarkMode = false;
        }
    }

    public async Task ToggleThemeAsync()
    {
        await SetThemeAsync(!_isDarkMode);
    }

    public async Task SetThemeAsync(bool isDarkMode)
    {
        _isDarkMode = isDarkMode;

        try
        {
            // Store preference in localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, isDarkMode ? "dark" : "light");

            // Apply theme to document
            await ApplyThemeToDocumentAsync();

            // Notify subscribers
            OnThemeChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting theme: {ex.Message}");
        }
    }

    private async Task ApplyThemeToDocumentAsync()
    {
        try
        {
            var theme = _isDarkMode ? "dark" : "light";
            await _jsRuntime.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{theme}')");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying theme to document: {ex.Message}");
        }
    }
}