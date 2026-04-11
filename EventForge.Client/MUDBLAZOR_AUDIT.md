# MudBlazor Audit Report — EventForge.Client

**Audit date:** Phase 1+2  
**MudBlazor version:** v6+  
**Project:** `EventForge.Client`

---

## 1. Theme Configuration Issues

### 1.1 Duplicate inline palette definitions (FIXED)
| File | Status |
|------|--------|
| `Layout/MainLayout.razor` | Had `GetLightPalette()` / `GetDarkPalette()` / `GetDefaultLightPalette()` / `GetDefaultDarkPalette()` inline — **removed, now delegates to `EventForgeTheme`** |
| `Layout/LoginLayout.razor` | Had stale palette methods referencing obsolete theme keys (`"dark"`, `"warm"`, `"cool"`, `"high-contrast"`, `"fun"`) that no longer exist in `ThemeService` — **removed, now delegates to `EventForgeTheme`** |

### 1.2 Centralised theme (NEW)
`Services/EventForgeTheme.cs` is the single source of truth for all MudBlazor palettes:

| Theme key | Mode | Notes |
|-----------|------|-------|
| `carbon-neon-light` | Light | Default; cyan primary on white surface |
| `carbon-neon-dark`  | Dark  | Neon cyan/magenta on near-black surface |
| *(fallback)*        | Both  | Navy-blue default palette |

`ThemeService` supports only `carbon-neon-light` and `carbon-neon-dark`.  Any other stored key falls back to the default palette via `EventForgeTheme`.

### 1.3 App.razor bare `<MudThemeProvider />`
`App.razor` contains a bare `<MudThemeProvider />` with no theme passed.  This is **intentional** — `App.razor` is the auth/routing shell; every real page is rendered inside `MainLayout` or `LoginLayout`, which both apply the correct theme.  No action required.

---

## 2. CSS Override Issues

### 2.1 Before audit
| File | `!important` count |
|------|--------------------|
| `wwwroot/css/components/mud-components.css` | **81** |
| `wwwroot/css/dialogs.css` | 3 (backdrop blur) |
| `wwwroot/css/sidepanel.css` | 1 |
| `wwwroot/css/management.css` | 0 |
| `wwwroot/css/icon-color-override.css` | 0 (all commented out) |

### 2.2 After Phase 2
| File | `!important` count | Change |
|------|--------------------|--------|
| `wwwroot/css/_mudblazor-overrides.css` *(new)* | 1 (drawer clip) | Extracted from `mud-components.css` |
| `wwwroot/css/components/mud-components.css` | **80** | −1 (drawer clip removed) |

### 2.3 Remaining `!important` in mud-components.css — categorised

| Category | Count | Rationale |
|----------|-------|-----------|
| Custom input sizing (`.ef-input`, `.ef-datepicker`, `.ef-select`) | ~12 | MudBlazor sets explicit heights/padding inline; `!important` required to override |
| Toolbar / action buttons (`.ef-toolbar-button`, `.ef-row-actionbutton`) | ~8 | Compact size constraints override MudBlazor button defaults |
| AppBar (`.ef-appbar`) | ~5 | Height + padding override (theme cannot set appbar height to 48px natively) |
| Dialog (`.ef-dialog`) | ~7 | Title-bar styling; `whitesmoke` background not expressible via palette |
| Menu / popover (`.ef-menu-button`, `.ef-account-menu`, `.ef-account-menu-popover`) | ~24 | Popover lives outside component DOM tree; scoping via attribute required |
| Table sticky header | 0 | Clean, uses CSS variables |
| Miscellaneous | ~24 | Cursor, box-model, colour fixes |

> **Recommendation:** In a future Phase 3, replace hard-coded `slategray`/`whitesmoke` colours in `.ef-appbar` and `.ef-dialog` with `var(--mud-palette-appbar-background)` and `var(--mud-palette-surface)` to make these classes theme-aware.

---

## 3. Inline Style Inventory

A quick scan found **~112 `style="…"` attributes** across **~57 Razor files**.

Common patterns observed:
- `style="margin-top: Npx"` / `style="padding: …"` — spacing adjustments
- `style="width: …; height: …"` — size overrides on MudBlazor components
- `style="color: …"` — manual colour not using theme tokens
- `Style="…"` (capital-S, MudBlazor parameter) — **~30+ instances** on MudBlazor components

These should be migrated to CSS utility classes or component parameters.  See `MUDBLAZOR_GUIDELINES.md` for the standard approach.

---

## 4. Component Inconsistencies

### 4.1 Button variants
Scattered usage of `Variant.Filled`, `Variant.Outlined`, and `Variant.Text` with no consistent pattern.  See `MUDBLAZOR_GUIDELINES.md` §1 for the agreed standard.

### 4.2 Form inputs
`Variant` and `Margin` not consistently applied across all form pages.  Standard is `Variant.Outlined` + `Margin.Dense` everywhere.

### 4.3 Card/Paper elevation
`Elevation` values vary inconsistently.  See `MUDBLAZOR_GUIDELINES.md` §3 for the elevation scale.

---

## 5. Files Modified in This Phase

| File | Change |
|------|--------|
| `Services/EventForgeTheme.cs` | **Created** — central palette + typography + layout-properties |
| `Layout/MainLayout.razor` | Removed inline palette methods; delegates to `EventForgeTheme` |
| `Layout/LoginLayout.razor` | Removed stale palette methods; delegates to `EventForgeTheme` |
| `wwwroot/css/_mudblazor-overrides.css` | **Created** — AppBar height var + drawer clip override |
| `wwwroot/css/components/mud-components.css` | Removed drawer/appbar-height block (now in `_mudblazor-overrides.css`) |
| `wwwroot/index.html` | Added `<link>` for `_mudblazor-overrides.css` before `mud-components.css` |

---

## 6. Recommended Follow-up (Phase 3+)

1. Replace `slategray`/`whitesmoke` hardcodes in `.ef-appbar` / `.ef-dialog` with MudBlazor CSS variables.
2. Audit all `Style="…"` (capital-S) usages on MudBlazor components — move to `Class=` with utility CSS.
3. Systematically apply button-variant, form-input, and elevation standards across all pages.
4. Consider adding a second AppBar height option (64px) for users on large screens.
