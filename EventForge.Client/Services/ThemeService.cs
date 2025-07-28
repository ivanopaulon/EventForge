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
    /// Gets the current theme key.
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Gets all available themes.
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
    /// Sets the theme by its key.
    /// </summary>
    /// <param name="themeKey">The theme key to set.</param>
    Task SetThemeAsync(string themeKey);

    /// <summary>
    /// Initializes the theme from stored preference.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Gets theme information by key.
    /// </summary>
    /// <param name="themeKey">The theme key.</param>
    /// <returns>Theme information or null if not found.</returns>
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
    private string _currentTheme = "light";
    private readonly Dictionary<string, ThemeInfo> _themes;

    public bool IsDarkMode => _currentTheme == "dark";
    public string CurrentTheme => _currentTheme;
    public IReadOnlyList<ThemeInfo> AvailableThemes => _themes.Values.ToList();

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _themes = InitializeThemes();
    }

    private Dictionary<string, ThemeInfo> InitializeThemes()
    {
        return new Dictionary<string, ThemeInfo>
        {
            ["light"] = new ThemeInfo
            {
                Key = "light",
                Name = "Light",
                Description = "Modern bright theme with EventForge colors",
                ColorPreview = "#F5F6FA",
                IsDark = false,
                Icon = "Icons.Material.Filled.LightMode"
            },
            ["dark"] = new ThemeInfo
            {
                Key = "dark",
                Name = "Dark",
                Description = "Classic dark theme for low-light environments",
                ColorPreview = "#1a1a2e",
                IsDark = true,
                Icon = "Icons.Material.Filled.DarkMode"
            },
            ["warm"] = new ThemeInfo
            {
                Key = "warm",
                Name = "Warm",
                Description = "Cozy theme with orange and earthy tones",
                ColorPreview = "#fdf2e9",
                IsDark = false,
                Icon = "Icons.Material.Filled.LocalFireDepartment"
            },
            ["cool"] = new ThemeInfo
            {
                Key = "cool",
                Name = "Cool",
                Description = "Refreshing theme with blue and turquoise colors",
                ColorPreview = "#e0f2f1",
                IsDark = false,
                Icon = "Icons.Material.Filled.AcUnit"
            },
            ["high-contrast"] = new ThemeInfo
            {
                Key = "high-contrast",
                Name = "High Contrast",
                Description = "High contrast theme for accessibility (WCAG AAA)",
                ColorPreview = "#000000",
                IsDark = false,
                Icon = "Icons.Material.Filled.Contrast"
            },
            ["fun"] = new ThemeInfo
            {
                Key = "fun",
                Name = "Fun",
                Description = "Playful theme with vibrant pastel colors",
                ColorPreview = "#f3e5f5",
                IsDark = false,
                Icon = "Icons.Material.Filled.Palette"
            }
        };
    }

    public async Task InitializeAsync()
    {
        try
        {
            var storedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThemeKey);
            
            // Handle backward compatibility with old boolean values
            if (storedTheme == "dark" || storedTheme == "true")
            {
                _currentTheme = "dark";
            }
            else if (storedTheme == "light" || storedTheme == "false" || string.IsNullOrEmpty(storedTheme))
            {
                _currentTheme = "light";
            }
            else if (_themes.ContainsKey(storedTheme))
            {
                _currentTheme = storedTheme;
            }
            else
            {
                _currentTheme = "light"; // Default fallback
            }

            // Apply theme to document
            await ApplyThemeToDocumentAsync();
        }
        catch
        {
            // Default to light mode if unable to read from localStorage
            _currentTheme = "light";
        }
    }

    public async Task ToggleThemeAsync()
    {
        // Backward compatibility: toggle between light and dark
        await SetThemeAsync(_currentTheme == "dark" ? "light" : "dark");
    }

    public async Task SetThemeAsync(bool isDarkMode)
    {
        // Backward compatibility method
        await SetThemeAsync(isDarkMode ? "dark" : "light");
    }

    public async Task SetThemeAsync(string themeKey)
    {
        if (!_themes.ContainsKey(themeKey))
        {
            Console.WriteLine($"Theme '{themeKey}' not found. Using default 'light' theme.");
            themeKey = "light";
        }

        _currentTheme = themeKey;

        try
        {
            // Store preference in localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, themeKey);

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

    public ThemeInfo? GetThemeInfo(string themeKey)
    {
        return _themes.TryGetValue(themeKey, out var theme) ? theme : null;
    }

    private async Task ApplyThemeToDocumentAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{_currentTheme}')");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying theme to document: {ex.Message}");
        }
    }
}