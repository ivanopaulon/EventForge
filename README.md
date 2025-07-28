# EventForge

EventForge is a comprehensive event management and monitoring system built with Blazor Server and MudBlazor.

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
- Click the theme icon in the top navigation bar
- Choose from the dropdown list showing theme names, descriptions, and color previews
- Theme preference is automatically saved and persists across sessions
- All themes support keyboard navigation and screen readers

#### Accessibility Features

- **WCAG Compliance**: All themes meet WCAG AA standards, with High Contrast theme meeting AAA
- **Color Contrast**: Minimum 4.5:1 contrast ratio for normal text, 3:1 for large text
- **Screen Reader Support**: Full ARIA labels and semantic markup
- **Keyboard Navigation**: Complete keyboard access to theme selector
- **Reduced Motion**: Respects user's motion preferences

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
- **ThemeService** - Multi-theme management with 6 color palettes

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
- [ ] Theme switching works properly for all 6 themes
- [ ] Theme preferences persist across browser sessions
- [ ] Authentication flows work correctly
- [ ] All translations display fallback when missing
- [ ] SuperAdmin pages follow consistent UI patterns
- [ ] MudTable sorting and filtering work correctly
- [ ] All interactive elements have translated tooltips
- [ ] Theme selector is accessible via keyboard navigation
- [ ] Screen readers can navigate theme options
- [ ] High contrast theme meets WCAG AAA standards
- [ ] All themes maintain proper color contrast ratios

### Translation Completeness
- [ ] All UI text uses TranslationService
- [ ] English translations complete (fallback language)
- [ ] Italian translations complete (default language)
- [ ] Missing translation keys logged to console
- [ ] Fallback mechanism works properly
- [ ] SuperAdmin pages fully translated
- [ ] No hard-coded text in UI components

### SuperAdmin UI Consistency
- [ ] All pages follow TenantManagement.razor pattern
- [ ] Consistent MudCard vertical layout with mb-4 spacing
- [ ] No SuperAdminBanner references remain
- [ ] Proper authorization checks on all pages
- [ ] MudTable with sortable columns and responsive DataLabel
- [ ] Toolbar actions with semantic icons and tooltips
- [ ] Statistics cards show relevant metrics

## 🎯 UI/UX Best Practices

### Refactored SuperAdmin Pages
The following pages have been updated to follow consistent UI patterns:

#### **TenantSwitch.razor**
- ✅ Vertical card layout: Current Status → Tenant Switch → User Impersonation → History
- ✅ Complete i18n integration with fallback support
- ✅ MudTooltip on all interactive elements
- ✅ Proper MudTable with sortable columns for history

#### **AuditTrail.razor**
- ✅ Vertical card layout: Statistics → Advanced Filters → Data Table → Critical Operations
- ✅ Sortable MudTable with responsive headers
- ✅ Comprehensive filtering options with translations
- ✅ Real-time refresh capabilities with toolbar actions

#### **SystemLogs.razor**
- ✅ Vertical card layout: Statistics → Search Filters → Data Table → Log Trends
- ✅ Advanced log filtering by level, source, and date range
- ✅ Responsive table design for mobile/tablet/desktop
- ✅ Auto-refresh functionality with translated controls

#### **Configuration.razor**
- ✅ Quick Actions card with toolbar for management operations
- ✅ Tab-based configuration categories
- ✅ Enhanced configuration cards with status icons and tooltips
- ✅ Dialog forms with complete translation support

#### **TranslationManagement.razor**
- ✅ Statistics card showing translation completion metrics
- ✅ Advanced filtering by language and search terms
- ✅ Consistent MudTable implementation
- ✅ Bulk operations toolbar for import/export

### Key Improvements Made
1. **Removed SuperAdminBanner** from all refactored pages
2. **Standardized Card Layout** - All cards use `mb-4` spacing and consistent structure
3. **Complete i18n Coverage** - Added 100+ translation keys to en.json
4. **Responsive Design** - All tables use DataLabel for mobile compatibility
5. **Tooltip Integration** - Every interactive element has translated tooltips
6. **Consistent MudTable** - Sortable columns, loading states, and empty state messages
7. **Toolbar Actions** - Semantic icons with proper grouping and tooltips

## 📖 Additional Documentation

- `TRANSLATION_GUIDE.md` - Detailed translation management
- `CUSTOM_THEME_GUIDE.md` - Theme customization
- `DRAWER_IMPLEMENTATION_GUIDE.md` - Navigation drawer patterns
- `EventForge.Server/README.md` - Server-specific documentation