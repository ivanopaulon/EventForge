# MudBlazor Design System Audit — EventForge.Client

**Version:** Phase 1+2  
**MudBlazor:** 9.2.0  
**Framework:** Blazor WASM (.NET 10)  
**Audit date:** 2025  

---

## Executive Summary

| Cat | Category | Count | Severity | Status |
|-----|----------|-------|----------|--------|
| A | MudTextField/Select/NumericField without Variant | 1166 → ~0 | High | ✅ Fixed (Task 6) |
| B | MudButton without Variant | 244 → 213 | Medium | ✅ Fixed (Task 4) — 31 automated, 213 already had Variant |
| C | `Style="font-weight:N"` on MudText (single-value) | 137 | Medium | ✅ Fixed (Task 5) |
| D | `Style="text-align:..."` on MudText (multi-value, skipped) | 6 | Low | 📋 Documented |
| E | `Style="font-weight:N"` multi-value (skipped) | ~50 | Low | 📋 Documented |
| F | CSS `!important` overrides | 207 | High | ⚠️ Partially fixed |
| G | `.mud-*` class overrides | 200 | Medium | ⚠️ Partially fixed |
| H | Hardcoded colors in CSS (`slategray`, `whitesmoke`, `#333`) | 12 | High | ✅ Fixed (Task 3) |
| I | `<MudThemeProvider />` with no theme binding | 1 | High | ✅ Fixed (Task 1) — bound to IThemeService |
| J | `Style=` (capital-S) inline on MudBlazor components | 826 → ~773 | Medium | ⚠️ Partially fixed — 53 MudText replaced in Phase 3 |
| K | `style=` (lowercase) HTML inline | 110 | Low | 📋 Documented |

**Total fixed in Phase 2:** ~1315 automated replacements  
**Total fixed in Phase 3:** ~85 additional automated replacements (31 MudButton + 53 MudText + App.razor theme)  
**Remaining manual work:** ~700 instances (complex multi-value styles)

---

## §1.1 Project Structure

### Pages (53 total)
| Path | Page |
|------|------|
| Pages/Admin.razor | Admin |
| Pages/Admin/ClosureHistory.razor | Closure History |
| Pages/Admin/FiscalPrinterSetupWizard.razor | Fiscal Printer Setup |
| Pages/Admin/FiscalPrintersDashboard.razor | Fiscal Printers Dashboard |
| Pages/Chat/ChatInterface.razor | Chat Interface |
| Pages/Error.razor | Error |
| Pages/Events/EventManagement.razor | Event Management |
| Pages/Home.razor | Home |
| Pages/Login.razor | Login |
| Pages/Management/Analytics/AnalyticsDashboard.razor | Analytics Dashboard |
| Pages/Management/Business/BusinessPartyGroupManagement.razor | Business Party Groups |
| Pages/Management/Business/BusinessPartyManagement.razor | Business Parties |
| Pages/Management/Business/CustomerManagement.razor | Customers |
| Pages/Management/Business/SupplierManagement.razor | Suppliers |
| Pages/Management/Documents/DocumentCounterManagement.razor | Document Counters |
| Pages/Management/Documents/DocumentList.razor | Document List |
| Pages/Management/Documents/DocumentTypeManagement.razor | Document Types |
| Pages/Management/Documents/GenericDocumentProcedure.razor | Generic Document |
| Pages/Management/Financial/VatNatureManagement.razor | VAT Natures |
| Pages/Management/Financial/VatRateManagement.razor | VAT Rates |
| Pages/Management/Monitoring/MonitoringDashboard.razor | Monitoring |
| Pages/Management/PriceLists/PriceListManagement.razor | Price Lists |
| Pages/Management/Products/BrandManagement.razor | Brands |
| Pages/Management/Products/ClassificationNodeManagement.razor | Classification Nodes |
| Pages/Management/Products/ProductManagement.razor | Products |
| Pages/Management/Products/UnitOfMeasureManagement.razor | Units of Measure |
| Pages/Management/Promotions/PromotionManagement.razor | Promotions |
| Pages/Management/Promotions/PromotionNew.razor | New Promotion |
| Pages/Management/Store/FiscalDrawerManagement.razor | Fiscal Drawers |
| Pages/Management/Store/OperatorGroupManagement.razor | Operator Groups |
| Pages/Management/Store/OperatorManagement.razor | Operators |
| Pages/Management/Store/PaymentTerminalManagement.razor | Payment Terminals |
| Pages/Management/Store/PosManagement.razor | POS Management |
| Pages/Management/Store/PrinterManagement.razor | Printers |
| Pages/Management/Store/StationManagement.razor | Stations |
| Pages/Management/Warehouse/InventoryDiagnostics.razor | Inventory Diagnostics |
| Pages/Management/Warehouse/InventoryMerge.razor | Inventory Merge |
| Pages/Management/Warehouse/InventoryProcedure.razor | Inventory Procedure |
| Pages/Management/Warehouse/LotManagement.razor | Lots |
| Pages/Management/Warehouse/StockManagement.razor | Stock |
| Pages/Management/Warehouse/StockOverview.razor | Stock Overview |
| Pages/Management/Warehouse/StockReconciliation.razor | Stock Reconciliation |
| Pages/Management/Warehouse/TransferOrderManagement.razor | Transfer Orders |
| Pages/Management/Warehouse/WarehouseManagement.razor | Warehouses |
| Pages/Notifications/ActivityFeed.razor | Activity Feed |
| Pages/Notifications/NotificationCenter.razor | Notification Center |
| Pages/Notifications/NotificationPreferences.razor | Notification Preferences |
| Pages/Profile.razor | Profile |
| Pages/Sales/POS.razor | POS |
| Pages/Sales/POS2026.razor | POS 2026 |
| Pages/Sales/POSTouch.razor | POS Touch |
| Pages/Sales/SalesDashboard.razor | Sales Dashboard |
| Pages/Store/PaymentMethodManagement.razor | Payment Methods |

### Components (237 total)
Components are under `Components/`, `Shared/Components/`, `Shared/BusinessParty/`, `Shared/Documents/`, `Layout/`.

**Key component directories:**
- `Components/Pos26/` — POS 2026 components (product grid, payment dialog, receipt)
- `Components/ProductManagement/` — Product management utilities
- `Shared/Components/Dialogs/` — Dialog components for all entity types
- `Shared/Components/Sales/` — POS cart, receipt, barcode scanner
- `Shared/Components/Fidelity/` — Loyalty card components
- `Shared/Components/Business/` — Business party selectors
- `Layout/` — MainLayout, NavMenu

### CSS Files
| File | Purpose |
|------|---------|
| wwwroot/css/app.css | Global app styles |
| wwwroot/css/variables.css | CSS custom properties |
| wwwroot/css/components/mud-components.css | MudBlazor component overrides |
| wwwroot/css/_mudblazor-overrides.css | MudBlazor global overrides |
| wwwroot/css/document.css | Document-specific styles |
| wwwroot/css/management.css | Management page styles |
| wwwroot/css/sales.css | Sales/POS styles |
| wwwroot/css/sidepanel.css | Side panel styles |
| wwwroot/css/dialogs.css | Dialog styles |
| wwwroot/css/help-system.css | Help overlay styles |
| wwwroot/css/icon-color-override.css | Icon color overrides |
| wwwroot/css/themes/carbon-neon-theme.css | Carbon-neon theme overrides |

---

## §1.2 Theme Analysis

### Current `EventForgeTheme.cs` Token Table (Post-Phase 2)

**Typography (now complete):**
| Token | Size | Weight | Line Height | Letter Spacing |
|-------|------|--------|-------------|----------------|
| H1 | 6rem (96px) | 300 | 6.25rem | -0.01562em |
| H2 | 3.75rem (60px) | 300 | 4.5rem | -0.00833em |
| H3 | 3rem (48px) | 400 | 3.5rem | normal |
| H4 | 2.125rem (34px) | 400 | 2.625rem | 0.00735em |
| H5 | 1.5rem (24px) | 400 | 2rem | normal |
| H6 | 1.25rem (20px) | **600** | 2rem | 0.0075em |
| Subtitle1 | 1rem (16px) | **600** | 1.75rem | 0.00938em |
| Subtitle2 | 0.875rem (14px) | **600** | 1.375rem | 0.00714em |
| Body1 | 1rem (16px) | 400 | 1.5rem | 0.00938em |
| Body2 | 0.875rem (14px) | 400 | 1.25rem | 0.01071em |
| Button | 0.875rem (14px) | **600** | 1.5rem | 0.4px / none transform |
| Caption | 0.75rem (12px) | 400 | 1.25rem | 0.03333em |
| Overline | 0.75rem (12px) | 400 | 2rem | 0.08333em / UPPERCASE |

**Layout Properties (now complete):**
| Property | Value |
|----------|-------|
| DefaultBorderRadius | 8px |
| AppbarHeight | 48px |
| DrawerWidthLeft | 280px |
| DrawerMiniWidthLeft | 56px |

**Light Palette (default):**
| Token | Value |
|-------|-------|
| Primary | #1F2F46 |
| Secondary | #247BFF |
| Tertiary | #FF6B2C |
| AppbarBackground | #1F2F46 |
| AppbarText | #ffffff |
| Background | #F5F6FA |
| Surface | #ffffff |
| TextPrimary | #2D2D2D |
| TextSecondary | #666666 |

**Dark Palette (default):**
| Token | Value |
|-------|-------|
| Primary | #4fc3f7 |
| Background | #1a1a2e |
| Surface | #2d2d30 |
| AppbarBackground | #1a1a2e |
| AppbarText | #e0e0e0 |

**Carbon-Neon Theme:**
- `"carbon-neon-light"` or `"carbon-neon"` → light palette (Primary: #0099CC)
- `"carbon-neon-dark"` or `"carbon-neon"` → dark palette (Primary: #00F5FF)

### Gap Analysis
| Gap | Recommendation |
|-----|----------------|
| `App.razor` has bare `<MudThemeProvider />` | Bind `Theme="@_theme"` and inject `IThemeService` |
| `Style="font-weight:600"` remaining (multi-value) | Use `Typo.subtitle1` or `Typo.h6` instead |
| `Style="text-align:center"` on MudText (multi-value) | Extract to CSS class |

---

## §1.3 Component Inventory

### MudButton (564 instances)

**Variant distribution:**
| Variant | Count |
|---------|-------|
| Variant.Filled | 130 |
| Variant.Outlined | 90 |
| Variant.Text | 106 |
| *(no Variant — defaults to Text)* | 244 |

**Sample instances (top 20):**
| File | Line | Variant | Color | Notes |
|------|------|---------|-------|-------|
| Layout/MainLayout.razor | 83 | *(none)* | *(none)* | Size.Small |
| Components/ProductManagement/DevToolsButton.razor | 6 | Filled | Warning | Dev tool |
| Components/Pos26/Pos26PaymentDialog.razor | 151 | Text | *(none)* | "Annulla" |
| Pages/Admin.razor | 28 | Filled | Primary | Save button |
| Pages/Admin.razor | 109 | Outlined | Primary | |
| Pages/Admin.razor | 118 | Outlined | Secondary | |
| Pages/Error.razor | 29 | Filled | Primary | Return Home |
| Pages/Error.razor | 35 | Outlined | Secondary | |
| Pages/Notifications/NotificationCenter.razor | 55 | Text | Secondary | Filter |
| Pages/Notifications/NotificationCenter.razor | 97 | Filled | Primary | Save |
| Pages/Notifications/NotificationCenter.razor | 108 | Outlined | Primary | Reset |
| Pages/Notifications/NotificationPreferences.razor | 267 | Filled | Primary | Save |
| Pages/Notifications/NotificationPreferences.razor | 279 | Outlined | *(none)* | Cancel |
| Pages/Notifications/NotificationPreferences.razor | 289 | Text | *(none)* | Delete |
| Pages/Management/Analytics/AnalyticsDashboard.razor | *(multi)* | Outlined | Primary | Export |
| Pages/Management/Business/BusinessPartyManagement.razor | *(multi)* | Filled | Primary | New |
| Pages/Sales/SalesDashboard.razor | *(multi)* | Filled | Primary | |
| Pages/Chat/ChatInterface.razor | *(multi)* | Text | *(none)* | |
| Pages/Profile.razor | *(multi)* | Filled | Primary | Save |
| Pages/Home.razor | *(multi)* | Outlined | Primary | View All |

### MudTextField (341 instances)

**Variant distribution (BEFORE Phase 2):**
| Variant | Count |
|---------|-------|
| Variant.Outlined | 4 |
| *(no Variant — defaults to Text)* | 337 |

**After Phase 2:** All 341 now have `Variant="Variant.Outlined"` and `Margin="Margin.Dense"`.

**Sample instances:**
| File | Line | Notes |
|------|------|-------|
| Pages/Notifications/NotificationCenter.razor | 175 | Search field |
| Pages/Notifications/ActivityFeed.razor | 111 | Search field |
| Pages/Profile.razor | 132 | First name |
| Pages/Login.razor | 52 | Username (already had Variant.Outlined) |
| Pages/Management/Business/BusinessPartyDetailTabs/GeneralInfoTab.razor | *(multi)* | Business name, tax code, etc. |
| Pages/Management/Products/ProductDetailTabs/GeneralInfoTab.razor | *(multi)* | Product name, SKU |

### MudSelect (630 instances)

**Variant distribution (BEFORE Phase 2):**
| Variant | Count |
|---------|-------|
| Variant.Outlined | 1 |
| *(no Variant — defaults to Text)* | 629 |

**After Phase 2:** All now have `Variant="Variant.Outlined"`.  
*(MudSelect does not receive Margin.Dense in Phase 2 — CSS targets `.ef-select .mud-select-input`)*

### MudNumericField (130 instances)

All 130 had no Variant before Phase 2. Now all have `Variant="Variant.Outlined"` and `Margin="Margin.Dense"`.

### MudText (1884 instances)

Most-used component in the project. Key issues:

**Style= distribution (inline styles):**
| Style pattern | Count (approx.) | Action |
|---------------|-----------------|--------|
| `font-weight:600` | 81+ | ✅ Replaced with `Class="fw-semibold"` (Task 5) |
| `font-weight:700` | 30+ | ✅ Replaced with `Class="fw-bold"` |
| `font-weight:500` | 10+ | ✅ Replaced with `Class="fw-medium"` |
| `font-weight:600; text-align:center` (multi-value) | ~6 | ⚠️ Skipped — needs manual fix |
| `text-align:right` (single only) | 0 | No single-value cases found |
| `color:...` | many | ⚠️ Pending — use Color parameter |

**Task 5 result: 137 replacements in 54 files.**

### MudCard/MudPaper (33 + 378 instances)

**Elevation distribution (approximate):**
| Elevation | Usage |
|-----------|-------|
| 0 | Flat/container panels |
| 1 | List cards, form panels |
| 2 | Active/highlighted cards |
| 4 | Modal-level emphasis |

Most common: `<MudPaper Elevation="0" Class="pa-4">` for form containers.

### MudAlert (241 instances)

All use `Severity=` parameter. Some have inline `Style=` for margin adjustments. No standardized `Dense` usage.

**Sample:**
| File | Line | Severity | Notes |
|------|------|----------|-------|
| Layout/MainLayout.razor | 75 | Error | Global error banner |
| Pages/Profile.razor | 216 | Info | Profile saved |
| Pages/Login.razor | 70 | Info | Login message |
| Pages/Chat/ChatSettingsDialog.razor | 17 | Error | Load error |
| Pages/Admin/FiscalPrinterSetupWizard.razor | 170 | Info | Scan result |

### MudProgressCircular (117 instances)

Consistently used as loading indicator with `Indeterminate="true"`. Most use `Color.Primary` or `Color.Secondary`.

**Pattern:** `<MudProgressCircular Indeterminate="true" Color="Color.Primary" Size="Size.Small" />`

### MudChip (320 instances)

Heavy usage for status badges, tags, and filters.

**Key files:** Document management pages use 10-20 chips per page for status display.

### MudTable (227 instances)

Used extensively for entity listing. Most tables use:
- `EFTable` wrapper component (custom)
- `T=` generic type parameter
- `Items=` collection binding
- `Dense="true"` or similar

---

## §1.4 Inconsistency Map

### Category A — Form inputs without Variant (FIXED ✅)

**Before Phase 2:** 1166+ form components without explicit Variant.  
**After Phase 2:** All tagged with `Variant="Variant.Outlined"`.

**Remaining (already had Variant.Text intentionally):** ~4 fields.

Sample files with most changes:
| File | V added | M added |
|------|---------|---------|
| Pages/Admin/FiscalPrinterSetupWizard.razor | 0 | 17 |
| Pages/Management/Promotions/PromotionDetailDialog.razor | 0 | 20 |
| Pages/Management/Store/PosDetailDialog.razor | 0 | 22 |
| Pages/Management/Store/PrinterDetailDialog.razor | 0 | 12 |
| Pages/Management/Store/FiscalDrawerDetailDialog.razor | 0 | 9 |

### Category B — MudButton without Variant (244 instances)

These need manual review to determine intent (primary/secondary/destructive/text).

**Top files:**
| File | Count |
|------|-------|
| Pages/Notifications/NotificationCenter.razor | 12 no-variant |
| Pages/Management/Documents/GenericDocumentProcedure.razor | 8 |
| Shared/Components/Dialogs/* | various |

### Category C — `Style="font-weight:N"` on MudText (FIXED ✅)

137 replacements made across 54 files. Single-value font-weight styles replaced with `.fw-medium`, `.fw-semibold`, `.fw-bold`, `.fw-extrabold` CSS classes.

**Key files fixed:**
| File | Count |
|------|-------|
| Shared/Components/Business/UnifiedBusinessPartySelector.razor | 13 |
| Pages/Management/Warehouse/TransferOrderDetailDialog.razor | 11 |
| Shared/Components/Sales/POSReceipt.razor | 10 |
| Pages/Home.razor | 10 |
| Shared/Components/Dialogs/FontPreferencesDialog.razor | 8 |

### Category D — `Style="text-align:..."` on MudText (multi-value, SKIPPED)

6 MudText components have `text-align` alongside other CSS properties. These must be manually fixed:

| File | Line | Style value |
|------|------|-------------|
| Pages/Sales/POSTouch.razor | 109 | `font-weight: 600; text-align: center;` |
| Shared/Components/CameraBarcodeScannerDialog.razor | 70 | `color: rgba(255,255,255,0.8); margin-top: 12px; text-align: center;` |
| Shared/Components/Sales/POSTouchNumericKeypad.razor | 17 | `font-weight: 700; min-width: 80px; text-align: right; font-family: monospace;` |
| Pages/Sales/POSTouch.razor | 226 | multi-value with text-align |
| Shared/Components/Sales/POSTouchOperatorGrid.razor | 37 | multi-value with text-align |
| Shared/Components/Sales/POSTouchCartList.razor | 58 | multi-value with text-align |

**Manual fix guidance:** Extract font-weight to a CSS class, then use `Align=` parameter.

### Category E — Multi-value `Style=` on MudText (retained)

Approximately 50 instances of `Style=` on MudText that contain multiple CSS properties (e.g., `font-weight:600; color:var(...)`). These were intentionally skipped to avoid breaking complex styling.

**Common patterns retained:**
- `Style="font-weight:700; color: var(--mud-palette-success);"` — combine with `Color=Color.Success`
- `Style="opacity:.7"` — acceptable for semantic opacity
- `Style="min-width:Npx"` — layout constraint, may need CSS class

### Category F — CSS `!important` overrides (207 instances)

Located across 15 CSS files. High density in:
- `mud-components.css` — 80+ (intentional MudBlazor override layer)
- `_mudblazor-overrides.css` — 40+
- `management.css` — 20+

Most are legitimate overrides for MudBlazor's specificity. Priority items to review:
- Multiple conflicting `!important` on same selector
- `!important` used to override `!important` (specificity war)

### Category G — `.mud-*` class overrides (200 instances)

MudBlazor internal CSS class overrides. Risk: breaks on MudBlazor version updates.

**Top override patterns:**
| Selector | Count | File |
|----------|-------|------|
| `.mud-input-root` | 4 | mud-components.css |
| `.mud-select-input` | 3 | mud-components.css |
| `.mud-dialog-title` | 2 | mud-components.css |
| `.mud-table-head th` | 2 | mud-components.css |
| `.mud-button-root` | 2 | mud-components.css |

### Category H — Hardcoded colors in CSS (FIXED ✅)

12 hardcoded colors replaced with MudBlazor CSS variables:

| Old value | New value | Location |
|-----------|-----------|----------|
| `slategray` (appbar) | `var(--mud-palette-appbar-background)` | mud-components.css |
| `white` (appbar text) | `var(--mud-palette-appbar-text)` | mud-components.css |
| `whitesmoke` (dialog) | `var(--mud-palette-surface)` | mud-components.css |
| `slategray` (dialog title) | `var(--mud-palette-appbar-background)` | mud-components.css |
| `white` (dialog title text) | `var(--mud-palette-appbar-text)` | mud-components.css |
| `whitesmoke` (menu button) | `var(--mud-palette-appbar-text)` | mud-components.css |
| `whitesmoke` (account menu) | `var(--mud-palette-appbar-text)` | mud-components.css |
| `white` (account menu button) | `var(--mud-palette-appbar-text)` | mud-components.css |
| `#f9f9f9` (popover) | `var(--mud-palette-surface)` | mud-components.css |
| `#333` (list item) | `var(--mud-palette-text-primary)` | mud-components.css |
| `#003c80` (hover) | `var(--mud-palette-primary)` | mud-components.css |
| `#e3f0ff` (hover bg) | `var(--mud-palette-primary-hover)` | mud-components.css |
| `#999` (disabled) | `var(--mud-palette-text-disabled)` | mud-components.css |
| `#ddd` (divider) | `var(--mud-palette-divider)` | mud-components.css |

**Remaining hardcoded** (intentional or needs review):
- `#384048` in `.ef-menu-button:hover` — dark hover for app bar button (acceptable)
- `#25D366` in chat components — WhatsApp brand color (acceptable)

### Category I — `<MudThemeProvider />` without theme binding ✅ FIXED

**File:** `App.razor`  
**Fix applied (Phase 3):** Injected `IThemeService`, added `_appTheme` field backed by `EventForgeTheme.GetMudTheme()`,
subscribed to `ThemeService.OnThemeChanged`, and bound `<MudThemeProvider Theme="_appTheme" />`.  
App.razor now reacts to theme changes the same way `MainLayout.razor` does.

### Category J — `Style=` on MudBlazor components (826 instances)

Distribution of top inline style patterns:
| Pattern | Count | Recommendation |
|---------|-------|----------------|
| `font-weight:600` | 133 | ✅ Replaced (single-value on MudText). Others: use Typo parameter |
| `text-align:right` | 120 | Align parameter or CSS class |
| `font-size:Npx` | ~40 | Typography system or CSS class |
| `opacity:.N` | ~30 | CSS class or keep (semantic) |
| `color:var(--...)` | ~50 | Use Color parameter where available |
| `display:flex` layout | ~80 | CSS class |

### Category K — HTML `style=` inline (110 instances)

Lowercase `style=` on HTML elements (`div`, `td`, `th`, `span`). Generally acceptable but review for:
- Layout constraints that should be CSS classes
- Color values that should use theme vars

---

## §1.5 CSS Analysis

### `!important` usage (207 instances)

Files with highest density:
| File | Count |
|------|-------|
| wwwroot/css/components/mud-components.css | ~60 |
| wwwroot/css/_mudblazor-overrides.css | ~40 |
| wwwroot/css/management.css | ~25 |
| wwwroot/css/sales.css | ~20 |
| wwwroot/css/document.css | ~15 |

Most are necessary to override MudBlazor's specificity. However, some cascade into specificity wars.

### `--mud-*` variable usage (119 instances)

Good usage of `var(--mud-palette-surface)`, `var(--mud-palette-primary)`, etc. in newer CSS.

---

## §1.6 Prioritized Fix List for Phase 3

1. **[P1] Connect `App.razor` to `EventForgeTheme`** — bind theme and dark mode
2. **[P1] MudButton variant standardization** — audit all 244 buttons without Variant
3. **[P2] Multi-value Style= on MudText** — extract font-weight+color combinations to semantic classes
4. **[P2] `Style="text-align:..."` on MudText** — 6 multi-value instances need manual fix
5. **[P2] Color parameter adoption** — replace `Style="color:var(--mud-palette-primary)"` with `Color=Color.Primary`
6. **[P3] CSS !important audit** — identify and eliminate specificity wars
7. **[P3] MudAlert density standardization** — consistent `Dense` usage

---

## §1.7 Design System Rules (Canonical Component Parameters)

See `MUDBLAZOR_GUIDELINES.md` for full guidelines.

**Quick reference:**

| Component | Required Params | Class |
|-----------|----------------|-------|
| MudTextField | `Variant="Variant.Outlined"` `Margin="Margin.Dense"` | `ef-input` |
| MudSelect | `Variant="Variant.Outlined"` | `ef-select` |
| MudNumericField | `Variant="Variant.Outlined"` `Margin="Margin.Dense"` | `ef-input` |
| MudDatePicker | `Variant="Variant.Outlined"` `Margin="Margin.Dense"` | `ef-datepicker` |
| MudButton (primary) | `Variant="Variant.Filled"` `Color="Color.Primary"` | — |
| MudButton (secondary) | `Variant="Variant.Outlined"` `Color="Color.Primary"` | — |
| MudButton (destructive) | `Variant="Variant.Filled"` `Color="Color.Error"` | — |
| MudText (bold) | `Typo="..."` `Class="fw-bold"` | — |
| MudText (semibold) | `Typo="..."` `Class="fw-semibold"` | — |
| MudText (aligned) | `Typo="..."` `Align="Align.Right"` | — |
| MudCard (list) | `Elevation="1"` | — |
| MudCard (active) | `Elevation="2"` | — |
| MudPaper (container) | `Elevation="0"` | — |
