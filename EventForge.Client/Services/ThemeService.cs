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

public class ThemeInfo
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? ColorPreview { get; init; }
    public bool IsDark { get; init; }

    public static readonly ThemeInfo CarbonNeonDark = new()
    {
        Key = "carbon-neon-dark",
        Name = "Carbon Neon Dark",
        Description = "Modern dark theme with neon accents",
        ColorPreview = "#00FFFF",
        IsDark = true
    };

    public static readonly ThemeInfo CarbonNeonLight = new()
    {
        Key = "carbon-neon-light",
        Name = "Carbon Neon Light",
        Description = "Clean light theme with modern colors",
        ColorPreview = "#0099CC",
        IsDark = false
    };
}

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private const string ThemeKey = "eventforge-theme";

    public static readonly List<ThemeInfo> AvailableThemes = new()
    {
        ThemeInfo.CarbonNeonDark,
        ThemeInfo.CarbonNeonLight
    };

    private string _currentTheme = ThemeInfo.CarbonNeonLight.Key;

    public bool IsDarkMode => AvailableThemes.FirstOrDefault(t => t.Key == _currentTheme)?.IsDark ?? false;
    public string CurrentTheme => _currentTheme;
    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task InitializeAsync()
    {
        try
        {
            var stored = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemeKey);

            // Backward compatibility with old "light"/"dark" values
            if (string.Equals(stored, "dark", StringComparison.OrdinalIgnoreCase))
                _currentTheme = ThemeInfo.CarbonNeonDark.Key;
            else if (string.Equals(stored, "light", StringComparison.OrdinalIgnoreCase))
                _currentTheme = ThemeInfo.CarbonNeonLight.Key;
            else if (!string.IsNullOrWhiteSpace(stored) && AvailableThemes.Any(t => t.Key == stored))
                _currentTheme = stored!;
            else
                _currentTheme = ThemeInfo.CarbonNeonLight.Key;

            await ApplyThemeToDocumentAsync();
        }
        catch
        {
            _currentTheme = ThemeInfo.CarbonNeonLight.Key;
        }
    }

    public async Task ToggleThemeAsync()
    {
        var next = _currentTheme == ThemeInfo.CarbonNeonDark.Key
            ? ThemeInfo.CarbonNeonLight.Key
            : ThemeInfo.CarbonNeonDark.Key;

        await SetThemeAsync(next);
    }

    public Task SetThemeAsync(bool isDarkMode)
        => SetThemeAsync(isDarkMode ? ThemeInfo.CarbonNeonDark.Key : ThemeInfo.CarbonNeonLight.Key);

    public async Task SetThemeAsync(string themeKey)
    {
        if (!AvailableThemes.Any(t => t.Key == themeKey))
            themeKey = ThemeInfo.CarbonNeonLight.Key;

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