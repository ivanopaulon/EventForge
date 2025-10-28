# EventForge Theming System

## Overview

EventForge uses a modern theming system based on **Carbon/Neon** design principles, providing both dark and light modes with consistent styling across MudBlazor and Syncfusion components.

## Theming Architecture

The theming system consists of three key components:

1. **ThemeService** - Manages theme state and persistence
2. **MudBlazor Palettes** - Defines colors for MudBlazor components
3. **CSS Variables** - Provides consistent styling through custom properties

## Available Themes

### Carbon Neon Dark
- **Theme Key**: `carbon-neon-dark`
- **Description**: Modern dark theme with neon accents
- **Primary Color**: `#00FFFF` (Neon Cyan)
- **Use Case**: Low-light environments, extended usage sessions

### Carbon Neon Light
- **Theme Key**: `carbon-neon-light`
- **Description**: Clean light theme with modern colors
- **Primary Color**: `#0099CC` (Bright Cyan)
- **Use Case**: Well-lit environments, daytime usage

## CSS Variables Reference

### Dark Theme Variables (`[data-theme="carbon-neon-dark"]`)

#### Core Colors
- `--primary`: #00FFFF (Neon Cyan)
- `--secondary`: #FF00FF (Neon Magenta)
- `--accent`: #00FF00 (Neon Green)

#### Backgrounds
- `--background`: #0D0D0D (Deep Black)
- `--surface`: #1A1A1A (Carbon Surface)
- `--surface-2`: #262626 (Elevated Surface)
- `--card-background`: #1A1A1A
- `--card-border`: #333333

#### Text
- `--text-primary`: #E0E0E0 (Light Gray)
- `--text-secondary`: #A0A0A0 (Medium Gray)
- `--text-muted`: #707070 (Muted Gray)

#### Interactive Elements
- `--button-primary-bg`: #00FFFF
- `--button-primary-text`: #0D0D0D
- `--button-primary-hover`: #00D9FF
- `--link`: #00FFFF
- `--link-hover`: #00D9FF

#### Borders & Effects
- `--border`: #333333
- `--divider`: #262626
- `--shadow-neon`: 0 0 10px rgba(0, 255, 255, 0.3)

### Light Theme Variables (`[data-theme="carbon-neon-light"]`)

#### Core Colors
- `--primary`: #0099CC (Bright Cyan)
- `--secondary`: #00D9FF (Electric Blue)
- `--accent`: #7B68EE (Medium Slate Blue)

#### Backgrounds
- `--background`: #F5F5F5 (Light Gray)
- `--surface`: #FFFFFF (White)
- `--surface-2`: #FAFAFA (Elevated Surface)
- `--card-background`: #FFFFFF
- `--card-border`: #E0E0E0

#### Text
- `--text-primary`: #1A1A1A (Near Black)
- `--text-secondary`: #6B6B6B (Medium Gray)
- `--text-muted`: #9E9E9E (Muted Gray)

#### Interactive Elements
- `--button-primary-bg`: #0099CC
- `--button-primary-text`: #FFFFFF
- `--button-primary-hover`: #007A99
- `--link`: #0099CC
- `--link-hover`: #007A99

#### Borders & Effects
- `--border`: #E0E0E0
- `--divider`: #F0F0F0
- `--shadow-neon`: 0 0 10px rgba(0, 153, 204, 0.2)

## Using Themes in Code

### Switching Themes Programmatically

```csharp
@inject IThemeService ThemeService

// Toggle between dark and light
await ThemeService.ToggleThemeAsync();

// Set specific theme by key
await ThemeService.SetThemeAsync("carbon-neon-dark");

// Set theme by dark/light boolean
await ThemeService.SetThemeAsync(isDarkMode: true);
```

### Checking Current Theme

```csharp
@inject IThemeService ThemeService

// Check if dark mode is active
bool isDark = ThemeService.IsDarkMode;

// Get current theme key
string currentTheme = ThemeService.CurrentTheme;
```

### Reacting to Theme Changes

```csharp
@inject IThemeService ThemeService
@implements IDisposable

protected override void OnInitialized()
{
    ThemeService.OnThemeChanged += HandleThemeChanged;
}

private void HandleThemeChanged()
{
    InvokeAsync(StateHasChanged);
}

public void Dispose()
{
    ThemeService.OnThemeChanged -= HandleThemeChanged;
}
```

## Using CSS Variables in Components

### In Razor Components (Inline Styles)

```razor
<div style="background-color: var(--surface); 
            color: var(--text-primary); 
            border: 1px solid var(--border);">
    Content here
</div>
```

### In Custom CSS Files

```css
.my-custom-component {
    background-color: var(--card-background);
    border: 1px solid var(--card-border);
    color: var(--text-primary);
    box-shadow: var(--shadow-neon);
}

.my-custom-button {
    background-color: var(--button-primary-bg);
    color: var(--button-primary-text);
}

.my-custom-button:hover {
    background-color: var(--button-primary-hover);
}
```

### Example: Custom Card Component

```razor
<div class="custom-card">
    <div class="custom-card-header">
        <h3>@Title</h3>
    </div>
    <div class="custom-card-body">
        @ChildContent
    </div>
</div>

<style>
    .custom-card {
        background-color: var(--card-background);
        border: 1px solid var(--card-border);
        border-radius: 4px;
        box-shadow: var(--shadow-neon);
    }
    
    .custom-card-header {
        padding: 16px;
        border-bottom: 1px solid var(--border);
        color: var(--text-primary);
        font-weight: 600;
    }
    
    .custom-card-body {
        padding: 16px;
        color: var(--text-secondary);
    }
</style>
```

## Adding New Themes

To add a new theme variant:

### 1. Define ThemeInfo in ThemeService.cs

```csharp
public static readonly ThemeInfo MyNewTheme = new()
{
    Key = "my-new-theme-dark",
    Name = "My New Theme Dark",
    Description = "A custom dark theme",
    ColorPreview = "#FF5733",
    IsDark = true
};
```

Add to AvailableThemes list:

```csharp
public static readonly List<ThemeInfo> AvailableThemes = new()
{
    ThemeInfo.CarbonNeonDark,
    ThemeInfo.CarbonNeonLight,
    ThemeInfo.MyNewTheme  // Add your new theme
};
```

### 2. Add CSS Variables in carbon-neon-theme.css

```css
[data-theme="my-new-theme-dark"] {
    --primary: #FF5733;
    --secondary: #C70039;
    --background: #1C1C1C;
    --surface: #2A2A2A;
    /* ... define all required variables ... */
}
```

### 3. Add MudBlazor Palette in MainLayout.razor

For light themes, add to `GetLightPalette()`:

```csharp
"my-new-theme-light" => new PaletteLight()
{
    Primary = "#FF5733",
    Secondary = "#C70039",
    Background = "#F8F8F8",
    Surface = "#FFFFFF",
    TextPrimary = "#333333",
    // ... other properties ...
},
```

For dark themes, add to `GetDarkPalette()`:

```csharp
"my-new-theme-dark" => new PaletteDark()
{
    Primary = "#FF5733",
    Secondary = "#C70039",
    Background = "#1C1C1C",
    Surface = "#2A2A2A",
    TextPrimary = "#E0E0E0",
    // ... other properties ...
},
```

## Component-Specific Styling

### MudBlazor Components

MudBlazor components automatically adopt the theme through:
1. **MudTheme Palettes** - Defined in MainLayout.razor
2. **CSS Variable Overrides** - In carbon-neon-theme.css

### Syncfusion Components

Syncfusion components use CSS variable overrides for theming. The base Syncfusion Material theme is loaded, then overridden with our CSS variables.

### Custom Components

For custom components, use CSS variables directly:

```razor
<MudCard Style="background-color: var(--card-background); 
                border: 1px solid var(--card-border);">
    <MudCardHeader>
        <MudText Color="Color.Primary">@Title</MudText>
    </MudCardHeader>
    <MudCardContent>
        <MudText Style="color: var(--text-secondary);">@Content</MudText>
    </MudCardContent>
</MudCard>
```

## Best Practices

1. **Always use CSS variables** instead of hard-coded colors
2. **Avoid inline styles** where possible; use CSS classes
3. **Test both themes** when adding new components
4. **Use semantic variable names** (e.g., `--text-primary` instead of `--gray-900`)
5. **Maintain contrast ratios** for accessibility (WCAG AA: 4.5:1 minimum)
6. **Use MudBlazor Color enums** when possible (they respect the palette)

## Backward Compatibility

The ThemeService maintains backward compatibility with the old "light" and "dark" keys:
- Old "light" → Maps to "carbon-neon-light"
- Old "dark" → Maps to "carbon-neon-dark"

Existing localStorage values are automatically migrated on first load.

## Troubleshooting

### Theme not applying
1. Check browser console for CSS loading errors
2. Verify `data-theme` attribute on `<html>` element
3. Clear browser cache and localStorage
4. Ensure ThemeService.InitializeAsync() is called in MainLayout

### Colors not matching
1. Verify CSS variable definitions match the design
2. Check MudBlazor palette configuration
3. Inspect element to see which styles are applied
4. Ensure carbon-neon-theme.css is loaded after MudBlazor.css

### Custom components not themed
1. Replace hard-coded colors with CSS variables
2. Use MudBlazor's Color enum values
3. Add proper theme-aware styling in component CSS

## Related Files

- **ThemeService**: `EventForge.Client/Services/ThemeService.cs`
- **MainLayout**: `EventForge.Client/Layout/MainLayout.razor`
- **CSS Theme**: `EventForge.Client/wwwroot/css/themes/carbon-neon-theme.css`
- **Index HTML**: `EventForge.Client/wwwroot/index.html`

## Future Enhancements

Potential improvements to the theming system:
1. User-selectable theme variants
2. Custom color picker for personalization
3. High contrast accessibility mode
4. Seasonal or event-specific themes
5. Theme preview before applying
6. System preference detection (prefers-color-scheme)
