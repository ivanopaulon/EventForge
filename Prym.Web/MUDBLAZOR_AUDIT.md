# MudBlazor Design System Audit — EventForge.Client

**Version:** Phase 1+2+3d+3e+3f+3g+3h+4+5+6+7+8+9  
**MudBlazor:** 9.2.0  
**Framework:** Blazor WASM (.NET 10)  
**Audit date:** 2025 — updated 2026-04-12  

---

## Executive Summary

| Cat | Category | Count | Severity | Status |
|-----|----------|-------|----------|--------|
| A | MudTextField/Select/NumericField without Variant | 1166 → ~0 | High | ✅ Fixed (Task 6) |
| B | MudButton without Variant | 244 → **18** | Medium | ✅ Substantially fixed — 18 remaining are intentional (e.g. icon-only buttons) |
| C | `Style="font-weight:N"` on MudText (single-value) | 137 → 0 | Medium | ✅ Fixed (Task 5) |
| D | `Style="text-align:..."` on MudText (multi-value, skipped) | 6 | Low | 📋 Documented |
| E | `Style="font-weight:N"` multi-value (skipped) | ~50 | Low | 📋 Documented |
| F | CSS `!important` overrides | 167 → **150** | High | ⚠️ Phase 5: 17 palette-var `!important` removed from mud-components.css; sizing rules kept |
| G | `.mud-*` class overrides | 122 → **122** | Medium | ⚠️ Unchanged — structural overrides that require CSS-level targeting |
| H | Hardcoded colors in CSS (`slategray`, `whitesmoke`, `#333`) | 12 → **0 (active)** | High | ✅ Phase 6: status-dot colors → `--ef-status-*`; scanner `#4CAF50`/`#FFC107` → palette vars; `#333`/`#cccccc` → semantic vars |
| I | `<MudThemeProvider />` with no theme binding | 1 → 0 | High | ✅ Fixed (Task 1) — bound to IThemeService |
| J | `Style=` (capital-S) inline on MudBlazor components | 826 → **129** | Medium | ⚠️ Substantially fixed — 697+ replacements total; Phase 9: 6 more converted (ef-touch-card ×3, nfd-action-link, ef-drop-zone-dashed, chat-img-thumb); ~77 dynamic (@expr) + ~21 intentional unique + ~31 multi-value intentional remain |
| K | `style=` (lowercase) HTML inline | 110 → **47** | Low | ✅ Substantially fixed; Phase 9: 19 more converted (color-dot-10 ×2, color-swatch-20 ×2, color-swatch-30r4, color-swatch-50r8, chat-avatar-wrap, chat-avatar-mini, chat-pre-wrap, mh-200/240/280-scroll ×5, ncd-* ×4, nfd-chips/tags/grid ×3); remaining 25 static are brand colors, unique dims, or POS-specific |

> **Note (Phase 5):** Previous Cat F/G counts (229/282) were measured by `grep` which matched inside `/* ... */` block comments.  
> The corrected baseline with comment-stripping is **167 active !important** and **122 active .mud-\* rules** across all CSS files.

**Total fixed in Phase 2:** ~1315 automated replacements  
**Total fixed in Phase 3a/3b:** ~85 additional (31 MudButton + 53 MudText + App.razor theme)  
**Total fixed in Phase 3c:** ~168 additional (165 MudTh/MudTd + 3 MudText static styles)  
**Total fixed in Phase 3d:** 135 additional (MudPaper 63 + MudStack 19 + MudDivider 10 + MudIconButton 9 + MudButton 12 + MudNumericField 12 + MudSelect 10)  
**Total fixed in Phase 3e:** ~47 additional (MudIcon, MudAvatar, MudChip, MudAlert, MudText remaining)  
**Total fixed in Phase 3f:** ~74 additional (MudPaper, MudCard, MudProgressLinear, MudProgressCircular, MudContainer, MudStepper, MudTh/Td, MudLink, MudIcon, MudIconButton)  
**Total fixed in Phase 3g:** 26 HTML inline style → CSS class conversions (overflow-y:auto, width:100%, cursor:pointer, font-weight:600, text-align:right/center on td/th); `.overflow-y-auto` utility class added  
**Total fixed in Phase 3h:** Added `--ef-hover-dark` CSS variable (replaces 2 hardcoded `#384048`); removed `!important` from 3 `cursor:pointer` hover rules + 1 redundant box-shadow  
**Total fixed in Phase 4:** `EventForgeTheme.GetMudTheme()` now includes `Shadows` (26 lighter elevation levels aligned to `--shadow-sm/md/lg`) and explicit `ZIndex` configuration; removed `.mud-paper.ef-tile { box-shadow !important }` override block  
**Total fixed in Phase 5:** 17 palette-var `!important` removed from mud-components.css; corrected baseline counts (comment-stripping) F: 167 active, G: 122 active  
**Total fixed in Phase 6:** 8 hardcoded colors removed from app.css/language-selector.css → `--ef-status-online/away/busy` vars added; `#4CAF50`→`var(--mud-palette-success)` ×2; `#FFC107`→`var(--mud-palette-warning)`; `#333`→`var(--neutral-dark-light)`; `#cccccc`→`var(--mud-palette-lines-default)`  
**Total fixed in Phase 7:** 25+ multi-value static `Style=`/`style=` converted to utility CSS classes in 15 razor files; 9 new utility classes added to app.css (`.ef-flex-col-fill`, `.ef-flex-child-fill`, `.pos-rel-no-shrink`, `.mh-300-scroll`, `.mh-350-scroll`, `.log-pre-mono`, `.log-pre-mt`, `.opacity-60-ml-4`, `.bg-grey-r8`, `.dlg-body-col`, `.color-swatch-btn`, `.presence-dot`); `.gap-3` utility added; `#4caf50` presence dot → `var(--ef-status-online)` in NewChatDialog  
**Total fixed in Phase 8:** ~24 static single-value `Style=`/`style=` converted in 14 razor files; 9 new utility classes added to app.css (`.text-error-color`, `.border-error-l-3`, `.border-info-l-3`, `.border-warning-2`, `.overflow-x-auto`, `.pos-relative`, `.bg-transparent`, `.bg-primary-lighten`, `.bg-error-lighten`); remaining 27 singles are intentionally kept (brand colors, unique dimensions, rgba/gradient values)  
**Total fixed in Phase 9:** ~25 static multi/single-value `style=`/`Style=` converted in 14 razor files; 21 new utility classes added to app.css (`.color-dot-10`, `.color-swatch-20/30r4/50r8`, `.mh-200/240/280-scroll`, `.chat-pre-wrap`, `.chat-avatar-wrap`, `.chat-avatar-mini`, `.chat-img-thumb`, `.ef-touch-card`, `.ncd-toolbar/icon-row/loading/empty`, `.nfd-chips-row/tags-row/grid-2col/action-link`, `.ef-drop-zone-dashed`); Cat J: 129 (77 dynamic + 21 intentional singles + 31 intentional multi-value); Cat K: 47 (22 dynamic + 25 static intentional)  
**Remaining static Style=:** ~151 (108 dynamic `@expr`, 43 complex multi-value — intentionally kept)

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

### Category F — CSS `!important` overrides (224 instances, updated 2026-04-12)

Located across 12 CSS files. High density in:

| File | Count |
|------|-------|
| `components/mud-components.css` | 81 |
| `app.css` | 57 |
| `dialogs.css` | 23 |
| `components/action-button-group.css` | 23 |
| `icon-color-override.css` | 17 |
| `document.css` | 8 |
| `components/entity-drawer.css` | 5 |
| `sidepanel.css` | 3 |
| `_mudblazor-overrides.css` | 3 |
| `sales.css` | 2 |
| `components/overlays.css` | 1 |

Most are legitimate overrides for MudBlazor's specificity. **Phase 3h** removed 4 unnecessary `!important`
(3x `cursor:pointer` hover rules where `:hover` specificity is sufficient; 1x redundant `box-shadow`
on `.mud-paper.ef-tile` now covered by `EventForgeTheme.GetShadows()` Elevation[1]).  
**Reaching ~50 requires full MudTheme migration** (moving palette-driven overrides in `mud-components.css` into `EventForgeTheme`).

### Category G — `.mud-*` class overrides (280 instances, updated 2026-04-12)

MudBlazor internal CSS class overrides. Risk: breaks on MudBlazor version updates.

| File | Count |
|------|-------|
| `components/action-button-group.css` | 82 |
| `icon-color-override.css` | 61 |
| `dialogs.css` | 42 |
| `components/mud-components.css` | 32 |
| `app.css` | 19 |
| `sales.css` | 15 |
| `components/entity-drawer.css` | 13 |
| `document.css` | 10 |
| `_mudblazor-overrides.css` | 2 |
| `themes/carbon-neon-theme.css` | 2 |

**Reaching ~30 requires full MudTheme migration** — moving component-specific palette, border-radius, and
elevation overrides from CSS into `EventForgeTheme.GetLightPalette()`/`GetDarkPalette()`.

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

**Phase 3h additional:** `#384048` (dark hover on appbar buttons) replaced with CSS variable `--ef-hover-dark` (defined in `variables.css`).

**Remaining hardcoded** (intentional):
- `#25D366` in chat components — WhatsApp brand color (acceptable)

### Category I — `<MudThemeProvider />` without theme binding ✅ FIXED

**File:** `App.razor`  
**Fix applied (Phase 3):** Injected `IThemeService`, added `_appTheme` field backed by `EventForgeTheme.GetMudTheme()`,
subscribed to `ThemeService.OnThemeChanged`, and bound `<MudThemeProvider Theme="_appTheme" />`.  
App.razor now reacts to theme changes the same way `MainLayout.razor` does.

### Category J — `Style=` on MudBlazor components (**151 remaining**, updated 2026-04-12)

Distribution of top inline style patterns:
| Pattern | Count | Recommendation |
|---------|-------|----------------|
| `font-weight:600` | 133 | ✅ Replaced (single-value on MudText). Others: use Typo parameter |
| `text-align:right` | 120 | Align parameter or CSS class |
| `font-size:Npx` | ~40 | Typography system or CSS class |
| `opacity:.N` | ~30 | CSS class or keep (semantic) |
| `color:var(--...)` | ~50 | Use Color parameter where available |
| `display:flex` layout | ~80 | CSS class |

**Remaining 151:** 108 are dynamic expressions (`@expr`, `@bgColor`, `@GetCardStyle()` etc.) — cannot be automated; 43 are complex static multi-value styles kept intentionally.

### Category K — HTML `style=` inline (**64 remaining**, updated 2026-04-12)

**Phase 3g** converted 26 simple single-property inline styles on HTML elements to existing CSS utility classes:
- `style="overflow-y:auto"` → `class="overflow-y-auto"` (new utility added to `app.css`)
- `style="width:100%"` on div/MudPaper → `w-full`
- `style="cursor:pointer"` on div → `cursor-pointer`
- `style="font-weight:600"` on td → `fw-semibold`
- `style="text-align:right/center"` on td/th → `text-right` / `text-center`

**Remaining 64:**
- 52 complex multi-property styles (e.g. `display:flex; flex-direction:column; height:100%; overflow:hidden`) — require creating single-use CSS classes with no reuse value; intentionally kept
- 12 single-property unique constraints (`height:200px`, `max-height:55vh`, `border-top:...`, `position:relative` etc.) — context-specific, no matching utility class exists

## §1.5 CSS Analysis

### `!important` usage (**224 instances**, updated 2026-04-12)

Files with current counts:
| File | Count |
|------|-------|
| `wwwroot/css/components/mud-components.css` | 81 |
| `wwwroot/css/app.css` | 57 |
| `wwwroot/css/dialogs.css` | 23 |
| `wwwroot/css/components/action-button-group.css` | 23 |
| `wwwroot/css/icon-color-override.css` | 17 |
| `wwwroot/css/document.css` | 8 |
| `wwwroot/css/components/entity-drawer.css` | 5 |
| `wwwroot/css/sidepanel.css` | 3 |
| `wwwroot/css/_mudblazor-overrides.css` | 3 |
| `wwwroot/css/sales.css` | 2 |
| `wwwroot/css/components/overlays.css` | 1 |

Most are necessary to override MudBlazor's specificity. However, some cascade into specificity wars.
**Phase 3h** eliminated 4 unnecessary `!important` declarations. Reaching ~50 requires full MudTheme migration.

### `--mud-*` variable usage

Good usage of `var(--mud-palette-surface)`, `var(--mud-palette-primary)`, etc. in newer CSS.
`--ef-hover-dark` (added Phase 3h) centralises the app-bar hover colour `#384048` in `variables.css`.

### MudTheme configuration (Phase 4)

`EventForgeTheme.GetMudTheme()` now includes:
- **`Shadows`** — 26 custom elevation levels lighter than Material Design defaults, aligned to `--shadow-sm/md/lg` CSS variables, so `MudPaper Elevation="1"` produces the same shadow as manually styled elements
- **`ZIndex`** — explicit Drawer/AppBar/Dialog/Snackbar/Tooltip z-index values
- **`LayoutProperties`** — AppbarHeight 48px, DefaultBorderRadius 8px, DrawerWidths

The `.mud-paper.ef-tile { box-shadow !important }` override block was removed as it is now redundant.

---

## §1.6 Prioritized Fix List — Remaining Work

1. **[P1] Cat J dynamic Style= (108 instances)** — audit each `@expr` to determine if it can be replaced by a CSS class or MudBlazor parameter
2. **[P2] Full MudTheme migration** — move `mud-components.css` palette/elevation overrides into `EventForgeTheme`; this is the only path to Cat F ~50 and Cat G ~30
3. **[P3] Cat B remaining (18 MudButton)** — verify each is intentional (icon-only, fab, etc.) or add `Variant`
4. **[P3] Cat K complex multi-property (52)** — evaluate if repeated patterns warrant new CSS utility classes
5. **[P3] CSS !important consolidation** — after MudTheme migration, audit remaining `!important` in `app.css` (57) for specificity-raising alternatives

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
