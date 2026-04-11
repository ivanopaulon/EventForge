using MudBlazor;

namespace EventForge.Client.Services;

/// <summary>
/// Centralizes all MudBlazor theme definitions for EventForge.
/// Use <see cref="GetMudTheme"/> to get the full theme for a given key,
/// or <see cref="GetLightPalette"/> / <see cref="GetDarkPalette"/> if you only need a palette.
/// </summary>
public static class EventForgeTheme
{
    /// <summary>
    /// Returns the full MudTheme for the given theme key.
    /// Falls back to the default theme when the key is unrecognized.
    /// </summary>
    public static MudTheme GetMudTheme(string themeKey) => new()
    {
        PaletteLight = GetLightPalette(themeKey),
        PaletteDark = GetDarkPalette(themeKey),
        Typography = GetTypography(),
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        }
    };

    /// <summary>Returns the light-mode palette for the given theme key.</summary>
    public static PaletteLight GetLightPalette(string themeKey) => themeKey switch
    {
        "carbon-neon-light" => new PaletteLight
        {
            Primary = "#0099CC",
            Secondary = "#00D9FF",
            Tertiary = "#7B68EE",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1A1A1A",
            Background = "#F5F5F5",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1A1A1A",
            TextPrimary = "#1A1A1A",
            TextSecondary = "#6B6B6B",
            Info = "#0099CC",
            Success = "#00C853",
            Warning = "#FFB300",
            Error = "#FF3D00"
        },
        _ => GetDefaultLightPalette()
    };

    /// <summary>Returns the dark-mode palette for the given theme key.</summary>
    public static PaletteDark GetDarkPalette(string themeKey) => themeKey switch
    {
        "carbon-neon-dark" => new PaletteDark
        {
            Primary = "#00F5FF",
            Secondary = "#FF006E",
            Background = "#121212",
            Surface = "#262626",
            DrawerBackground = "#1A1A1A",
            DrawerText = "#F5F5F5",
            AppbarBackground = "#000000",
            AppbarText = "#FFFFFF",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#B3B3B3",
            ActionDefault = "#00E5FF",
            Divider = "rgba(255,255,255,0.1)",
            Info = "#00E5FF",
            Success = "#10B981",
            Warning = "#F59E0B",
            Error = "#EF4444"
        },
        _ => GetDefaultDarkPalette()
    };

    // ── Private helpers ──────────────────────────────────────────────────────

    private static PaletteLight GetDefaultLightPalette() => new()
    {
        Primary = "#1F2F46",
        Secondary = "#247BFF",
        Tertiary = "#FF6B2C",
        AppbarBackground = "#1F2F46",
        AppbarText = "#ffffff",
        Background = "#F5F6FA",
        Surface = "#ffffff",
        DrawerBackground = "#1F2F46",
        DrawerText = "#d7d7d7",
        TextPrimary = "#2D2D2D",
        TextSecondary = "#666666",
        Info = "#247BFF",
        Success = "#4caf50",
        Warning = "#ff9800",
        Error = "#f44336"
    };

    private static PaletteDark GetDefaultDarkPalette() => new()
    {
        Black = "#1a1a2e",
        Background = "#1a1a2e",
        Surface = "#2d2d30",
        TextPrimary = "#e0e0e0",
        TextSecondary = "#b0b0b0",
        AppbarBackground = "#1a1a2e",
        AppbarText = "#e0e0e0",
        DrawerBackground = "#1a1a2e",
        DrawerText = "#b0b0b0",
        Primary = "#4fc3f7",
        Secondary = "#ffb74d",
        Tertiary = "#4fc3f7",
        Info = "#4fc3f7",
        Success = "#66bb6a",
        Warning = "#ffb74d",
        Error = "#f06292"
    };

    private static Typography GetTypography() => new()
    {
        Default = new DefaultTypography
        {
            FontFamily = ["Noto Sans", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "sans-serif"]
        },
        H1 = new H1Typography
        {
            FontFamily = ["Noto Sans Display", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"]
        },
        H2 = new H2Typography
        {
            FontFamily = ["Noto Sans Display", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"]
        },
        H3 = new H3Typography
        {
            FontFamily = ["Noto Sans Display", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"]
        },
        H4 = new H4Typography
        {
            FontFamily = ["Noto Sans Display", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"]
        },
        H5 = new H5Typography
        {
            FontFamily = ["Noto Sans Display", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"]
        },
        H6 = new H6Typography
        {
            FontFamily = ["Noto Sans Display", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"]
        }
    };
}
