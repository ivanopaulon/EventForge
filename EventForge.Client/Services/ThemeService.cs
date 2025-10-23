using Microsoft.JSInterop;

namespace EventForge.Client.Services;

/// <summary>
/// Represents a theme with metadata for display and functionality.
/// </summary>
public class ThemeInfo
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ColorPreview { get; set; } = string.Empty;
    public bool IsDark { get; set; } = false;
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Service for managing application theme with support for multiple theme options.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme mode (backward compatibility).
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Gets the key of the current theme.
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Gets the available themes.
    /// </summary>
    IReadOnlyList<ThemeInfo> AvailableThemes { get; }

    /// <summary>
    /// Event triggered when theme changes.
    /// </summary>
    event Action? OnThemeChanged;

    /// <summary>
    /// Toggles between dark and light mode (backward compatibility).
    /// </summary>
    Task ToggleThemeAsync();

    /// <summary>
    /// Sets the theme mode (backward compatibility).
    /// </summary>
    /// <param name="isDarkMode">True for dark mode, false for light mode.</param>
    Task SetThemeAsync(bool isDarkMode);

    /// <summary>
    /// Sets the theme by key.
    /// </summary>
    /// <param name="themeKey">The key of the theme to set.</param>
    Task SetThemeAsync(string themeKey);

    /// <summary>
    /// Initializes the theme from stored preference.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Gets the theme information by key.
    /// </summary>
    /// <param name="themeKey">The key of the theme.</param>
    /// <returns>The theme information, or null if not found.</returns>
    ThemeInfo? GetThemeInfo(string themeKey);
}

/// <summary>
/// Implementation of theme service using localStorage for persistence.
/// Supports multiple themes while maintaining backward compatibility.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private const string ThemeKey = "eventforge-theme";
    private readonly Dictionary<string, ThemeInfo> _themes;
    private string _currentTheme = "light";

    public bool IsDarkMode => _currentTheme == "dark";
    public string CurrentTheme => _currentTheme;
    public IReadOnlyList<ThemeInfo> AvailableThemes => _themes.Values.ToList();
    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _themes = new Dictionary<string, ThemeInfo>
        {
            ["light"] = new ThemeInfo { Key = "light", Name = "Light", Description = "Light theme", ColorPreview = "#F5F6FA", IsDark = false, Icon = "Icons.Material.Filled.LightMode" },
            ["dark"] = new ThemeInfo { Key = "dark", Name = "Dark", Description = "Dark theme", ColorPreview = "#1a1a2e", IsDark = true, Icon = "Icons.Material.Filled.DarkMode" }
        };
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
        if (!_themes.ContainsKey(themeKey))
        {
            themeKey = "light";
        }

        _currentTheme = themeKey;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, _currentTheme);
            await ApplyThemeToDocumentAsync();
        }
        catch { }

        OnThemeChanged?.Invoke();
    }

    public ThemeInfo? GetThemeInfo(string themeKey)
    {
        return _themes.TryGetValue(themeKey, out var info) ? info : null;
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