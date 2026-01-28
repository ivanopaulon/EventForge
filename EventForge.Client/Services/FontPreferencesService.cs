using Blazored.LocalStorage;
using EventForge.DTOs.Profile;
using Microsoft.JSInterop;

namespace EventForge.Client.Services;

public interface IFontPreferencesService
{
    UserDisplayPreferencesDto CurrentPreferences { get; }
    event Action? OnPreferencesChanged;
    Task InitializeAsync();
    Task UpdatePreferencesAsync(UserDisplayPreferencesDto preferences);
    Task ApplyPreferencesAsync();
}

public class FontPreferencesService : IFontPreferencesService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILocalStorageService _localStorage;
    private readonly IProfileService _profileService;
    private readonly ILogger<FontPreferencesService> _logger;
    
    private const string StorageKey = "eventforge-font-preferences";
    private UserDisplayPreferencesDto _currentPreferences = new();

    public UserDisplayPreferencesDto CurrentPreferences => _currentPreferences;
    public event Action? OnPreferencesChanged;

    public FontPreferencesService(
        IJSRuntime jsRuntime,
        ILocalStorageService localStorage,
        IProfileService profileService,
        ILogger<FontPreferencesService> logger)
    {
        _jsRuntime = jsRuntime;
        _localStorage = localStorage;
        _profileService = profileService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // 1. Prova a caricare dal profilo server
            var profile = await _profileService.GetProfileAsync();
            if (profile?.DisplayPreferences != null)
            {
                _currentPreferences = profile.DisplayPreferences;
                await ApplyPreferencesAsync();
                return;
            }

            // 2. Fallback a localStorage
            var stored = await _localStorage.GetItemAsync<UserDisplayPreferencesDto>(StorageKey);
            if (stored != null)
            {
                _currentPreferences = stored;
            }
            else
            {
                // 3. Default preferences
                _currentPreferences = new UserDisplayPreferencesDto
                {
                    PrimaryFontFamily = "Noto Sans",
                    MonospaceFontFamily = "Noto Sans Mono",
                    BaseFontSize = 16,
                    PreferredTheme = "carbon-neon-light",
                    EnableExtendedFonts = true,
                    UseSystemFonts = false
                };
            }

            await ApplyPreferencesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing font preferences");
        }
    }

    public async Task UpdatePreferencesAsync(UserDisplayPreferencesDto preferences)
    {
        _currentPreferences = preferences;

        // Salva in localStorage
        await _localStorage.SetItemAsync(StorageKey, preferences);

        // Applica CSS
        await ApplyPreferencesAsync();

        // Aggiorna profilo server (opzionale, in background)
        _ = Task.Run(async () =>
        {
            try
            {
                var profile = await _profileService.GetProfileAsync();
                if (profile != null)
                {
                    var updateDto = new UpdateProfileDto
                    {
                        FirstName = profile.FirstName,
                        LastName = profile.LastName,
                        Email = profile.Email,
                        PhoneNumber = profile.PhoneNumber,
                        PreferredLanguage = profile.PreferredLanguage,
                        TimeZone = profile.TimeZone,
                        DisplayPreferences = preferences
                    };
                    await _profileService.UpdateProfileAsync(updateDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync font preferences to server");
            }
        });

        OnPreferencesChanged?.Invoke();
    }

    public async Task ApplyPreferencesAsync()
    {
        try
        {
            var fontFamily = _currentPreferences.UseSystemFonts 
                ? "var(--font-family-system)" 
                : $"'{_currentPreferences.PrimaryFontFamily}', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";
            
            var monoFamily = _currentPreferences.UseSystemFonts
                ? "'Courier New', Consolas, monospace"
                : $"'{_currentPreferences.MonospaceFontFamily}', 'Courier New', monospace";

            var fontSize = $"{_currentPreferences.BaseFontSize}px";

            // Apply CSS properties safely using dedicated JavaScript helper function
            await _jsRuntime.InvokeVoidAsync("EventForge.setFontPreferences", fontFamily, monoFamily, fontSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying font preferences");
        }
    }
}
