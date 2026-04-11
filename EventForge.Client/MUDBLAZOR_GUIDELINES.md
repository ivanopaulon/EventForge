# MudBlazor Component Guidelines — EventForge.Client

These guidelines define the **standard** way to use MudBlazor v6+ components in
this codebase.  All new code and any refactored code must follow these rules.

---

## 1. Button Variants

| Variant | Use case |
|---------|----------|
| `Variant.Filled` | **Primary actions** — the single most important action on a page or in a dialog (e.g. "Save", "Confirm", "Submit"). Only one per visible action group. |
| `Variant.Outlined` | **Secondary actions** — supporting actions alongside a primary button (e.g. "Cancel", "Back", "Export"). |
| `Variant.Text` | **Tertiary / low-emphasis actions** — links, destructive-confirm buttons, menu items in a popover. |

```razor
<!-- ✅ Correct -->
<MudButton Variant="Variant.Filled" Color="Color.Primary">Save</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Default">Cancel</MudButton>
<MudButton Variant="Variant.Text" Color="Color.Error">Delete</MudButton>

<!-- ❌ Wrong — multiple Filled buttons in one action group -->
<MudButton Variant="Variant.Filled">Save</MudButton>
<MudButton Variant="Variant.Filled">Cancel</MudButton>
```

**Icon buttons** (`MudIconButton`) use no variant — they are always compact.

---

## 2. Form Inputs

All form inputs must use:

```razor
Variant="Variant.Outlined"
Margin="Margin.Dense"
```

This applies to: `MudTextField`, `MudSelect`, `MudDatePicker`, `MudTimePicker`,
`MudAutocomplete`, `MudNumericField`, `MudCheckBox` (label only).

```razor
<!-- ✅ Correct -->
<MudTextField @bind-Value="model.Name"
              Label="Name"
              Variant="Variant.Outlined"
              Margin="Margin.Dense" />

<!-- ❌ Wrong — missing Variant/Margin -->
<MudTextField @bind-Value="model.Name" Label="Name" />
```

For dense data-entry forms use the CSS helper class `ef-input` / `ef-select` to
further compact the input height to 38px.

---

## 3. Card / Paper Elevation Scale

| Elevation | Use case |
|-----------|----------|
| `0` | Flat surface — sidebar panels, table row backgrounds, inner sections that sit on an already-elevated card. |
| `1` | Default card — standard content blocks. |
| `2` | Elevated section — secondary cards inside a page, action panels. |
| `4` | Hero / featured card — primary summary panel, main dashboard widget. |

```razor
<!-- ✅ Correct -->
<MudPaper Elevation="1">...</MudPaper>
<MudCard Elevation="4">...</MudCard>  <!-- hero card -->

<!-- ❌ Wrong — arbitrary elevation value -->
<MudPaper Elevation="10">...</MudPaper>
```

---

## 4. Typography — MudText vs Raw HTML

| Scenario | Use |
|----------|-----|
| Text that needs theme-aware colour or typography variant | `<MudText>` |
| Static layout headings that will never change with the theme | `<h1>`…`<h6>` with CSS class |
| Body copy inside a `MudCard` or `MudPaper` | `<MudText Typo="Typo.body1">` |
| Icon + label combinations | `<MudText>` inside a `MudStack` |

```razor
<!-- ✅ Correct -->
<MudText Typo="Typo.h5" Color="Color.Primary">Section title</MudText>
<MudText Typo="Typo.body2" Color="Color.Secondary">Supporting text</MudText>

<!-- ❌ Wrong — raw HTML bypasses theme colours -->
<h5 style="color: #0099CC">Section title</h5>
```

---

## 5. Theme System

### How to use ThemeService

```csharp
@inject IThemeService ThemeService

// Toggle between light and dark
await ThemeService.ToggleThemeAsync();

// Set explicit theme by key
await ThemeService.SetThemeAsync("carbon-neon-dark");

// Check current state
bool isDark = ThemeService.IsDarkMode;
string key  = ThemeService.CurrentTheme; // "carbon-neon-light" | "carbon-neon-dark"
```

### How to use EventForgeTheme

`EventForgeTheme` is a **static class** — use it only from layout components that
host a `<MudThemeProvider>`.  Do **not** call it from individual pages or widgets.

```csharp
// Get the full MudTheme for a theme key
MudTheme theme = EventForgeTheme.GetMudTheme(ThemeService.CurrentTheme);

// Get only a palette (rare)
PaletteLight light = EventForgeTheme.GetLightPalette("carbon-neon-light");
PaletteDark  dark  = EventForgeTheme.GetDarkPalette("carbon-neon-dark");
```

### Adding a new theme

1. Add a new `ThemeInfo` static member in `ThemeService.cs` and include it in `AvailableThemes`.
2. Add the new key to the switch expression in `EventForgeTheme.GetLightPalette` and/or `GetDarkPalette`.
3. Update `ThemeService.InitializeAsync` backward-compat mapping if needed.

---

## 6. No Inline Styles on MudBlazor Components

**Never** use `style="…"` (lowercase) or `Style="…"` (MudBlazor parameter) on
MudBlazor components.  Use `Class=` with CSS utility classes instead.

```razor
<!-- ❌ Wrong -->
<MudButton Style="margin-top: 16px; width: 100%">Click</MudButton>
<MudText style="color: red">Error</MudText>

<!-- ✅ Correct -->
<MudButton Class="mt-4 full-width">Click</MudButton>
<MudText Color="Color.Error">Error</MudText>
```

### Spacing utility classes

Use MudBlazor's built-in spacing helpers (`ma-`, `pa-`, `mt-`, `mb-`, `ml-`, `mr-`,
`pt-`, `pb-`, `pl-`, `pr-`) wherever possible before reaching for custom CSS.

```razor
<MudStack Spacing="2" Class="mt-2 mb-4">...</MudStack>
```

If you need a truly custom layout rule, add it to the **component's own `.razor.css`
scoped stylesheet** — never as an inline attribute.

---

## 7. CSS File Ownership

| File | Purpose |
|------|---------|
| `wwwroot/css/_mudblazor-overrides.css` | Structural MudBlazor overrides (AppBar height, drawer clip). No component styles here. |
| `wwwroot/css/components/mud-components.css` | EventForge custom CSS classes (`ef-input`, `ef-appbar`, `ef-dialog`, etc.). |
| `wwwroot/css/app.css` | Global application styles, font configuration, accessibility helpers. |
| `wwwroot/css/variables.css` | CSS custom properties mapped to `--mud-palette-*` tokens. |
| `*.razor.css` | Component-scoped styles for individual Blazor components. |

Do **not** add `!important` to `app.css` or `variables.css`.  
Use `!important` in `mud-components.css` only when MudBlazor specificity cannot
be overcome by nesting or attribute selectors.
