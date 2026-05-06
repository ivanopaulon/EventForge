using Blazored.LocalStorage;
using Microsoft.JSInterop;
using Prym.DTOs.Profile;

namespace Prym.Web.Services;

public interface IFontPreferencesService
{
    UserDisplayPreferencesDto CurrentPreferences { get; }
    event Action? OnPreferencesChanged;
    Task InitializeAsync(CancellationToken ct = default);
    Task UpdatePreferencesAsync(UserDisplayPreferencesDto preferences, CancellationToken ct = default);
    Task ApplyPreferencesAsync(CancellationToken ct = default);
    /// <summary>Loads font preferences from an already-fetched profile (called after login).</summary>
    Task LoadFromProfileAsync(UserProfileDto? profile, CancellationToken ct = default);
    /// <summary>Resets to default font preferences and clears localStorage entry (called on logout).</summary>
    Task ResetToDefaultsAsync(CancellationToken ct = default);
}

public class FontPreferencesService(
    IJSRuntime jsRuntime,
    ILocalStorageService localStorage,
    IProfileService profileService,
    IAuthService authService,
    ILogger<FontPreferencesService> logger) : IFontPreferencesService
{
    private const string StorageKey = "eventforge-font-preferences";
    private UserDisplayPreferencesDto _currentPreferences = new();

    public UserDisplayPreferencesDto CurrentPreferences => _currentPreferences;
    public event Action? OnPreferencesChanged;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            // 1. Prova a caricare dal profilo server (solo se autenticato)
            UserProfileDto? profile = null;
            if (await authService.IsAuthenticatedAsync())
            {
                profile = await profileService.GetProfileAsync();
            }
            if (profile?.DisplayPreferences != null)
            {
                _currentPreferences = profile.DisplayPreferences;
                await ApplyPreferencesAsync();
                return;
            }

            // 2. Fallback a localStorage
            var stored = await localStorage.GetItemAsync<UserDisplayPreferencesDto>(StorageKey);
            if (stored != null)
            {
                _currentPreferences = stored;
            }
            else
            {
                // 3. Default preferences
                _currentPreferences = new UserDisplayPreferencesDto
                {
                    BodyFont = "Noto Sans",
                    HeadingsFont = "Noto Sans Display",
                    MonospaceFont = "Noto Sans Mono",
                    ContentFont = "Noto Serif",
                    BaseFontSize = 16,
                    UseSystemFonts = false,
                    EnableExtendedScripts = false,
                    EnabledScripts = new()
                };
            }

            await ApplyPreferencesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing font preferences");
        }
    }

    public async Task UpdatePreferencesAsync(UserDisplayPreferencesDto preferences, CancellationToken ct = default)
    {
        // Preserve the current theme preference so a font change never overwrites it on the server
        if (!string.IsNullOrWhiteSpace(_currentPreferences.PreferredTheme))
            preferences.PreferredTheme = _currentPreferences.PreferredTheme;
        _currentPreferences = preferences;

        // Salva in localStorage
        await localStorage.SetItemAsync(StorageKey, preferences);

        // Applica CSS
        await ApplyPreferencesAsync();

        // Aggiorna display-preferences sul server (opzionale, in background)
        _ = Task.Run(async () =>
        {
            try
            {
                await profileService.UpdateDisplayPreferencesAsync(preferences);
            }
            catch (HttpRequestException ex)
            {
                // Background sync failed, not critical (already logged by HttpClientService)
                logger.LogDebug(ex, "Background sync font preferences failed");
            }
        });

        OnPreferencesChanged?.Invoke();
    }

    public async Task LoadFromProfileAsync(UserProfileDto? profile, CancellationToken ct = default)
    {
        try
        {
            if (profile?.DisplayPreferences != null)
            {
                _currentPreferences = profile.DisplayPreferences;
                await localStorage.SetItemAsync(StorageKey, _currentPreferences);
                await ApplyPreferencesAsync();
                OnPreferencesChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading font preferences from profile");
        }
    }

    public async Task ResetToDefaultsAsync(CancellationToken ct = default)
    {
        try
        {
            _currentPreferences = new UserDisplayPreferencesDto
            {
                BodyFont = "Noto Sans",
                HeadingsFont = "Noto Sans Display",
                MonospaceFont = "Noto Sans Mono",
                ContentFont = "Noto Serif",
                BaseFontSize = 16,
                UseSystemFonts = false,
                EnableExtendedScripts = false,
                EnabledScripts = new()
            };

            await localStorage.RemoveItemAsync(StorageKey);
            await ApplyPreferencesAsync();
            OnPreferencesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting font preferences to defaults");
        }
    }

    public async Task ApplyPreferencesAsync(CancellationToken ct = default)
    {
        try
        {
            var bodyFamily = _currentPreferences.UseSystemFonts
                ? "var(--font-family-system)"
                : $"'{_currentPreferences.BodyFont}', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";

            var headingsFamily = _currentPreferences.UseSystemFonts
                ? "var(--font-family-system)"
                : $"'{_currentPreferences.HeadingsFont}', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";

            var monoFamily = _currentPreferences.UseSystemFonts
                ? "'Courier New', Consolas, monospace"
                : $"'{_currentPreferences.MonospaceFont}', 'Courier New', monospace";

            var contentFamily = _currentPreferences.UseSystemFonts
                ? "var(--font-family-system)"
                : $"'{_currentPreferences.ContentFont}', Georgia, serif";

            var fontSize = $"{_currentPreferences.BaseFontSize}px";

            // Apply CSS properties using the new multi-context function
            await jsRuntime.InvokeVoidAsync("EventForge.setFontPreferences",
                bodyFamily, headingsFamily, monoFamily, contentFamily, fontSize);

            logger.LogInformation("Applied font preferences: Body={Body}, Headings={Headings}, Size={Size}px",
                _currentPreferences.BodyFont,
                _currentPreferences.HeadingsFont,
                _currentPreferences.BaseFontSize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying font preferences");
        }
    }
}
