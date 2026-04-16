# EventForge Client Code Structure

## Overview
This document describes the organization and architecture of the EventForge Blazor WebAssembly client application.

## Project Statistics
- **Total Razor Files**: ~230 (pages + components)
- **Total C# Files**: ~130
- **Build Status**: ✅ 0 Errors
- **Last Reorganization**: April 2026 – Full project structure reorganization (Components/ consolidated into Shared/Components/, unused files deleted, Chat dialogs moved out of Pages/)

## Folder Structure

### `/Pages` — Application Pages

Pages are routable Blazor components (with `@page` directive), organized by feature domain.

#### Root Pages
- `Home.razor` — Landing page (requires auth)
- `Login.razor` — Authentication
- `Profile.razor` — User profile
- `Admin.razor` — Admin dashboard (`/admin`)
- `Error.razor` — Error handling

#### `/Pages/Admin`
- `FiscalPrintersDashboard.razor` — `/admin/fiscal-printers`
- `FiscalPrinterSetupWizard.razor` — `/admin/fiscal-printers/setup`
- `ClosureHistory.razor` — `/admin/fiscal-printers/{id}/closures`

#### `/Pages/Events`
- `EventManagement.razor` — `/events/management`

#### `/Pages/Management/Analytics`
- `AnalyticsDashboard.razor` — `/management/analytics`

#### `/Pages/Management/Business`
- `BusinessPartyManagement.razor` — `/business/parties`
- `BusinessPartyGroupManagement.razor` — `/business/groups`
- `CustomerManagement.razor` — `/business/customers`
- `SupplierManagement.razor` — `/business/suppliers`
- **Dialog components** (no `@page`, used via `DialogService`):
  - `BusinessPartyDetailDialog.razor`
  - `BusinessPartyGroupDetailDialog.razor`
- **Tab sub-components** (`BusinessPartyDetailTabs/`):
  - `GeneralInfoTab.razor`, `AddressesTab.razor`, `ContactsTab.razor`
  - `RecapitiTab.razor`, `ReferencesTab.razor`, `AccountingTab.razor`
  - `CommercialeTab.razor`, `OperativoTab.razor`

#### `/Pages/Management/Documents`
- `DocumentList.razor` — `/documents/list`
- `DocumentTypeManagement.razor` — `/documents/types`
- `DocumentCounterManagement.razor` — `/documents/counters`
- `GenericDocumentProcedure.razor` — `/documents/create`, `/documents/edit/{id}`
- **Dialog component**: `DocumentDetailDialog.razor`
- **Dialog component**: `DocumentTypeDetailDialog.razor`

#### `/Pages/Management/Financial`
- `VatRateManagement.razor` — `/financial/vat-rates`
- `VatNatureManagement.razor` — `/financial/vat-natures`
- **Dialog components**: `VatRateDetailDialog.razor`, `VatNatureDetailDialog.razor`

#### `/Pages/Management/Monitoring`
- `MonitoringDashboard.razor` — `/management/monitoring`

#### `/Pages/Management/PriceLists`
- `PriceListManagement.razor` — `/management/pricelists`
- **Dialog component**: `PriceListDetailDialog.razor`

#### `/Pages/Management/Products`
- `ProductManagement.razor` — `/product-management/products`
- `BrandManagement.razor` — `/product-management/brands`
- `ClassificationNodeManagement.razor` — `/management/classification-nodes`
- `UnitOfMeasureManagement.razor` — `/settings/unit-of-measures`
- **Dialog components**: `ProductDetailDialog.razor`, `BrandDetailDialog.razor`, `ClassificationNodeDetailDialog.razor`, `UnitOfMeasureDetailDialog.razor`
- **Tab sub-components** (`ProductDetailTabs/`):
  - `GeneralInfoTab.razor`, `ProductCodesTab.razor`, `ProductUnitsTab.razor`
  - `ProductSuppliersTab.razor`, `PricingFinancialTab.razor`, `ClassificationTab.razor`
  - `BundleItemsTab.razor`, `StockInventoryTab.razor`

#### `/Pages/Management/Promotions`
- `PromotionManagement.razor` — `/management/promotions`
- `PromotionNew.razor` — `/management/promotions/new`
- **Dialog component**: `PromotionDetailDialog.razor`

#### `/Pages/Management/Reports`
- `ReportsList.razor` — `/management/reports`
- `ReportDesigner.razor` — `/management/reports/designer`
- `ReportViewer.razor` — `/management/reports/viewer/{id}`
- **Dialog component**: `CreateReportDialog.razor`

#### `/Pages/Management/Store`
> All store-management pages are co-located here (including payment methods, moved here from `Pages/Store/` in April 2026).
- `FiscalDrawerManagement.razor` / `FiscalDrawerDetailDialog.razor`
- `OperatorManagement.razor` / `OperatorDetailDialog.razor`
- `OperatorGroupManagement.razor` / `OperatorGroupDetailDialog.razor`
- `PaymentMethodManagement.razor` / `PaymentMethodDetailDialog.razor`
- `PaymentTerminalManagement.razor` / `PaymentTerminalDetailDialog.razor`
- `PosManagement.razor` / `PosDetailDialog.razor`
- `PrinterManagement.razor` / `PrinterDetailDialog.razor`
- `StationManagement.razor` / `StationDetailDialog.razor`

#### `/Pages/Management/Warehouse`
- `WarehouseManagement.razor`, `LotManagement.razor`, `StockManagement.razor`
- `StockOverview.razor`, `StockReconciliation.razor`
- `InventoryProcedure.razor`, `InventoryDiagnostics.razor`, `InventoryMerge.razor`
- `TransferOrderManagement.razor`
- **Dialog components**: `WarehouseDetailDialog.razor`, `TransferOrderDetailDialog.razor`, `StockFiltersDialog.razor`

#### `/Pages/Notifications`
- `NotificationCenter.razor` — `/notifications`
- `ActivityFeed.razor` — `/activity-feed`
- `NotificationPreferences.razor` — `/notifications/preferences`

#### `/Pages/Sales`
- `POS.razor` — `/sales/pos` (classic POS)
- `POSTouch.razor` — `/sales/postouch` (touch-optimized POS)
- `POS2026.razor` — `/sales/pos2026` (next-gen POS, uses `Shared/Components/Sales/Pos26/`)
- `SalesDashboard.razor` — `/sales/dashboard`

---

### `/Shared` — Shared Razor Infrastructure

#### `/Shared/Components` — Reusable Components
All reusable, non-routable components live here, organized by feature.

##### `/Shared/Components/Business`
Business party selector, tabs and related dialogs (consolidated from former `Shared/BusinessParty/`):
- `UnifiedBusinessPartySelector.razor` — Unified search/select with create and edit modes
- `PriceListAssignmentCard.razor`, `PriceListPreviewDialog.razor`
- `BusinessPartyDocumentsTab.razor`, `BusinessPartyProductsTab.razor`
- `BusinessPartySuppliedProductsTab.razor`, `ProductDocumentsDialog.razor`

##### `/Shared/Components/Common`
Cross-cutting atoms and enums:
- `GroupBadge.razor` — Business party group badge chip
- `SelectorBehaviorEnums.cs` — `EntityEditMode`, `EntityCreateMode`, `EntityDisplayMode` enums used by unified selectors

##### `/Shared/Components/Dashboard`
- `ManagementDashboard.razor` — Configurable widget dashboard
- `DashboardModels.cs` — Dashboard configuration models

##### `/Shared/Components/Dialogs`
All modal dialogs organized by domain:

- **Root dialogs**: `EFDialog.razor` (base), `EFSystemDialog.razor`, `AddressDialog.razor`, `ContactDialog.razor`, `ReferenceDialog.razor`, `AuditHistoryDialog.razor`, `ErrorDetailDialog.razor`, `LogDetailDialog.razor`, `ExportDialog.razor`, `ImportCsvDialog.razor`, `ProfileDialog.razor`, `ChangePasswordForm.razor`, `UpdatesDialog.razor`, `HealthStatusDialog.razor`, `FiscalPrinterStatusDialog.razor`, `FontPreferencesDialog.razor`, `NotificationCenterDialog.razor`, `NotificationDetailDialog.razor`, `DocumentViewerDialog.razor`, `ModelDialog.razor`, `DashboardConfigurationDialog.razor`, `ColumnConfigurationDialog.razor`, `MetricEditorDialog.razor`, `ComposeMessageDialog.razor`, `NewChatDialog.razor`, `GlobalLoadingOverlay.razor`, `AutoRepairOptionsDialog.razor`
- **Products**: `QuickCreateProductDialog.razor`, `AdvancedQuickCreateProductDialog.razor`, `EditProductCodeDialog.razor`, `EditProductSupplierDialog.razor`, `EditBundleItemDialog.razor`, `ProductUnitDialog.razor`, `ProductNotFoundDialog.razor`, `BulkVatUpdateDialog.razor`
- **Price Lists**: `CreatePriceListDialog.razor`, `DuplicatePriceListDialog.razor`, `ImportPriceListDialog.razor`, `EditPriceListEntryDialog.razor`, `GenerateFromDefaultPricesDialog.razor`, `GeneratePriceListFromDocumentsDialog.razor`, `BulkUpdatePricesDialog.razor`
- **Suppliers**: `AssignProductToSupplierDialog.razor`, `AssignBusinessPartyDialog.razor`, `EditBusinessPartyAssignmentDialog.razor`, `ManageSupplierProductsDialog.razor`, `BulkEditSupplierProductsDialog.razor`, `SupplierSuggestionDialog.razor`
- **Warehouse**: `InventoryRowDialog.razor`, `StorageLocationDialog.razor`, `StockReconciliationConfirmDialog.razor`, `RebuildMovementsDialog.razor`, `ManageActiveInventoriesDialog.razor`
- **`/Dialogs/Business`**: `QuickCreateBusinessPartyDialog.razor`, `ManageGroupMembersDialog.razor`, `EditGroupMemberDialog.razor`
- **`/Dialogs/Chat`** *(moved from Pages/Chat/ in April 2026)*: `ChatDialog.razor`, `ChatSettingsDialog.razor`, `StartConversationDialog.razor`, `WhatsAppConfigDialog.razor`
- **`/Dialogs/Documents`**: `DocumentHeaderDialog.razor`, `DocumentCounterDialog.razor`, `DocumentRowDialog.razor` (+panels: `DocumentRowDiscountsPanel.razor`, `DocumentRowNotesPanel.razor`, `DocumentRowQuantityPrice.razor`, `DocumentRowRecentTransactions.razor`, `DocumentRowSummary.razor`, `DocumentRowVatPanel.razor`)
- **`/Dialogs/Events`**: `CreateEventDialog.razor`, `CreateCalendarReminderDialog.razor`
- **`/Dialogs/Sales`**: `PaymentDialog.razor`, `SplitPaymentDialog.razor`, `GlobalDiscountDialog.razor`, `CouponInputDialog.razor`, `FiscalDrawerTransactionDialog.razor`, `ItemNotesDialog.razor`, `MergeSessionsDialog.razor`, `SessionNoteDialog.razor`, `POSTouchLineEditDialog.razor`
- **`/Dialogs/SystemDialogs`**: `ConnectionLostSystemDialog.razor`, `LoginSystemDialog.razor`, `PageLoadingSystemDialog.razor`, `ServerConfigSystemDialog.razor`, `UpdateMaintenanceSystemDialog.razor`
- **`/Dialogs/UnifiedInventoryDialog`**: `UnifiedInventoryDialog.razor` (+steps: `InventoryConfirmStep.razor`, `InventoryEditStep.razor`, `InventoryHistoryStep.razor`, `InventoryViewStep.razor`)
- **`/Dialogs/Warehouse`**: `CreateTransferOrderDialog.razor`, `InventoryViewDialog.razor`, `MergeInventoriesDialog.razor`, `ReceiveTransferOrderDialog.razor`, `ShipTransferOrderDialog.razor`

##### `/Shared/Components/Documents`
Display-only document components (consolidated from former `Shared/Documents/`):
- `DocumentTotalsCard.razor` — Totals summary card
- `DocumentVatSummary.razor` — VAT breakdown table

##### `/Shared/Components/Drawers`
- `EntityDrawer.razor` — Slide-in side panel base component

##### `/Shared/Components/Export`
- `ExportModels.cs` — `ExportColumnConfig`, `ExportFormat`, `ExportRequest` used by `EFTable.razor`

##### `/Shared/Components/Fidelity`
- `FidelityCardList.razor`, `FidelityCardItem.razor`
- `CreateFidelityCardDialog.razor`, `EditFidelityCardDialog.razor`
- `FidelityPointsHistoryDialog.razor`, `ManageFidelityPointsDialog.razor`

##### `/Shared/Components/FiscalPrinting`
- `DailyClosureDialog.razor`

##### `/Shared/Components/MetricBuilder`
- `FieldSelector.razor`, `FilterBuilder.razor`

##### `/Shared/Components/PriceList`
- `BusinessPartyAssignmentList.razor`

##### `/Shared/Components/Products`
- `ProductQuickInfo.razor`

##### `/Shared/Components/Reports`
Embeddable Syncfusion report viewer/designer components (renamed from `*Component` suffix in April 2026):
- `ReportDesignerPanel.razor` — Embedded Syncfusion report designer
- `ReportViewerPanel.razor` — Embedded Syncfusion report viewer

##### `/Shared/Components/Sales`
Sales UI components:
- `CartSummary.razor`, `POSCartTable.razor`, `POSHeader.razor`, `POSReceipt.razor`
- `POSActionButton.razor`, `POSScanModeToggle.razor`, `PaymentPanel.razor`
- `POSTouchCartList.razor`, `POSTouchNumericKeypad.razor`, `POSTouchOperatorGrid.razor`
- `ProductPreviewCard.razor`

##### `/Shared/Components/Sales/Pos26`
POS2026-specific UI components (consolidated from former root `Components/Pos26/` in April 2026):
- `Pos26SearchBar.razor`, `Pos26SortBar.razor`, `Pos26CategoryBar.razor`, `Pos26ContextBanner.razor`
- `Pos26ProductGrid.razor`, `Pos26ProductCard.razor`
- `Pos26Receipt.razor`, `Pos26Numpad.razor`
- `Pos26PaymentDialog.razor`, `Pos26PaymentMethodCard.razor`, `Pos26PaymentRow.razor`

##### `/Shared/Components/Warehouse`
Warehouse analytics and inventory management:
- `ActiveInventoriesManager.razor`, `InventoryBarcodeAuditPanel.razor`
- `CommonTrendWrapper.razor`, `PriceTrendChart.razor`, `StockTrendChart.razor`

##### Top-level Shared Components
General-purpose components at `Shared/Components/`:
- **UI**: `EFTable.razor`, `EFTableColumnHeader.razor`, `EFDateTimeRangePicker.razor`, `EFOverlay.razor`
- **Navigation/Actions**: `ActionButtonGroup.razor`, `ManagementTableToolbar.razor`, `QuickFilters.razor`, `ScrollToTop.razor`
- **Product**: `UnifiedProductSelector.razor`, `ClassificationNodePicker.razor`, `ProductTabSection.razor`
- **Notifications/Overlays**: `NotificationBadge.razor`, `NotificationGrouping.razor`, `RichNotificationCard.razor`, `ConnectionLostOverlay.razor`, `LoginOverlay.razor`, `ServerConfigOverlay.razor`, `PageLoadingOverlay.razor`, `UpdateMaintenanceOverlay.razor`, `GlobalErrorHandler.razor`
- **Chat/Messaging**: `FloatingChat.razor`, `EnhancedMessage.razor`, `EnhancedMessageComposer.razor`
- **Floating UI**: `FloatingFabMenu.razor`, `FloatingProfile.razor`
- **Scanning**: `CameraBarcodeScannerDialog.razor`
- **User/Preferences**: `UserAccountMenu.razor`, `ThemeSelector.razor`, `LanguageSelector.razor`, `FontPreferencesDialog.razor` (via Dialogs)
- **Help/Onboarding**: `HelpTooltip.razor`, `InteractiveWalkthrough.razor`, `OnboardingModal.razor`
- **Updates/Downloads**: `UpdateDownloadSnackbar.razor`, `DownloadProgressSnackbar.razor`, `LogCleanupNotificationSnackbar.razor`
- **System**: `HealthFooter.razor`, `FiscalPrinterStatusIndicator.razor`, `LazyAttachmentComponent.razor`

#### `/Shared/Management` — Generic CRUD Infrastructure
- `EntityManagementPage.razor` — Generic management page base (table + toolbar + paging)
- `ManagementPageHeader.razor` — Reusable page header with title and actions
- `EntityManagementConfig.cs` — Configuration class for the generic page
- `IEntityManagementService.cs` — Interface for management adapters
- `/Adapters/` — 25 concrete adapters (one per entity: `ProductManagementService.cs`, `BusinessPartyManagementService.cs`, etc.)

#### `/Shared/Helpers`
Chat-related utility classes (used from both `Shared/Components/` and `Pages/Chat/`):
- `ChatEditorConfig.cs` — Syncfusion rich-text editor toolbar configuration
- `ChatMarkdownRenderer.cs` — Markdown-to-HTML renderer for chat messages

#### `/Shared/BusinessParty` — ❌ DELETED (April 2026)
Contents merged into `/Shared/Components/Business/`.

#### `/Shared/Atoms` — ❌ DELETED (April 2026)
- `GroupBadge.razor` → moved to `/Shared/Components/Common/`
- `FidelityCardPlaceholder.razor` → deleted (unused placeholder)
- `PriceListBadge.razor` → deleted (unused placeholder)

---

### `/Layout` — Application Shell
- `MainLayout.razor` — Primary layout (contains `NavMenu`, `FloatingFabMenu`, system overlays)
- `LoginLayout.razor` — Login page layout
- `NavMenu.razor` — Role-based navigation sidebar

---

### `/Services` — Business Logic Layer

#### Core Services
- **Auth**: `AuthService.cs`, `CustomAuthenticationStateProvider.cs`
- **HTTP**: `HttpClientService.cs`
- **Real-time**: `OptimizedSignalRService.cs`
- **System**: `HealthService.cs`, `ConfigurationService.cs`, `BackupService.cs`, `ThemeService.cs`, `ServerConfigService.cs`
- **Notifications**: `AppNotificationService.cs`, `NotificationService.cs`, `UpdateNotificationService.cs`
- **Chat**: `ChatService.cs`
- **i18n**: `TranslationService.cs`
- **Context**: `TenantContextService.cs`, `InventorySessionService.cs`
- **Logging**: `ClientLogService.cs`, `LogManagementService.cs`
- **UI**: `HelpService.cs`, `LoadingDialogService.cs`, `FontPreferencesService.cs`, `FilterStateService.cs`, `TablePreferencesService.cs`
- **Optimization**: `PerformanceOptimizationService.cs`, `LookupCacheService.cs`

#### Domain Services (by subdirectory)
- **`/Documents`**: `DocumentRowCalculationService.cs`, `DocumentRowValidator.cs`, `DocumentDialogCacheService.cs`, `CsvImportService.cs`
- **`/Sales`**: `SalesService.cs`, `PaymentMethodService.cs`, `NoteFlagService.cs`, `TableManagementService.cs`
- **`/Store`**: `FiscalDrawerService.cs`, `StorePosService.cs`, `StoreUserService.cs`, `StoreUserGroupService.cs`, `PaymentTerminalService.cs`
- **`/Station`**: `StationService.cs`
- **`/WhatsApp`**: `WhatsAppClientService.cs`
- **`/External`**: `VatLookupService.cs`
- **`/Updates`**: `UpdateClientDtos.cs`
- **`/Schema`**: `EntitySchemaProvider.cs`
- **`/Common`**: `DebouncedAction.cs`
- **`/Mock`**: `MockFidelityService.cs` — registered in production (placeholder for fidelity API integration)

#### Top-level Domain Services
`ProductService.cs`, `BrandService.cs`, `BusinessPartyService.cs`, `BusinessPartyGroupService.cs`, `FinancialService.cs`, `WarehouseService.cs`, `InventoryService.cs`, `StockService.cs`, `LotService.cs`, `StorageLocationService.cs`, `TransferOrderService.cs`, `DocumentHeaderService.cs`, `DocumentTypeService.cs`, `DocumentCounterService.cs`, `PriceListService.cs`, `PromotionClientService.cs`, `AnalyticsService.cs`, `MonitoringClientService.cs`, `EventService.cs`, `ProfileService.cs`, `LicenseService.cs`, `UMService.cs`, `ModelService.cs`, `EntityManagementService.cs`, `PriceResolutionService.cs`, `StockReconciliationService.cs`, `SupplierSuggestionService.cs`, `CalendarReminderService.cs`, `ReportDefinitionService.cs`, `FiscalPrintingService.cs`, `DevToolsService.cs`, `BrandingService.cs`

---

### `/Models` — Data and State Models
- **`/Documents`**: `DocumentRowDialogState.cs`, `DocumentRowCalculationCache.cs`, `DocumentRowCalculationModels.cs`, `ContinuousScanEntry.cs`, `DialogMode.cs`
- **`/Fidelity`**: `FidelityCardViewModel.cs`, `FidelityPointsTransactionViewModel.cs`
- **`/Messaging`**: `MessagingModels.cs`
- **`/Sales`**: `Pos26Models.cs`

### `/ViewModels` — Page/Dialog State ViewModels
21 view models for entity detail forms:
`BaseEntityDetailViewModel.cs`, `ProductDetailViewModel.cs`, `BusinessPartyDetailViewModel.cs`, `DocumentHeaderDetailViewModel.cs`, `DocumentTypeDetailViewModel.cs`, `DocumentCounterDetailViewModel.cs`, `VatRateDetailViewModel.cs`, `VatNatureDetailViewModel.cs`, `WarehouseDetailViewModel.cs`, `LotDetailViewModel.cs`, `InventoryDetailViewModel.cs`, `OperatorDetailViewModel.cs`, `OperatorGroupDetailViewModel.cs`, `FiscalDrawerDetailViewModel.cs`, `PaymentTerminalDetailViewModel.cs`, `PaymentTermDetailViewModel.cs`, `PosDetailViewModel.cs`, `PrinterDetailViewModel.cs`, `StationDetailViewModel.cs`, `StorageLocationDetailViewModel.cs`, `POSViewModel.cs`

### `/Constants`
- `ButtonLabels.cs` — Centralized button label string constants

### `/Extensions`
- `SearchExtensions.cs` — LINQ search helper extensions

### `/Helpers`
- `CacheHelper.cs` — Cache key constants and options factory (used by service layer)

### `/Scripts`
- `Setup-EventForge-Client-IIS.ps1` — IIS deployment setup script

---

### `/wwwroot` — Static Assets

#### CSS
- `app.css` — Main styles (includes `.plsd-*` progress-log classes for system dialogs)
- `variables.css` — CSS custom properties
- Feature styles: `sales.css`, `sidepanel.css`, `help-system.css`, `product.css`, `brand.css`, etc.
- Component styles (`css/components/`): `entity-drawer.css`, `action-button-group.css`, `mud-components.css`, etc.
- Themes (`css/themes/`): `carbon-neon-theme.css`

#### JavaScript
- `js/` — helper JS files (component interop)

#### Internationalization
- `i18n/en.json`, `i18n/it.json`

---

## Architecture Patterns

### Component Location Rules
| Type | Location |
|---|---|
| Routable pages (`@page`) | `/Pages/{domain}/` |
| Dialog components (no `@page`, opened via `DialogService`) | `/Shared/Components/Dialogs/{domain}/` |
| Tab sub-components of a dialog | `/Pages/{domain}/{Entity}DetailTabs/` (co-located with dialog) |
| Reusable display/input components | `/Shared/Components/{domain}/` |
| Feature-specific POS components | `/Shared/Components/Sales/Pos26/` |
| Embedded third-party widgets | `/Shared/Components/Reports/` |
| Generic management infrastructure | `/Shared/Management/` |

### Service Registration (`Program.cs`)
```csharp
builder.Services.AddScoped<IServiceName, ServiceImplementation>();
```

### Authentication & Authorization
- `[Authorize]` / `[Authorize(Roles = "...")]` on pages
- Roles: Admin, Manager, Operator, Viewer
- Custom state: `CustomAuthenticationStateProvider`

### Naming Conventions
- **Pages**: `{Entity}Management.razor`, `{Entity}DetailDialog.razor`
- **Components**: `{Feature}{Type}.razor` (e.g., `DocumentTotalsCard.razor`, `PaymentDialog.razor`)
- **Report widgets**: `{Report}Panel.razor` (not `*Component`)
- **Services**: `I{Name}Service.cs` / `{Name}Service.cs`
- **ViewModels**: `{Entity}DetailViewModel.cs`

---

## Architecture History

### April 2026 — Full Project Reorganization
- **`Components/`** root folder eliminated; all contents moved under `Shared/Components/`:
  - `Components/Pos26/` → `Shared/Components/Sales/Pos26/`
  - `Components/Reports/ReportDesignerComponent.razor` → `Shared/Components/Reports/ReportDesignerPanel.razor`
  - `Components/Reports/ReportViewerComponent.razor` → `Shared/Components/Reports/ReportViewerPanel.razor`
  - `Components/ProductManagement/DevToolsButton.razor` → **deleted** (unused)
- **`Shared/BusinessParty/`** merged into `Shared/Components/Business/`
- **`Shared/Documents/`** merged into `Shared/Components/Documents/`
- **`Pages/Store/`** merged into `Pages/Management/Store/` (consistent store admin path)
- **`Pages/Chat/`** dialog components moved to `Shared/Components/Dialogs/Chat/`; dead redirect `ChatInterface.razor` deleted
- **`Shared/Atoms/`** dissolved: `GroupBadge.razor` → `Shared/Components/Common/`; unused atoms deleted
- **`Shared/Components/DevTools/`** deleted (unused components)
- **6 unused files deleted**: `FidelityCardPlaceholder.razor`, `PriceListBadge.razor`, `DevToolsButton.razor`, `GenerateProductsButton.razor`, `GenerateProductsDialog.razor`, `ChatInterface.razor`

### February 2026 — SuperAdmin Cleanup (PR #4)
All SuperAdmin pages, components, and services removed from client. SuperAdmin functionality lives exclusively in the Server Dashboard (`EventForge.Server`, Razor Pages).

### November 2025 — Fast Inventory Cleanup
Removed dual inventory implementations (`InventoryProcedureFast`, `InventoryProcedureSyncfusion`). Classic `InventoryProcedure.razor` is the sole implementation.

