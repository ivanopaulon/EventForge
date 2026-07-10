# PRYM Theming System

## Overview

PRYM uses two official themes:

- `prym-light`
- `prym-dark`

Theme switching is managed by `ThemeService` and propagated through:

1. `data-theme` on `<html>`
2. MudBlazor `MudThemeProvider` palettes (`EventForgeTheme`)
3. CSS variables in `wwwroot/css/themes/prym-theme.css`

## Brand assets

Static assets in `Prym.Web/wwwroot/`:

- `prym-icon.svg` (favicon/app icon/assistant avatar)
- `prym-logo-lockup-light.svg` (for light backgrounds)
- `prym-logo-lockup-dark.svg` (for dark backgrounds)

Header/AppBar and splash screen must switch lockup using `ThemeService.IsDarkMode` and `ThemeService.OnThemeChanged`.

## Theme keys and backward compatibility

Canonical keys:

- `prym-light`
- `prym-dark`

Legacy keys are automatically mapped in `ThemeService`:

- `light` → `prym-light`
- `dark` → `prym-dark`
- `carbon-neon-light` → `prym-light`
- `carbon-neon-dark` → `prym-dark`

## CSS tokens (`wwwroot/css/themes/prym-theme.css`)

### `prym-light`

- `--primary: #2B2F36`
- `--secondary: #5A6068`
- `--accent: #D69A3C`
- `--background: #F2F1EF`
- `--surface: #FFFFFF`
- `--surface-2: #FAFAF8`
- `--card-background: #FFFFFF`
- `--card-border: #E2E1DE`
- `--appbar-background: #2B2F36`
- `--appbar-text: #FFFFFF`
- `--drawer-background: #FFFFFF`
- `--drawer-text: #2B2F36`
- `--text-primary: #22252A`
- `--text-secondary: #666B72`
- `--button-primary-bg: #2B2F36`
- `--button-primary-text: #FFFFFF`
- `--button-primary-hover: #3A3F47`
- `--border: #E2E1DE`
- `--divider: #EDECEA`
- `--shadow-neon: none`

### `prym-dark`

- `--primary: #E8B563`
- `--secondary: #8A939C`
- `--accent: #E8B563`
- `--background: #16181B`
- `--surface: #202327`
- `--surface-2: #292D32`
- `--card-background: #202327`
- `--card-border: rgba(255,255,255,0.08)`
- `--appbar-background: #101214`
- `--appbar-text: #F1F3F5`
- `--drawer-background: #1A1D20`
- `--drawer-text: #F1F3F5`
- `--text-primary: #F1F3F5`
- `--text-secondary: #A7ACB1`
- `--button-primary-bg: #E8B563`
- `--button-primary-text: #16181B`
- `--button-primary-hover: #F0C583`
- `--border: rgba(255,255,255,0.08)`
- `--divider: rgba(255,255,255,0.08)`
- `--shadow-neon: none`

## MudBlazor palettes (`Prym.Web/Services/EventForgeTheme.cs`)

- Light palette: graphite/amber values for `prym-light`
- Dark palette: amber-on-graphite values for `prym-dark`

## Reacting to theme changes in components

Use the standard pattern:

```csharp
protected override void OnInitialized()
{
    ThemeService.OnThemeChanged += HandleThemeChanged;
}

private void HandleThemeChanged()
    => InvokeAsync(StateHasChanged);

public void Dispose()
    => ThemeService.OnThemeChanged -= HandleThemeChanged;
```

Use `ThemeService.IsDarkMode` to choose light/dark-specific assets.

## Files involved

- `Prym.Web/Services/ThemeService.cs`
- `Prym.Web/Services/EventForgeTheme.cs`
- `Prym.Web/Layout/MainLayout.razor`
- `Prym.Web/wwwroot/index.html`
- `Prym.Web/wwwroot/js/app-interop.js`
- `Prym.Web/wwwroot/css/themes/prym-theme.css`
- `Prym.Web/wwwroot/css/variables.css`
