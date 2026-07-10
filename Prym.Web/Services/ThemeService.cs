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

    public static readonly ThemeInfo PrymDark = new()
    {
        Key = "prym-dark",
        Name = "PRYM Scuro",
        Description = "Tema grafite e ambra per ambienti scuri",
        ColorPreview = "#E8B563",
        IsDark = true
    };

    public static readonly ThemeInfo PrymLight = new()
    {
        Key = "prym-light",
        Name = "PRYM Chiaro",
        Description = "Tema grafite e ambra per ambienti chiari",
        ColorPreview = "#2B2F36",
        IsDark = false
    };
}

public class ThemeService(
    IJSRuntime jsRuntime,
    IProfileService profileService,
    IAuthService authService,
    ILogger<ThemeService> logger) : IThemeService, IDisposable
{
    private const string ThemeKey = "eventforge-theme";

    // Serializza le sync verso il profilo server: senza questo lock, due SetThemeAsync
    // ravvicinati leggerebbero lo stesso profilo "vecchio" e scriverebbero in ordine
    // non garantito, con rischio che l'ultimo a scrivere non corrisponda all'ultima
    // scelta effettiva dell'utente.
    private readonly SemaphoreSlim _serverSyncLock = new(1, 1);

    public static readonly List<ThemeInfo> AvailableThemes =
    [
        ThemeInfo.PrymDark,
        ThemeInfo.PrymLight
    ];

    private string _currentTheme = ThemeInfo.PrymLight.Key;

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
                var serverTheme = NormalizeThemeKey(profile?.DisplayPreferences?.PreferredTheme);
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
            if (string.Equals(stored, "dark", StringComparison.OrdinalIgnoreCase) || string.Equals(stored, "carbon-neon-dark", StringComparison.OrdinalIgnoreCase))
                _currentTheme = ThemeInfo.PrymDark.Key;
            else if (string.Equals(stored, "light", StringComparison.OrdinalIgnoreCase) || string.Equals(stored, "carbon-neon-light", StringComparison.OrdinalIgnoreCase))
                _currentTheme = ThemeInfo.PrymLight.Key;
            else if (!string.IsNullOrWhiteSpace(stored) && AvailableThemes.Any(t => t.Key == stored))
                _currentTheme = stored!;
            else
                _currentTheme = ThemeInfo.PrymLight.Key;

            await ApplyThemeToDocumentAsync();
            OnThemeChanged?.Invoke();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initialize theme from localStorage, using default");
            _currentTheme = ThemeInfo.PrymLight.Key;
        }
    }

    public async Task LoadFromProfileAsync(UserProfileDto? profile, CancellationToken ct = default)
    {
        try
        {
            var serverTheme = NormalizeThemeKey(profile?.DisplayPreferences?.PreferredTheme);
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
            _currentTheme = ThemeInfo.PrymLight.Key;
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
            var next = _currentTheme == ThemeInfo.PrymDark.Key
                ? ThemeInfo.PrymLight.Key
                : ThemeInfo.PrymDark.Key;

            await SetThemeAsync(next);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling theme");
            throw;
        }
    }

    public Task SetThemeAsync(bool isDarkMode, CancellationToken ct = default)
        => SetThemeAsync(isDarkMode ? ThemeInfo.PrymDark.Key : ThemeInfo.PrymLight.Key);

    public async Task SetThemeAsync(string themeKey, CancellationToken ct = default)
    {
        themeKey = NormalizeThemeKey(themeKey) ?? ThemeInfo.PrymLight.Key;

        if (!AvailableThemes.Any(t => t.Key == themeKey))
            themeKey = ThemeInfo.PrymLight.Key;

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

        // Sync al profilo server — atteso (non più Task.Run scollegato dal circuito Blazor Server),
        // ma non bloccante per l'esperienza percepita dato che localStorage/DOM sono già stati
        // aggiornati sopra. Il fire-and-forget precedente perdeva silenziosamente il salvataggio
        // se il circuito terminava prima del completamento del task interno.
        // WaitAsync(ct) è fuori dal blocco try/finally: se viene cancellato prima di acquisire
        // il lock lancia OperationCanceledException senza mai entrare nel finally, quindi
        // Release() non viene mai chiamato su un semaforo non acquisito.
        await _serverSyncLock.WaitAsync(ct);
        try
        {
            if (await authService.IsAuthenticatedAsync())
            {
                var profile = await profileService.GetProfileAsync(ct);
                if (profile != null)
                {
                    // Riapplica sempre _currentTheme (non themeKey): se un altro toggle è
                    // arrivato mentre eravamo in coda sul lock, vince l'ultima scelta reale.
                    var prefs = profile.DisplayPreferences ?? new UserDisplayPreferencesDto();
                    prefs.PreferredTheme = _currentTheme;
                    var updated = await profileService.UpdateDisplayPreferencesAsync(prefs, ct);
                    if (updated == null)
                    {
                        logger.LogWarning("Server sync of theme {ThemeKey} failed (null response)", themeKey);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to sync theme {ThemeKey} to server profile", themeKey);
        }
        finally
        {
            _serverSyncLock.Release();
        }
    }


    private static string? NormalizeThemeKey(string? themeKey)
    {
        if (string.IsNullOrWhiteSpace(themeKey))
            return themeKey;

        return themeKey switch
        {
            "dark" or "carbon-neon-dark" => ThemeInfo.PrymDark.Key,
            "light" or "carbon-neon-light" => ThemeInfo.PrymLight.Key,
            _ => themeKey
        };
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

    public void Dispose() => _serverSyncLock.Dispose();
}