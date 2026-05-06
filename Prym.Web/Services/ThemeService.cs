using Microsoft.JSInterop;
using Prym.DTOs.Profile;

namespace Prym.Web.Services;

/// <summary>
/// Service for managing light/dark theme with localStorage persistence and server-profile sync.
/// </summary>
public interface IThemeService
{
    bool IsDarkMode { get; }
    string CurrentTheme { get; }
    event Action? OnThemeChanged;
    Task ToggleThemeAsync(CancellationToken ct = default);
    Task SetThemeAsync(bool isDarkMode, CancellationToken ct = default);
    Task SetThemeAsync(string themeKey, CancellationToken ct = default);
    Task InitializeAsync(CancellationToken ct = default);
    /// <summary>Loads theme from an already-fetched profile (called after login).</summary>
    Task LoadFromProfileAsync(UserProfileDto? profile, CancellationToken ct = default);
    /// <summary>Resets to default theme and clears localStorage entry (called on logout).</summary>
    Task ResetToDefaultsAsync(CancellationToken ct = default);
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

public class ThemeService(
    IJSRuntime jsRuntime,
    IProfileService profileService,
    IAuthService authService,
    ILogger<ThemeService> logger) : IThemeService
{
    private const string ThemeKey = "eventforge-theme";

    public static readonly List<ThemeInfo> AvailableThemes =
    [
        ThemeInfo.CarbonNeonDark,
        ThemeInfo.CarbonNeonLight
    ];

    private string _currentTheme = ThemeInfo.CarbonNeonLight.Key;

    public bool IsDarkMode => AvailableThemes.FirstOrDefault(t => t.Key == _currentTheme)?.IsDark ?? false;
    public string CurrentTheme => _currentTheme;
    public event Action? OnThemeChanged;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            // 1. If authenticated, prefer the server-side profile value
            if (await authService.IsAuthenticatedAsync())
            {
                var profile = await profileService.GetProfileAsync(ct);
                var serverTheme = profile?.DisplayPreferences?.PreferredTheme;
                if (!string.IsNullOrWhiteSpace(serverTheme) && AvailableThemes.Any(t => t.Key == serverTheme))
                {
                    _currentTheme = serverTheme;
                    await ApplyThemeToDocumentAsync();
                    OnThemeChanged?.Invoke();
                    return;
                }
            }

            // 2. Fallback to localStorage
            var stored = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemeKey);

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
            OnThemeChanged?.Invoke();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initialize theme from localStorage, using default");
            _currentTheme = ThemeInfo.CarbonNeonLight.Key;
        }
    }

    public async Task LoadFromProfileAsync(UserProfileDto? profile, CancellationToken ct = default)
    {
        try
        {
            var serverTheme = profile?.DisplayPreferences?.PreferredTheme;
            if (!string.IsNullOrWhiteSpace(serverTheme) && AvailableThemes.Any(t => t.Key == serverTheme))
            {
                _currentTheme = serverTheme;
                await jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, _currentTheme);
                await ApplyThemeToDocumentAsync();
                OnThemeChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load theme from profile");
        }
    }

    public async Task ResetToDefaultsAsync(CancellationToken ct = default)
    {
        try
        {
            _currentTheme = ThemeInfo.CarbonNeonLight.Key;
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", ThemeKey);
            await ApplyThemeToDocumentAsync();
            OnThemeChanged?.Invoke();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to reset theme to defaults");
        }
    }

    public async Task ToggleThemeAsync(CancellationToken ct = default)
    {
        try
        {
            var next = _currentTheme == ThemeInfo.CarbonNeonDark.Key
                ? ThemeInfo.CarbonNeonLight.Key
                : ThemeInfo.CarbonNeonDark.Key;

            await SetThemeAsync(next);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling theme");
            throw;
        }
    }

    public Task SetThemeAsync(bool isDarkMode, CancellationToken ct = default)
        => SetThemeAsync(isDarkMode ? ThemeInfo.CarbonNeonDark.Key : ThemeInfo.CarbonNeonLight.Key);

    public async Task SetThemeAsync(string themeKey, CancellationToken ct = default)
    {
        if (!AvailableThemes.Any(t => t.Key == themeKey))
            themeKey = ThemeInfo.CarbonNeonLight.Key;

        _currentTheme = themeKey;

        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, _currentTheme);
            await ApplyThemeToDocumentAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist theme {ThemeKey} to localStorage", themeKey);
        }

        OnThemeChanged?.Invoke();

        // Background-sync to server profile (fire and forget, non-critical)
        _ = Task.Run(async () =>
        {
            try
            {
                if (!await authService.IsAuthenticatedAsync())
                    return;

                var profile = await profileService.GetProfileAsync();
                if (profile == null)
                    return;

                // Update only PreferredTheme; preserve all other display preference fields
                var prefs = profile.DisplayPreferences ?? new UserDisplayPreferencesDto();
                prefs.PreferredTheme = _currentTheme;
                await profileService.UpdateDisplayPreferencesAsync(prefs);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Background sync of theme to server profile failed");
            }
        });
    }

    private async Task ApplyThemeToDocumentAsync()
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", _currentTheme);
            // Swap Syncfusion CSS stylesheet to match the current theme.
            await jsRuntime.InvokeVoidAsync("EventForge.setSyncfusionTheme", IsDarkMode);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to apply theme to document (normal in SSR)");
        }
    }
}