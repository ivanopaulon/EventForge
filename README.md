# EventForge

EventForge is a comprehensive event management and monitoring system built with Blazor Server and MudBlazor.

## 🎨 UI Design Principles

### MudCard Best Practices
- **Vertical Stack Layout**: All cards are organized in a vertical stack for consistent presentation
- **Full Width**: Cards use full container width for better space utilization
- **Uniform Spacing**: Consistent padding (`pa-4`) and margin (`mb-4`) across all cards
- **Logical Order**: Cards are ordered by importance: Tenant Selection → Statistics → Filters → Actions

### Component Standards
- **MudIconButton**: Use semantic icons with proper contrast and visibility
- **MudTooltip**: All actions must have translated tooltips for accessibility
- **MudToolbar**: Quick actions are collected above data tables
- **Responsive Design**: All components adapt to mobile, tablet, and desktop viewports

### Card Organization Guidelines
1. **Statistics Card** - Always first, shows key metrics
2. **Quick Actions Card** - Second, provides immediate access to common operations  
3. **Data Tables** - Last, with proper sorting and responsive behavior

## 🌍 Translation System

### Supported Languages
- **Italian (it)** - Default language
- **English (en)** - Fallback language
- **Spanish (es)** - Additional language
- **French (fr)** - Additional language

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

### Runtime Language Switching
The system supports dynamic language switching without page reload:
- Language preference is persisted in browser localStorage
- All components automatically update when language changes
- MainLayout subscribes to language change events for immediate UI updates

## 🔧 Development Guidelines

### Adding New Translations
1. Add the key to `EventForge.Client/wwwroot/i18n/en.json` (English as reference)
2. Add corresponding translations to `it.json`, `es.json`, `fr.json`
3. Use the TranslationService in your components
4. Always provide fallback text as the second parameter

### Card Layout Standards
```razor
<MudGrid Spacing="4">
    <!-- Statistics Card -->
    <MudItem xs="12">
        <MudCard Elevation="2" Class="pa-4">
            <MudCardContent>
                <MudText Typo="Typo.h6" Class="mb-4">
                    <MudIcon Icon="Icons.Material.Filled.Dashboard" Class="mr-2" />
                    @TranslationService.GetTranslation("admin.statistics", "Statistics")
                </MudText>
                <!-- Statistics content -->
            </MudCardContent>
        </MudCard>
    </MudItem>

    <!-- Quick Actions Card -->
    <MudItem xs="12">
        <MudCard Elevation="2" Class="pa-4">
            <MudCardContent>
                <MudText Typo="Typo.h6" Class="mb-4">
                    <MudIcon Icon="Icons.Material.Filled.Settings" Class="mr-2" />
                    @TranslationService.GetTranslation("admin.quickActions", "Quick Actions")
                </MudText>
                <!-- Action buttons -->
            </MudCardContent>
        </MudCard>
    </MudItem>
</MudGrid>
```

### Tooltip Best Practices
Always provide translated tooltips for better accessibility:
```razor
<MudTooltip Text="@TranslationService.GetTranslation("action.edit", "Edit item")">
    <MudIconButton Icon="Icons.Material.Filled.Edit" 
                   OnClick="@(() => EditItem(item))" />
</MudTooltip>
```

## 📱 Responsive Design

- **Mobile First**: Components are designed for mobile and scale up
- **Breakpoints**: Uses MudBlazor's responsive grid system
- **Touch Friendly**: Adequate spacing for touch interactions
- **Accessible**: Proper ARIA labels and semantic HTML

## 🏗️ Architecture

### Project Structure
- **EventForge.Client** - Blazor WebAssembly client application
- **EventForge.Server** - ASP.NET Core server application  
- **EventForge.DTOs** - Shared data transfer objects

### Key Services
- **TranslationService** - Handles multi-language support
- **AuthService** - Authentication and authorization
- **ThemeService** - Dark/light theme management

## 🚀 Getting Started

1. **Prerequisites**: .NET 8.0 SDK
2. **Build**: `dotnet build`
3. **Run**: `dotnet run --project EventForge.Server`
4. **Browse**: Navigate to the displayed localhost URL

## 📋 Quality Assurance

### Manual Testing Checklist
- [ ] Language switching updates entire UI immediately
- [ ] All cards display correctly on mobile, tablet, desktop
- [ ] Tooltips show translated text
- [ ] Theme switching works properly
- [ ] Authentication flows work correctly
- [ ] All translations display fallback when missing

### Translation Completeness
- [ ] All UI text uses TranslationService
- [ ] English translations complete (fallback language)
- [ ] Italian translations complete (default language)
- [ ] Missing translation keys logged to console
- [ ] Fallback mechanism works properly

## 📖 Additional Documentation

- `TRANSLATION_GUIDE.md` - Detailed translation management
- `CUSTOM_THEME_GUIDE.md` - Theme customization
- `DRAWER_IMPLEMENTATION_GUIDE.md` - Navigation drawer patterns
- `EventForge.Server/README.md` - Server-specific documentation