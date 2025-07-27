# EventForge Custom Theme Implementation

## Overview
This document outlines the custom color palette implementation for the EventForge client, as specified in issue #70. The implementation ensures consistent brand colors across the entire application while maintaining accessibility and following best practices.

## Color Palette
The custom theme implements the following color palette:

### Primary Colors
- **Navy Blue (`#1F2F46`)**: Primary color used for headers, navbar, and dark backgrounds
- **Electric Blue (`#247BFF`)**: Secondary color used for primary buttons, links, and highlights  
- **Orange Fire (`#FF6B2C`)**: Accent color used sparingly for CTAs, badges, and active states

### Neutral Colors
- **Light Gray (`#F5F6FA`)**: Neutral light color for backgrounds, containers, and card backgrounds
- **Charcoal (`#2D2D2D`)**: Neutral dark color for text, icons, and borders

## File Structure

### 1. Custom Theme File
**Location**: `wwwroot/css/custom-theme.css`
- Contains all CSS custom properties (variables) for the color palette
- Includes semantic color mappings for different UI components
- Provides accessibility enhancements and media query support
- Includes spacing, shadow, and border radius variables

### 2. Global Import
**Location**: `wwwroot/index.html`
- Custom theme is imported first to establish base variables
- Import order: `custom-theme.css` → `bootstrap.min.css` → `app.css` → `sidepanel.css`

### 3. Updated Application Styles
The following files were updated to use the custom theme variables:

#### Main Application CSS (`wwwroot/css/app.css`)
- Removed duplicate color variable definitions
- Updated button, link, and focus styles to use theme variables
- Updated accessibility styles (skip links, focus indicators)

#### Component-Specific CSS
- **`Layout/MainLayout.razor.css`**: Updated layout styles to use theme variables
- **`Layout/NavMenu.razor.css`**: Updated navigation styles to use theme variables  
- **`wwwroot/css/sidepanel.css`**: Replaced MudBlazor variables with custom theme variables

#### MudBlazor Theme Integration (`Layout/MainLayout.razor`)
- Updated `UpdateTheme()` method to map custom colors to MudBlazor palette
- Ensures MudBlazor components use custom colors instead of defaults
- Maintains dark mode support while applying custom light theme

## Key Features

### 1. Consistent Color Application
- All brand colors are centrally defined in `custom-theme.css`
- Components use semantic variable names (e.g., `--button-primary-bg`)
- No hardcoded colors in component files

### 2. MudBlazor Integration
- Custom colors are properly mapped to MudBlazor's theme system
- Prevents fallback to default MudBlazor/Material Design colors
- Maintains component functionality while applying custom styling

### 3. Accessibility Compliance
- High contrast mode support
- Proper focus indicators using brand colors
- Reduced motion support
- WCAG-compliant color contrast ratios

### 4. Maintainability
- Single source of truth for color definitions
- Semantic naming conventions
- Comprehensive documentation and comments
- Easy to extend for future color additions

## Usage Guidelines

### Adding New Colors
1. Define new variables in `custom-theme.css` following the naming convention
2. Add semantic mappings if needed
3. Update component styles to use the new variables

### Modifying Existing Colors
1. Update values in `custom-theme.css`
2. Verify contrast ratios remain accessible
3. Test across all components

### Best Practices
- Use semantic variable names (e.g., `--button-primary-bg` instead of `--blue`)
- Limit Orange Fire (`--accent`) to high-emphasis elements only
- Test color changes with accessibility tools
- Maintain consistency with the established palette

## Accessibility Testing
The implementation follows WCAG guidelines and should be tested with:
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- Screen readers
- High contrast mode
- Keyboard navigation

## References
- [UI Color Palette Design](https://www.interaction-design.org/literature/article/ui-color-palette)
- [Creating the Best UI Color Palette](https://atmos.style/blog/create-best-ui-color-palette)
- [WCAG Color Contrast Guidelines](https://webaim.org/resources/contrastchecker/)