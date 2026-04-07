# EventForge - Project Overview

EventForge is a comprehensive event management and monitoring system built with Blazor Server and MudBlazor.

## 🎯 Project Mission

EventForge provides a modern, scalable platform for event management with advanced features including:
- Multi-tenant architecture
- Real-time notifications and chat
- Document management and workflow
- Barcode integration
- Advanced reporting and analytics
- Comprehensive audit system

## 🏗️ Technology Stack

- **Frontend**: Blazor Server with MudBlazor UI components
- **Backend**: .NET 8 with Entity Framework Core
- **Database**: SQL Server with audit logging
- **Real-time**: SignalR for live updates
- **Architecture**: Clean Architecture with CQRS patterns

## 🎨 UI Design Principles

### Multi-Theme Support

EventForge supports 6 distinct color themes to accommodate different user preferences and accessibility needs:

#### Available Themes

1. **Light Theme** - Modern bright theme with EventForge navy blue and electric blue colors
2. **Dark Theme** - Classic dark theme for low-light environments with light blue accents
3. **Warm Theme** - Cozy theme with orange, red, and earthy tones for a welcoming feel
4. **Cool Theme** - Refreshing theme with blue, green, and turquoise colors inspired by nature
5. **High Contrast Theme** - Black, yellow, and white theme for maximum accessibility (WCAG AAA)
6. **Fun Theme** - Playful theme with vibrant purple, pink, and lime green colors

#### Theme Selection

Users can select their preferred theme using the theme selector in the app bar:
- **Location**: Theme selector icon in the top navigation bar (AppBar)
- **Access**: Click the theme icon (varies by current theme) to open the dropdown menu
- **Options**: Choose from 6 available themes with names, descriptions, and color previews
- **Persistence**: Theme preference is automatically saved to localStorage and persists across sessions
- **Real-time**: Theme changes apply immediately without page reload
- **Accessibility**: Full keyboard navigation and screen reader support with ARIA labels

#### Theme Switcher Features

- **Visual Preview**: Each theme option shows a color preview swatch
- **Current Theme Indicator**: Active theme is marked with a checkmark and highlighted
- **Responsive Design**: Theme selector adapts to different screen sizes
- **Icon Contextuality**: Theme selector icon changes based on current theme
- **Instant Application**: No page reload required when switching themes

#### Accessibility Features

- **WCAG Compliance**: All themes meet WCAG AA standards, with High Contrast theme meeting AAA
- **Color Contrast**: Minimum 4.5:1 contrast ratio for normal text, 3:1 for large text
- **Screen Reader Support**: Full ARIA labels and semantic markup
- **Keyboard Navigation**: Complete keyboard access to theme selector
- **Reduced Motion**: Respects user's motion preferences

## 🌍 Translation System

### Supported Languages
- **Italian (it)** - Default language
- **English (en)** - Fallback language

### Translation Key Structure
```json
{
  "common": { "save": "Save", "cancel": "Cancel" },
  "navigation": { "home": "Home", "profile": "Profile" },
  "admin": { "statistics": "Statistics", "quickActions": "Quick Actions" },
  "auth": { "login": "Login", "logout": "Logout" },
  "theme": { "darkMode": "Dark Mode", "lightMode": "Light Mode" },
  "profile": { "title": "My Profile", "basicInfo": "Basic Information" },
  "home": { "title": "Home", "systemStatus": "System Status" }
}
```

### Using Translations in Components
```razor
@inject ITranslationService TranslationService

<!-- Basic usage -->
<MudText>@TranslationService.GetTranslation("common.save", "Save")</MudText>

<!-- With parameters -->
<MudText>@TranslationService.GetTranslation("admin.welcomeBack", "Welcome back, {0}!", user.Name)</MudText>

<!-- With tooltips -->
<MudTooltip Text="@TranslationService.GetTranslation("admin.saveTooltip", "Save changes")">
    <MudIconButton Icon="Icons.Material.Filled.Save" />
</MudTooltip>
```

## 📖 Next Steps

- [Getting Started Guide](./getting-started.md) - Setup and first steps
- [Project Structure](./project-structure.md) - Codebase organization
- [Development Guidelines](../backend/development-guidelines.md) - Development best practices
- [UI Guidelines](../frontend/ui-guidelines.md) - Frontend development guidelines