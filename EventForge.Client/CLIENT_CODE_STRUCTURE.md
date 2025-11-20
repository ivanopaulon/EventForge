# EventForge Client Code Structure

## Overview
This document describes the organization and architecture of the EventForge Blazor WebAssembly client application.

## Project Statistics
- **Total Razor Components**: 147
- **Total C# Files**: 69
- **Total CSS Files**: 16
- **Total JavaScript Files**: 4
- **Build Status**: ✅ 0 Errors
- **Last Cleanup**: November 2025 - Phase 2 Client Optimization (removed obsolete assets, added BaseUrl constants)

## Folder Structure

### `/Pages` - Application Pages (64 pages)
Pages are organized by feature domain with nested categorization:

#### Root Pages (5)
- `Home.razor` - Landing page (requires authentication)
- `Login.razor` - Authentication page
- `Profile.razor` - User profile page
- `Admin.razor` - Admin dashboard
- `Error.razor` - Error handling page

#### SuperAdmin (13 pages)
Administrative pages for system-level management:
- `TenantManagement.razor` / `TenantDetail.razor` - Multi-tenant management
- `UserManagement.razor` / `UserDetail.razor` - System user management
- `LicenseManagement.razor` / `LicenseDetail.razor` - License administration
- `TenantSwitch.razor` - Tenant context switching
- `SystemLogs.razor` - System-level logging
- `ClientLogManagement.razor` - Client-side log management
- `AuditTrail.razor` - Audit trail viewer
- `ChatModeration.razor` - Chat moderation tools
- `Configuration.razor` - System configuration
- `TranslationManagement.razor` - I18n management

#### Management/Warehouse (3 pages)
Warehouse and inventory management:
- `WarehouseManagement.razor` / `WarehouseDetail.razor` - Warehouse facilities
- `LotManagement.razor` - Lot tracking
- `InventoryProcedure.razor` - Inventory process (classic implementation)
- `InventoryList.razor` - Inventory document listing

**Note**: Fast Inventory implementations (MudBlazor and Syncfusion) have been removed. Use the classic `InventoryProcedure.razor` for all inventory operations.

#### Management/Products (17 pages)
Product catalog and classification:
- `ProductManagement.razor` / `ProductDetail.razor` - Product CRUD
- `ProductDetailTabs/` - Tab components for product detail page:
  - `GeneralInfoTab.razor` - Basic product information
  - `ProductCodesTab.razor` - Barcode and SKU management
  - `ProductUnitsTab.razor` - Unit of measure
  - `ProductSuppliersTab.razor` - Supplier relationships
  - `PricingFinancialTab.razor` - Pricing and financial data
  - `ClassificationTab.razor` - Product categorization
  - `BundleItemsTab.razor` - Product bundle management
  - `StockInventoryTab.razor` - Stock levels and inventory
- `BrandManagement.razor` / `BrandDetail.razor` - Brand management
- `UnitOfMeasureManagement.razor` / `UnitOfMeasureDetail.razor` - UOM management
- `ClassificationNodeManagement.razor` / `ClassificationNodeDetail.razor` - Category tree

**Note**: `CreateProduct.razor` and `AssignBarcode.razor` pages have been removed. Use `ProductDetail.razor` with route `/products/new` for product creation. Barcode assignment is handled inline in `ProductNotFoundDialog.razor`.

#### Management/Documents (5 pages)
Document management system:
- `DocumentList.razor` - Document listing
- `DocumentTypeManagement.razor` / `DocumentTypeDetail.razor` - Document type definitions
- `DocumentCounterManagement.razor` - Document numbering
- `GenericDocumentProcedure.razor` - Generic document workflow

#### Management/Business (3 pages)
Business partner management:
- `SupplierManagement.razor` - Supplier CRUD
- `CustomerManagement.razor` - Customer CRUD
- `BusinessPartyDetail.razor` - Partner detail page

#### Management/Financial (4 pages)
Financial configuration:
- `VatRateManagement.razor` / `VatRateDetail.razor` - VAT rate management
- `VatNatureManagement.razor` / `VatNatureDetail.razor` - VAT nature codes

#### Notifications (3 pages)
Notification system:
- `NotificationCenter.razor` - Notification hub
- `ActivityFeed.razor` - Activity timeline
- `NotificationPreferences.razor` - User preferences

#### Sales (2 pages)
Point of sale functionality:
- `SalesWizard.razor` - Sales transaction wizard
- `TableManagementStep.razor` - Table management for restaurants

#### Chat (1 page)
- `ChatInterface.razor` - Real-time chat interface

#### Printing (1 page)
- `PrinterManagement.razor` - Printer configuration

### `/Shared/Components` - Reusable Components (52 components)

#### Dialogs (23 components)
Modal dialogs for user interaction:
- **Product Management**: `AddProductCodeDialog`, `AddProductUnitDialog`, `AddProductSupplierDialog`, `AddBundleItemDialog`, `EditProductCodeDialog`, `EditProductUnitDialog`, `EditProductSupplierDialog`, `EditBundleItemDialog`
- **Business**: `AddressDialog`, `ContactDialog`, `ReferenceDialog`
- **Warehouse**: `InventoryEntryDialog`, `EditInventoryRowDialog`, `InventoryDocumentDetailsDialog`, `ProductNotFoundDialog`
- **Documents**: `AddDocumentRowDialog`, `DocumentCounterDialog`
- **System**: `ConfirmationDialog`, `LoadingDialog`, `GlobalLoadingDialog`, `HealthStatusDialog`, `ModelDialog`, `ManageSupplierProductsDialog`

#### Drawers (3 components)
Side panel components (streamlined after deprecation cleanup):
- `ProductDrawer.razor` (2075 lines) - Comprehensive product management (still used in InventoryProcedure)
- `AuditLogDrawer.razor` - Audit log viewer (used in SuperAdmin pages)
- `EntityDrawer.razor` - Generic entity drawer base component

**Removed Deprecated Drawers** (November 2025):
- ~~`BusinessPartyDrawer.razor`~~ → Replaced by `BusinessPartyDetail.razor` page
- ~~`BrandDrawer.razor`~~ → Replaced by `BrandDetail.razor` page
- ~~`StorageLocationDrawer.razor`~~ → Managed in `WarehouseDetail.razor`
- ~~`AuditHistoryDrawer.razor`~~ → Replaced by `AuditHistoryDialog.razor` (fullscreen dialog)

#### Sales Components (3)
- `ProductSearch.razor` - Product search widget
- `CartSummary.razor` - Shopping cart display
- `PaymentPanel.razor` - Payment processing

#### Warehouse Components (3)
Chart and trend visualization components:
- `CommonTrendWrapper.razor` - Common wrapper for trend charts
- `PriceTrendChart.razor` - Product price trend visualization
- `StockTrendChart.razor` - Stock level trend visualization

**Removed Fast Inventory Components** (November 2025):
~~Two implementations (MudBlazor and Syncfusion) have been removed~~
- Fast Inventory implementations were deprecated and removed
- Use classic `InventoryProcedure.razor` for all inventory operations

#### Other Shared Components (19)
- **Navigation**: `UserAccountMenu.razor`, `LanguageSelector.razor`, `ThemeSelector.razor`
- **UI Elements**: `ActionButtonGroup.razor`, `ClassificationNodePicker.razor`, `ProductTabSection.razor`, `SuperAdminCollapsibleSection.razor`, `SuperAdminPageLayout.razor`
- **Help System**: `HelpTooltip.razor`, `InteractiveWalkthrough.razor`, `OnboardingModal.razor`
- **Notifications**: `NotificationBadge.razor`, `NotificationGrouping.razor`, `RichNotificationCard.razor`
- **Chat**: `EnhancedMessage.razor`, `EnhancedMessageComposer.razor`
- **System**: `PageLoadingOverlay.razor`, `HealthFooter.razor`, `LazyAttachmentComponent.razor`
- **Admin**: `SuperAdminBanner.razor`

### `/Services` - Business Logic Layer (60 service files)

#### Core Services
- **Authentication**: `AuthService.cs`, `CustomAuthenticationStateProvider.cs`
- **HTTP Communication**: `HttpClientService.cs`
- **Real-time**: `SignalRService.cs` (1275 lines), `OptimizedSignalRService.cs`
- **System**: `HealthService.cs`, `ConfigurationService.cs`, `BackupService.cs`, `ThemeService.cs`
- **Notifications**: `NotificationService.cs`, `ChatService.cs`
- **Internationalization**: `TranslationService.cs`
- **Context**: `TenantContextService.cs`, `InventorySessionService.cs`
- **Logging**: `ClientLogService.cs`
- **UI**: `HelpService.cs`, `LoadingDialogService.cs`, `PrintingService.cs`
- **Optimization**: `PerformanceOptimizationService.cs`

#### Domain Services

**Warehouse Management**:
- `ILotService.cs` / `LotService.cs`
- `IInventoryService.cs` / `InventoryService.cs`
- `IInventoryFastService.cs` / `InventoryFastService.cs` - **Fast Procedure business logic**
- `IWarehouseService.cs` / `WarehouseService.cs`
- `IStorageLocationService.cs` / `StorageLocationService.cs`
- `IStockService.cs` / `StockService.cs`

**Product Management**:
- `IProductService.cs` / `ProductService.cs`
- `IUMService.cs` / `UMService.cs` - Unit of Measure
- `IBrandService.cs` / `BrandService.cs`
- `IModelService.cs` / `ModelService.cs`

**Document Management**:
- `IDocumentHeaderService.cs` / `DocumentHeaderService.cs`
- `IDocumentTypeService.cs` / `DocumentTypeService.cs`
- `IDocumentCounterService.cs` / `DocumentCounterService.cs`

**Business & Financial**:
- `BusinessPartyService.cs`
- `FinancialService.cs`
- `EntityManagementService.cs`

**Administration**:
- `SuperAdmin/ISuperAdminService.cs` / `SuperAdminService.cs`
- `LogsService.cs`
- `LicenseService.cs`

**Sales** (`Services/Sales/`):
- `ISalesService.cs` / `SalesService.cs`
- `IPaymentMethodService.cs` / `PaymentMethodService.cs`
- `INoteFlagService.cs` / `NoteFlagService.cs`
- `ITableManagementService.cs` / `TableManagementService.cs`

**Events**:
- `EventService.cs`

### `/Layout` - Application Shell (5 files)
- `MainLayout.razor` - Primary application layout
- `MainLayout.razor.css` - Layout-specific styles
- `NavMenu.razor` - Navigation menu with role-based rendering
- `NavMenu.razor.css` - Navigation styles
- `LoginLayout.razor` - Login page layout

### `/Constants` - Application Constants
- `ButtonLabels.cs` - Centralized button label constants

### `/wwwroot` - Static Assets

#### CSS (16 files)
**Core Styles**:
- `app.css` - Main application styles
- `variables.css` - CSS custom properties

**Feature Styles**:
- `sales.css` - Sales module styles
- `sidepanel.css` - Side panel styles
- `help-system.css` - Help system styles
- `icon-color-override.css` - Icon customization
- `product.css` - Product management styles
- `brand.css` - Brand management styles
- `unit-of-measure.css` - Unit of measure styles
- `vat-rate.css` - VAT rate management styles

**Component Styles** (`css/components/`):
- `entity-drawer.css` - Entity drawer styles
- `action-button-group.css` - Action button group styles
- `mud-components.css` - MudBlazor overrides
- `language-selector.css` - Language selector styles

**Theme** (`css/themes/`):
- `carbon-neon-theme.css` - Custom theme implementation

**Bootstrap** (`css/bootstrap/`):
- `bootstrap.min.css` - Bootstrap framework

#### JavaScript (4 files)
- `console-filter.js` - Console log filtering for Mono diagnostics
- `help-system.js` - Help system interactivity
- `qz-setup.js` - QZ Tray printer integration
- `qz-tray.js` - QZ Tray library (installed via npm)

#### Internationalization (`i18n/`)
- `en.json` - English translations
- `it.json` - Italian translations

#### Images (6 files)
- `EventForge.ico` - Application icon
- `favicon.png` - Browser favicon
- `icon-192.png` - PWA icon
- `trace.svg` - Logo SVG
- `login_background.jpg` - Login background
- `login_panel_background.jpg` - Login panel background

## Architecture Patterns

### Service Registration
Services follow a consistent pattern in `Program.cs`:
```csharp
builder.Services.AddScoped<IServiceName, ServiceImplementation>();
```

All domain services use interface-based registration for testability and dependency injection.

### Authentication & Authorization
- Role-based authorization using `[Authorize(Roles = "...")]` attribute
- Custom authentication state provider: `CustomAuthenticationStateProvider`
- Roles: SuperAdmin, Admin, Manager, Operator, Viewer

### Component Communication
- **SignalR**: Real-time updates (notifications, chat)
- **Events**: Service-level event handlers
- **Cascading Parameters**: State propagation
- **Service Injection**: Shared state via services

### State Management
- `TenantContextService` - Multi-tenant context
- `InventorySessionService` - Inventory session persistence
- `TranslationService` - Language/locale management
- `ThemeService` - UI theme management

### Loading Patterns
Three loading components for different use cases:
1. **LoadingDialog** (509 lines) - Full-featured modal with progress, timing, and custom branding
2. **GlobalLoadingDialog** (59 lines) - Service-controlled wrapper around LoadingDialog
3. **PageLoadingOverlay** (45 lines) - Simple overlay for page-level loading states

## Code Quality Standards

### Compilation Status
- ✅ **0 Errors**
- ⚠️ **Warnings**: Primarily informational (RZ10012 for Syncfusion components, some CS1998 for async methods)

### Naming Conventions
- **Pages**: PascalCase descriptive names (e.g., `ProductManagement.razor`)
- **Components**: PascalCase with component type suffix (e.g., `ProductDrawer.razor`, `LoadingDialog.razor`)
- **Services**: Interface-based (`IServiceName.cs` / `ServiceName.cs`)
- **CSS Files**: kebab-case feature names (e.g., `inventory-fast.css`)

### File Size Guidelines
- Pages: Average ~200-400 lines
- Components: Average ~100-300 lines
- Services: Average ~200-400 lines

**Notable Large Files** (candidates for future refactoring):
- `ProductDrawer.razor` - 2075 lines (comprehensive product management)
- `InventoryProcedure.razor` - 1346 lines (complex inventory workflow)
- `SignalRService.cs` - 1275 lines (extensive real-time features)
- `UserManagement.razor` - 1112 lines (comprehensive user admin)
- `BusinessPartyDrawer.razor` - 1001 lines (business partner management)

These files are feature-rich and serve complex business needs. Refactoring should be done carefully with comprehensive testing.

## Recent Improvements (Current PR)

### Phase 1: Compilation Warning Fixes
- ✅ Removed duplicate `using` directives in Syncfusion components
- ✅ Fixed nullable reference warnings (CS8602) in 4 files
- ✅ Fixed async method warnings (CS1998) in 3 files
- ✅ Fixed property hiding warnings (CS0114) in 2 files

### Phase 2: Code Cleanup
- ✅ Removed unused files: `demo.html`, `weather.json`, `enhanced-chat.js`
- ✅ Removed empty `sample-data` folder
- ✅ Verified all CSS files are properly referenced
- ✅ Verified all loading components are actively used
- ✅ Reviewed TODO/FIXME comments (all are for planned features)

## Best Practices

### Adding New Pages
1. Place in appropriate domain folder under `/Pages`
2. Add `@page` directive with route
3. Use `[Authorize]` attribute for protected pages
4. Add navigation link to `NavMenu.razor` if accessible via menu
5. Follow existing naming patterns

### Adding New Components
1. Place in `/Shared/Components` with appropriate subfolder
2. Use component type suffix (Dialog, Drawer, etc.)
3. Create accompanying CSS file if needed (`.razor.css`)
4. Document parameters and usage

### Adding New Services
1. Create interface in `/Services` (e.g., `IMyService.cs`)
2. Implement service (e.g., `MyService.cs`)
3. Add `private const string BaseUrl` at the top of the class (e.g., `"api/v1/myservice"`)
4. Register in `Program.cs` using `AddScoped`
5. Follow constructor injection pattern

**Note**: All services now include `BaseUrl` constants for consistent API endpoint management.

### CSS Organization
1. Feature-specific CSS in separate files
2. Use CSS custom properties from `variables.css`
3. Reference in `index.html` or dynamically load in component
4. Use scoped CSS (`.razor.css`) for component-specific styles

## Known Issues and Future Considerations

### Pages Not in Navigation Menu
- `CreateProduct.razor` (`/products/create`)
- `AssignBarcode.razor` (`/products/assign-barcode`)

These pages have routes but are not accessible via the navigation menu. They may be:
- Legacy pages scheduled for removal
- Admin-accessible via direct navigation
- Feature pages accessed from other contexts

**Recommendation**: Verify usage and either add to navigation or remove if obsolete.

### Large File Refactoring
The following files exceed 1000 lines and could benefit from refactoring:
- `ProductDrawer.razor` (2075 lines) - Consider splitting into tab-based child components
- `InventoryProcedure.razor` (1346 lines) - Evaluate state machine pattern
- `SignalRService.cs` (1275 lines) - Consider splitting by functional area

**Recommendation**: Refactor during dedicated maintenance sprints with comprehensive testing.

### Dual Inventory Implementations
Two parallel inventory implementations exist:
- MudBlazor-based (InventoryProcedureFast)
- Syncfusion-based (InventoryProcedureSyncfusion)

**Recommendation**: Evaluate performance and user preference, consider consolidating to single implementation in future.

## Conclusion

The EventForge client codebase demonstrates:
- ✅ Well-organized folder structure
- ✅ Consistent naming conventions
- ✅ Proper separation of concerns
- ✅ Clean service architecture
- ✅ Minimal technical debt
- ✅ Zero compilation errors

The structure supports maintainability, scalability, and team collaboration effectively.
