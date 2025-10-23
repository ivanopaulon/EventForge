using Microsoft.JSInterop;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing light/dark theme with localStorage persistence.
/// Keeps a minimal compatible API used across the app.
/// </summary>
public interface IThemeService
{
    bool IsDarkMode { get; }
    string CurrentTheme { get; }
    event Action? OnThemeChanged;
    Task ToggleThemeAsync();
    Task SetThemeAsync(bool isDarkMode);
    Task SetThemeAsync(string themeKey);
    Task InitializeAsync();
}

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private const string ThemeKey = "eventforge-theme";
    private string _currentTheme = "light";

    public bool IsDarkMode => _currentTheme == "dark";
    public string CurrentTheme => _currentTheme;
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
            if (storedTheme == "dark" || storedTheme == "true")
                _currentTheme = "dark";
            else
                _currentTheme = "light";

            await ApplyThemeToDocumentAsync();
        }
        catch
        {
            _currentTheme = "light";
        }
    }

    public async Task ToggleThemeAsync()
    {
        await SetThemeAsync(CurrentTheme == "dark" ? "light" : "dark");
    }

    public async Task SetThemeAsync(bool isDarkMode)
    {
        await SetThemeAsync(isDarkMode ? "dark" : "light");
    }

    public async Task SetThemeAsync(string themeKey)
    {
        // Accept only "light" or "dark"; fallback to light
        if (themeKey != "dark")
            themeKey = "light";

        _currentTheme = themeKey;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, _currentTheme);
            await ApplyThemeToDocumentAsync();
        }
        catch { }

        OnThemeChanged?.Invoke();
    }

    private async Task ApplyThemeToDocumentAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{_currentTheme}')");
        }
        catch { }
    }
}