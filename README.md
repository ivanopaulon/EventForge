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

### MudCard Best Practices
- **Vertical Stack Layout**: All cards are organized in a vertical stack for consistent presentation
- **Full Width Cards**: All cards use `xs="12"` (MudItem) for full container width utilization  
- **Uniform Spacing**: Consistent padding (`pa-4`) and margin (`mb-4`) across all cards
- **Logical Order**: Cards are ordered by importance: Statistics → Filters → Data Tables → Actions
- **Mobile-First Design**: Cards stack vertically on all screen sizes for optimal mobile experience

### Component Standards
- **MudIconButton**: Use semantic icons with proper contrast and visibility
- **MudTooltip**: All actions must have translated tooltips for accessibility
- **MudToolbar**: Quick actions are collected above data tables with semantic icon grouping
- **Responsive Design**: All components adapt to mobile, tablet, and desktop viewports
- **Table Standards**: All tables include sortable columns (MudTableSortLabel) and DataLabel for mobile

### Card Organization Guidelines
1. **Statistics Card** - Always first, shows key metrics and quick overview
2. **Filters Card** - Second, provides search and filtering capabilities  
3. **Data Tables** - Third, with proper sorting, DataLabel, and contextual actions
4. **Quick Actions Card** - Last, provides immediate access to common operations

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
        <MudCard Elevation="2" Class="pa-4 mb-4">
            <MudCardContent>
                <MudText Typo="Typo.h6" Class="mb-4">
                    <MudIcon Icon="Icons.Material.Filled.Dashboard" Class="mr-2" />
                    @TranslationService.GetTranslation("admin.statistics", "Statistics")
                </MudText>
                <!-- Statistics content -->
            </MudCardContent>
        </MudCard>
    </MudItem>

    <!-- Filters Card -->
    <MudItem xs="12">
        <MudCard Elevation="2" Class="pa-4 mb-4">
            <MudCardContent>
                <MudText Typo="Typo.h6" Class="mb-4">
                    <MudIcon Icon="Icons.Material.Filled.FilterList" Class="mr-2" />
                    @TranslationService.GetTranslation("admin.filters", "Filters")
                </MudText>
                <!-- Filter controls -->
            </MudCardContent>
        </MudCard>
    </MudItem>

    <!-- Data Table Card -->
    <MudItem xs="12">
        <MudCard Elevation="2" Class="pa-4 mb-4">
            <MudCardContent>
                <MudText Typo="Typo.h6" Class="mb-4">
                    <MudIcon Icon="Icons.Material.Filled.TableView" Class="mr-2" />
                    @TranslationService.GetTranslation("admin.dataTable", "Data")
                </MudText>
                <!-- MudTable with sortable columns and DataLabel -->
                <MudTable T="DataType" Items="@items">
                    <HeaderContent>
                        <MudTh><MudTableSortLabel SortBy="@(x => x.Property)">
                            @TranslationService.GetTranslation("table.column", "Column")
                        </MudTableSortLabel></MudTh>
                    </HeaderContent>
                    <RowTemplate>
                        <MudTd DataLabel="@TranslationService.GetTranslation("table.column", "Column")">
                            @context.Property
                        </MudTd>
                    </RowTemplate>
                </MudTable>
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

## 🔍 Route Conflict Detection & Swagger Maintenance

EventForge include un sistema automatizzato per rilevare conflitti di route HTTP che possono impedire la generazione corretta del file Swagger/OpenAPI.

### Strumenti Disponibili

#### 1. Script Automatizzato di Analisi
```bash
# Linux/macOS
./analyze-routes.sh

# Windows
analyze-routes.bat

# Con parametri personalizzati
./analyze-routes.sh "percorso/custom/Controllers" "report_personalizzato.txt"
```

#### 2. Applicazione Console Diretta
```bash
cd RouteConflictAnalyzer
dotnet run -- "../EventForge.Server/Controllers" "../report.txt"
```

### Output dell'Analisi

Lo script genera un report completo che include:
- **Mapping completo** di tutte le route con HTTP methods
- **Rilevamento conflitti** con route duplicate o ambigue
- **Soluzioni suggerite** per ogni conflitto trovato
- **Statistiche** di distribuzione delle route per controller e metodo HTTP

### Risoluzione Conflitti

1. **Consulta la Checklist**: Leggi `SWAGGER_ROUTE_CONFLICTS_CHECKLIST.md` per la procedura dettagliata
2. **Analizza il Report**: Esamina il file di output per identificare i conflitti
3. **Applica le Correzioni**: Utilizza le soluzioni suggerite nel report
4. **Ri-esegui l'Analisi**: Verifica che i conflitti siano stati risolti

### Integrazione nel Workflow di Sviluppo

- **Pre-commit**: Esegui l'analisi prima di ogni commit che modifica i controller
- **CI/CD**: Integra lo script nel pipeline per rilevare conflitti automaticamente
- **Code Review**: Utilizza il report per documentare le modifiche alle route

### Esempio di Conflitto e Risoluzione

```csharp
// ❌ CONFLITTO: Due route identiche
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUser(Guid id) { }

[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUserDetails(Guid id) { }

// ✅ RISOLTO: Route differenziate
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUser(Guid id) { }

[HttpGet("{id}/details")]
public async Task<ActionResult<UserDto>> GetUserDetails(Guid id) { }
```

## 📋 Quality Assurance

### Manual Testing Checklist
- [x] Language switching updates entire UI immediately
- [x] All cards display correctly on mobile, tablet, desktop with xs="12" layout
- [x] Tooltips show translated text
- [x] Theme switching works properly for all 6 themes (light, dark, warm, cool, high-contrast, fun)
- [x] Theme preferences persist across browser sessions via localStorage
- [x] Authentication flows work correctly with conditional Home page content
- [x] All translations display fallback when missing
- [x] SuperAdmin pages follow consistent vertical layout patterns (xs="12")
- [x] MudTable sorting and filtering work correctly with sortable columns
- [x] All interactive elements have translated tooltips
- [x] Theme selector is accessible via keyboard navigation with ARIA labels
- [x] Screen readers can navigate theme options
- [x] High contrast theme meets WCAG AAA standards
- [x] All themes maintain proper color contrast ratios
- [x] Home page login card bug fixed (shows only when not authenticated)
- [x] Cards have uniform heights and consistent spacing

### Translation Completeness
- [ ] All UI text uses TranslationService
- [ ] English translations complete (fallback language)
- [ ] Italian translations complete (default language)
- [ ] Missing translation keys logged to console
- [ ] Fallback mechanism works properly
- [ ] SuperAdmin pages fully translated
- [ ] No hard-coded text in UI components

### SuperAdmin UI Consistency
- [x] All pages follow consistent vertical layout pattern with xs="12" cards
- [x] Consistent MudCard vertical layout with mb-4 spacing and pa-4 padding
- [x] No SuperAdminBanner references remain (removed in previous refactoring)
- [x] Proper authorization checks on all pages
- [x] MudTable with sortable columns (MudTableSortLabel) and responsive DataLabel
- [x] Toolbar actions with semantic icons and translated tooltips
- [x] Statistics cards show relevant metrics at the top of each page
- [x] Home page login card bug resolved (conditional rendering)
- [x] Uniform card heights and consistent visual hierarchy
- [x] Mobile-responsive design with DataLabel attributes

## 🎯 UI/UX Best Practices

### Refactored SuperAdmin Pages
The following pages have been updated to follow consistent UI patterns:

#### **Home.razor**
- ✅ **Fixed login card bug**: Login card only shows for unauthenticated users
- ✅ **Vertical card layout**: All cards use xs="12" for full-width stacking
- ✅ **Authentication-aware content**: Dashboard view for authenticated users
- ✅ **Responsive system status**: Adaptive grid layout for different screen sizes
- ✅ **Uniform card heights**: Consistent padding and structure across all cards

#### **TenantSwitch.razor**
- ✅ Vertical card layout: Current Status → Tenant Switch → User Impersonation → History
- ✅ Complete i18n integration with fallback support
- ✅ MudTooltip on all interactive elements
- ✅ Proper MudTable with sortable columns and DataLabel for mobile
- ✅ Contextual actions with translated tooltips

#### **SystemLogs.razor**
- ✅ Vertical card layout: Statistics → Search Filters → Data Table → Log Trends
- ✅ Advanced log filtering by level, source, and date range
- ✅ Responsive table design for mobile/tablet/desktop
- ✅ Auto-refresh functionality with translated controls
- ✅ Full-width cards (xs="12") for consistent presentation

#### **AuditTrail.razor**
- ✅ Vertical card layout: Statistics → Advanced Filters → Data Table → Critical Operations
- ✅ Sortable MudTable with responsive headers
- ✅ Comprehensive filtering options with translations
- ✅ Real-time refresh capabilities with toolbar actions
- ✅ Full-width layout for optimal space utilization

#### **TranslationManagement.razor**
- ✅ Statistics card showing translation completion metrics
- ✅ Advanced filtering by language and search terms
- ✅ Consistent MudTable implementation with sortable columns
- ✅ Bulk operations toolbar for import/export
- ✅ Vertical layout pattern with xs="12" cards

#### **Configuration.razor**
- ✅ Quick Actions card with toolbar for management operations
- ✅ Tab-based configuration categories
- ✅ Enhanced configuration cards with status icons and tooltips
- ✅ Dialog forms with complete translation support

### Key Improvements Made
1. **Fixed Home Page Bug** - Login card now conditionally renders based on authentication state
2. **Standardized Card Layout** - All cards use xs="12" for full-width vertical stacking with consistent `mb-4` spacing
3. **Complete i18n Coverage** - Added 100+ translation keys to en.json and maintained fallback support
4. **Enhanced Responsive Design** - All tables use DataLabel for mobile compatibility, cards adapt to all screen sizes
5. **Improved Table Functionality** - Sortable columns (MudTableSortLabel), loading states, and contextual actions
6. **Consistent Toolbar Actions** - Semantic icons with proper grouping, tooltips, and translation support
7. **Theme Switcher Enhancement** - Documented 6-theme system with localStorage persistence and accessibility features
8. **Authentication-Aware UI** - Home page shows different content based on user authentication and role status

## 📖 Additional Documentation

- `TRANSLATION_GUIDE.md` - Detailed translation management
- `CUSTOM_THEME_GUIDE.md` - Theme customization
- `DRAWER_IMPLEMENTATION_GUIDE.md` - Navigation drawer patterns
- `EventForge.Server/README.md` - Server-specific documentation