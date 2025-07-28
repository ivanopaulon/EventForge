# EventForge Multi-Theme System

## Overview
EventForge features a comprehensive multi-theme system that provides 6 distinct color palettes to accommodate different user preferences, accessibility needs, and usage contexts. The system ensures consistent brand identity while offering variety and flexibility.

## Available Themes

### 1. Light Theme (Default)
- **Purpose**: Modern, professional interface for general use
- **Primary Color**: Navy Blue (`#1F2F46`) - Headers, navigation, emphasis
- **Secondary Color**: Electric Blue (`#247BFF`) - Buttons, links, interactions
- **Accent Color**: Orange Fire (`#FF6B2C`) - CTAs, badges, highlights
- **Background**: Light Gray (`#F5F6FA`) - Main background
- **Use Case**: Default theme, suitable for most business environments

### 2. Dark Theme
- **Purpose**: Comfortable viewing in low-light environments
- **Primary Color**: Dark Navy (`#1a1a2e`) - Dark backgrounds
- **Secondary Color**: Light Blue (`#4fc3f7`) - Buttons, links
- **Accent Color**: Soft Orange (`#ffb74d`) - Highlights, accents
- **Background**: Dark Gray (`#2d2d30`) - Card backgrounds
- **Use Case**: Evening work, reduced eye strain, modern aesthetic

### 3. Warm Theme
- **Purpose**: Welcoming, cozy interface with earthy tones
- **Primary Color**: Rust Red (`#c0392b`) - Headers, emphasis
- **Secondary Color**: Burnt Orange (`#e67e22`) - Buttons, interactions
- **Accent Color**: Golden Yellow (`#f39c12`) - CTAs, highlights
- **Background**: Warm Beige (`#fdf2e9`) - Comfortable background
- **Use Case**: Creative industries, hospitality, friendly environments

### 4. Cool Theme
- **Purpose**: Refreshing, calming interface inspired by nature
- **Primary Color**: Deep Teal (`#006064`) - Headers, navigation
- **Secondary Color**: Cyan Blue (`#0097a7`) - Buttons, links
- **Accent Color**: Mint Green (`#4db6ac`) - Highlights, accents
- **Background**: Ice Blue (`#e0f2f1`) - Soothing background
- **Use Case**: Healthcare, wellness, environmental sectors

### 5. High Contrast Theme
- **Purpose**: Maximum accessibility compliance (WCAG AAA)
- **Primary Color**: Pure Black (`#000000`) - Strong contrast
- **Secondary Color**: Bright Yellow (`#ffeb3b`) - High visibility highlights
- **Accent Color**: Warning Orange (`#ff9800`) - Critical alerts
- **Background**: Pure White (`#ffffff`) - Maximum contrast
- **Use Case**: Visual impairments, bright environments, accessibility requirements

### 6. Fun Theme
- **Purpose**: Vibrant, playful interface for creative applications
- **Primary Color**: Vibrant Purple (`#9c27b0`) - Headers, emphasis
- **Secondary Color**: Hot Pink (`#e91e63`) - Buttons, interactions
- **Accent Color**: Lime Green (`#8bc34a`) - CTAs, highlights
- **Background**: Soft Lavender (`#f3e5f5`) - Playful background
- **Use Case**: Gaming, creative apps, youth-oriented platforms

## File Structure

### Theme CSS Files
Each theme is defined in its own CSS file using the `[data-theme="theme-name"]` selector:

- **`theme-light.css`** - Default light theme with EventForge branding
- **`theme-dark.css`** - Dark theme for low-light environments  
- **`theme-warm.css`** - Warm theme with orange/red/earthy tones
- **`theme-cool.css`** - Cool theme with blue/green/turquoise colors
- **`theme-high-contrast.css`** - High contrast theme for accessibility
- **`theme-fun.css`** - Playful theme with vibrant pastel colors

### Core System Files

#### 1. ThemeService (`Services/ThemeService.cs`)
- Manages theme selection and persistence
- Provides theme metadata (name, description, color preview)
- Handles localStorage integration
- Supports backward compatibility with old light/dark system

#### 2. ThemeSelector Component (`Shared/Components/ThemeSelector.razor`)
- Accessible dropdown for theme selection
- Shows theme name, description, and color preview
- Supports keyboard navigation and screen readers
- Real-time theme switching

#### 3. MainLayout Integration (`Layout/MainLayout.razor`)
- Maps CSS themes to MudBlazor palette system
- Handles theme change events
- Maintains responsive design across themes

### HTML Integration
**Location**: `wwwroot/index.html`
All theme CSS files are imported to ensure availability:
```html
<link rel="stylesheet" href="css/custom-theme.css" />
<link rel="stylesheet" href="css/theme-light.css" />
<link rel="stylesheet" href="css/theme-dark.css" />
<link rel="stylesheet" href="css/theme-warm.css" />
<link rel="stylesheet" href="css/theme-cool.css" />
<link rel="stylesheet" href="css/theme-high-contrast.css" />
<link rel="stylesheet" href="css/theme-fun.css" />
```

## Key Features

### 1. Consistent CSS Variable System
- All themes use identical CSS custom property names
- Semantic naming: `--primary`, `--secondary`, `--accent`, `--background-primary`
- Component-specific variables: `--appbar-background`, `--card-background`
- Status colors: `--success`, `--warning`, `--error`, `--info`

### 2. Theme Selection Interface
- Accessible dropdown in app navigation bar
- Visual previews with color swatches
- Descriptive names and use-case information
- Keyboard navigation support
- Screen reader compatibility

### 3. Automatic Persistence
- Theme selection saved to localStorage
- Automatic application on page load
- Backward compatibility with old boolean system
- Graceful degradation if localStorage unavailable

### 4. MudBlazor Integration
- Themes map to MudBlazor's PaletteLight and PaletteDark
- Consistent component styling across all themes
- Real-time theme switching without page reload
- Maintains Material Design principles

### 5. Accessibility Compliance
- **WCAG AA**: All themes meet minimum contrast requirements
- **WCAG AAA**: High Contrast theme exceeds strict requirements
- **Color Blindness**: Tested for common color vision deficiencies
- **Screen Readers**: Full ARIA labeling and semantic markup
- **Keyboard Navigation**: Complete keyboard accessibility

### 6. Responsive Design
- All themes work across mobile, tablet, and desktop
- Consistent spacing and layout principles
- Touch-friendly interface elements
- Adaptive component behavior

## Usage Guidelines

### Adding New Themes
1. **Create CSS File**: Copy an existing theme file as a template
2. **Update Selector**: Change `[data-theme="existing"]` to `[data-theme="new-theme"]`
3. **Define Colors**: Update color values while maintaining variable names
4. **Test Contrast**: Verify WCAG compliance using accessibility tools
5. **Update ThemeService**: Add theme metadata to the `InitializeThemes()` method
6. **Add Translations**: Include theme name and description in i18n files
7. **Update HTML**: Import the new CSS file in `index.html`

### Modifying Existing Themes
1. **Update CSS Variables**: Modify color values in the theme's CSS file
2. **Verify Contrast**: Test accessibility compliance after changes
3. **Update Metadata**: Adjust descriptions if the theme purpose changes
4. **Test Components**: Ensure all UI elements work with new colors

### Theme Development Best Practices
- **Consistent Naming**: Always use the same variable names across themes
- **Semantic Colors**: Use descriptive names (`--button-primary-bg` not `--blue`)
- **Accessibility First**: Test with accessibility tools and real users
- **Component Testing**: Verify all MudBlazor components work properly
- **Mobile Testing**: Ensure themes work on all device sizes
- **Documentation**: Update guides when adding new themes

### CSS Variable Architecture
```css
[data-theme="theme-name"] {
  /* Primary palette */
  --primary: #color;
  --secondary: #color;
  --accent: #color;
  
  /* Backgrounds */
  --background-primary: #color;
  --background-secondary: #color;
  
  /* Text colors */
  --text-primary: #color;
  --text-secondary: #color;
  
  /* Component-specific */
  --appbar-background: #color;
  --card-background: #color;
  --button-primary-bg: #color;
}
```

## Accessibility Testing
All themes must meet accessibility standards:

### Contrast Requirements
- **Normal Text**: Minimum 4.5:1 contrast ratio (WCAG AA)
- **Large Text**: Minimum 3:1 contrast ratio (WCAG AA)
- **High Contrast Theme**: 7:1+ contrast ratio (WCAG AAA)

### Testing Tools
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [Colour Contrast Analyser (CCA)](https://www.tpgi.com/color-contrast-checker/)
- Browser DevTools accessibility audits
- Screen reader testing (NVDA, JAWS, VoiceOver)

### Testing Checklist
- [ ] All text meets minimum contrast ratios
- [ ] Color is not the only way to convey information
- [ ] Focus indicators are clearly visible
- [ ] Themes work with Windows High Contrast mode
- [ ] Screen readers can navigate theme selector
- [ ] Keyboard-only navigation works properly
- [ ] Themes support reduced motion preferences

## Implementation Examples

### Using Themes in Components
```razor
<!-- Theme-aware component styling -->
<div class="my-component" style="background-color: var(--card-background); color: var(--text-primary);">
    <button style="background-color: var(--button-primary-bg); color: var(--button-primary-text);">
        Primary Action
    </button>
</div>
```

### Accessing Theme Information in Code
```csharp
// Get current theme
var currentTheme = ThemeService.CurrentTheme;

// Get theme metadata
var themeInfo = ThemeService.GetThemeInfo("warm");
Console.WriteLine($"Theme: {themeInfo.Name} - {themeInfo.Description}");

// Set theme programmatically
await ThemeService.SetThemeAsync("dark");
```

### Theme Change Events
```csharp
protected override void OnInitialized()
{
    ThemeService.OnThemeChanged += HandleThemeChange;
}

private void HandleThemeChange()
{
    // React to theme changes
    InvokeAsync(StateHasChanged);
}
```

## References
- [UI Color Palette Design](https://www.interaction-design.org/literature/article/ui-color-palette)
- [Creating the Best UI Color Palette](https://atmos.style/blog/create-best-ui-color-palette)
- [WCAG Color Contrast Guidelines](https://webaim.org/resources/contrastchecker/)
- [CSS Custom Properties](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)
- [MudBlazor Theme System](https://mudblazor.com/features/colors)
- [Accessibility Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Color Universal Design](https://jfly.uni-koeln.de/color/)

## Migration from Old System

### Backward Compatibility
The new multi-theme system maintains backward compatibility with the previous light/dark toggle:
- Old localStorage values (`"true"/"false"`) are automatically converted
- Existing `ToggleThemeAsync()` method still works
- `IsDarkMode` property remains functional

### Migration Steps for Developers
1. **Replace theme toggles** with `<ThemeSelector />` component
2. **Update theme references** from boolean logic to theme names
3. **Test all themes** with existing components
4. **Update documentation** and user guides
5. **Train users** on new theme selection interface

The multi-theme system represents a significant enhancement to EventForge's user experience, providing flexibility, accessibility, and visual appeal while maintaining the professional quality and brand consistency that users expect.