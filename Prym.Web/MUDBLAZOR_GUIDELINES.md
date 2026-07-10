# MudBlazor Design System Guidelines — EventForge.Client

**MudBlazor:** 9.2.0 | **Framework:** Blazor WASM (.NET 10)

---

## §1 Core Principle

> **All styling goes through the theme system or utility classes. Inline `Style=` is prohibited except for dynamic/computed values.**

The precedence order is:
1. **MudBlazor parameters** (`Variant=`, `Color=`, `Typo=`, `Align=`, `Size=`, etc.)
2. **Theme tokens** via `EventForgeTheme.cs`
3. **CSS utility classes** (`.fw-bold`, `.ef-input`, `.ef-select`, etc.)
4. **CSS variables** (`var(--mud-palette-primary)`) for values not in MudBlazor params
5. **Inline `Style=`** — only for truly dynamic/computed values (e.g., `Style="@($"width:{value}px;")"`)

---

## §2 Canonical Component Rules

### 2.1 MudButton

| Use case | Variant | Color | Notes |
|----------|---------|-------|-------|
| Primary action (Save, Submit) | `Variant.Filled` | `Color.Primary` | Main CTA |
| Secondary action (Cancel, Back) | `Variant.Outlined` | `Color.Primary` | |
| Destructive (Delete, Remove) | `Variant.Filled` | `Color.Error` | Always confirm first |
| Tertiary / text-only | `Variant.Text` | `Color.Primary` | Low-emphasis |
| Toolbar icon action | `MudIconButton` | `Color.Inherit` | `Size.Small` |

**✅ Correct:**
```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SaveAsync">
    Salva
</MudButton>

<MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="Cancel">
    Annulla
</MudButton>

<MudButton Variant="Variant.Filled" Color="Color.Error" OnClick="DeleteAsync">
    Elimina
</MudButton>
```

**❌ Wrong:**
```razor
<MudButton>Salva</MudButton>                              <!-- No Variant, no Color -->
<MudButton Style="font-weight:bold">Salva</MudButton>    <!-- Inline style -->
<MudButton Variant="Variant.Filled">Delete</MudButton>   <!-- Missing Color.Error for destructive -->
```

---

### 2.2 MudTextField / MudNumericField / MudDatePicker / MudTimePicker

**Always use:** `Variant="Variant.Outlined"` + `Margin="Margin.Dense"` + `Class="ef-input"` (where applicable).

**✅ Correct:**
```razor
<MudTextField @bind-Value="_model.Name"
              Label="Nome"
              Variant="Variant.Outlined"
              Margin="Margin.Dense"
              Class="ef-input" />

<MudNumericField @bind-Value="_model.Quantity"
                 Label="Quantità"
                 Variant="Variant.Outlined"
                 Margin="Margin.Dense"
                 Min="0"
                 Class="ef-input" />

<MudDatePicker @bind-Date="_model.Date"
               Label="Data"
               Variant="Variant.Outlined"
               Margin="Margin.Dense"
               Class="ef-datepicker" />
```

**❌ Wrong:**
```razor
<MudTextField @bind-Value="_model.Name" Label="Nome" />                    <!-- No Variant -->
<MudTextField @bind-Value="_model.Name" Variant="Variant.Text" ... />      <!-- Wrong Variant -->
<MudTextField @bind-Value="_model.Name" Style="height:30px;" ... />        <!-- Inline style -->
```

---

### 2.3 MudSelect / MudAutocomplete

**Always use:** `Variant="Variant.Outlined"`.

**✅ Correct:**
```razor
<MudSelect @bind-Value="_model.Status"
           Label="Stato"
           Variant="Variant.Outlined"
           Class="ef-select">
    <MudSelectItem Value="@Status.Active">Attivo</MudSelectItem>
    <MudSelectItem Value="@Status.Inactive">Inattivo</MudSelectItem>
</MudSelect>

<MudAutocomplete @bind-Value="_selectedItem"
                 SearchFunc="SearchAsync"
                 Label="Cerca"
                 Variant="Variant.Outlined"
                 Margin="Margin.Dense" />
```

---

### 2.4 MudText

**Rules:**
- ALWAYS specify `Typo=` parameter
- Bold text: use `Class="fw-bold"` (or `fw-semibold`, `fw-medium`) — NOT `Style="font-weight:700"`
- Aligned text: use `Align=` parameter — NOT `Style="text-align:right"`
- Colored text: use `Color=` parameter — NOT `Style="color:var(...)"`

**Font-weight utility classes:**
| Class | Weight | Use when |
|-------|--------|----------|
| `.fw-medium` | 500 | Slightly emphasized body text |
| `.fw-semibold` | 600 | Labels, subtitles, table headers |
| `.fw-bold` | 700 | Key figures, amounts, strong emphasis |
| `.fw-extrabold` | 800 | Hero numbers, KPI values |

**✅ Correct:**
```razor
<MudText Typo="Typo.h5" Class="fw-bold">@_revenue</MudText>

<MudText Typo="Typo.subtitle1" Align="Align.Center">@_title</MudText>

<MudText Typo="Typo.body2" Color="Color.Secondary">@_subtitle</MudText>

<MudText Typo="Typo.caption" Class="fw-semibold">@_label</MudText>
```

**❌ Wrong:**
```razor
<MudText Style="font-weight:700;">@_revenue</MudText>               <!-- Use fw-bold class -->
<MudText Style="text-align:center;">@_title</MudText>              <!-- Use Align= param -->
<MudText Style="color:var(--mud-palette-secondary);">@s</MudText>  <!-- Use Color= param -->
<MudText>@_text</MudText>                                          <!-- Missing Typo= -->
```

---

### 2.5 MudCard / MudPaper

**Elevation standards:**
| Context | Elevation | Notes |
|---------|-----------|-------|
| Embedded/flat container | `0` | Inside another card or panel |
| Standard list card | `1` | Default raised look |
| Active/selected/highlighted | `2` | Current selection |
| Modal/dialog emphasis | `4` | Used sparingly |

**✅ Correct:**
```razor
@* List card *@
<MudCard Elevation="1">
    <MudCardContent>...</MudCardContent>
</MudCard>

@* Pure container *@
<MudPaper Elevation="0" Class="pa-4">
    ...
</MudPaper>

@* Highlighted *@
<MudCard Elevation="2" Class="selected-card">
    ...
</MudCard>
```

**❌ Wrong:**
```razor
<MudPaper Style="box-shadow: 0 2px 4px rgba(0,0,0,0.1);">...</MudPaper>  <!-- Use Elevation= -->
<MudCard Elevation="8">...</MudCard>                                       <!-- Too high -->
```

---

### 2.6 MudDialog

**Standards:**
- Default `MaxWidth="MaxWidth.Medium"` for standard forms
- `DisableBackdropClick="true"` for forms with unsaved data
- Use `EFDialog` wrapper component (provides consistent header, close button, progress)

**✅ Correct:**
```razor
<MudDialog>
    <DialogContent>
        <MudForm @ref="_form">
            ...
        </MudForm>
    </DialogContent>
    <DialogActions>
        <MudButton Variant="Variant.Text" OnClick="Cancel">Annulla</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SaveAsync">Salva</MudButton>
    </DialogActions>
</MudDialog>
```

---

### 2.7 Dialog/pagine multistep (PrymStepper / EFStepperDialog)

**Standards:**
- Non usare mai `MudStepper`/`MudStep` direttamente: usano `CaptureUnmatchedValues` e un `ActionContent` che, se non soppresso esplicitamente, renderizza sempre una propria barra di navigazione nativa (Precedente/Successivo/Completa) in aggiunta a qualunque footer custom — causa doppie barre di comandi (fix ST01).
- Usare `PrymStepper`/`PrymStep` (in `Prym.UI/Components/`) al posto di `MudStepper`/`MudStep`: whitelisted params, nessun `ActionContent` nativo, un solo stato reale (`ActiveIndex`/`ActiveIndexChanged`).
- Per il caso standard "dialog multistep con Annulla + Indietro/Avanti/Conferma nel footer", usare `EFStepperDialog` (in `Prym.Web/Shared/Components/Dialogs/`): combina `EFDialog`+`PrymStepper`+`PrymStepperActions` in un solo componente.
- Non usare `@bind-ActiveStepIndex` (era dead code su `MudStepper`, fix ST02): il binding reale è `ActiveIndex`/`ActiveIndexChanged`, sempre esplicito.
- `<PrymStep Title="...">` (maiuscolo): il parametro è `Title`, non `title` (fix ST03, tipico refuso da porting rapido).
- Side-effect legati all'avanzamento di step (validazioni, chiamate API, side-effect asincroni) vanno esposti tramite `BeforeNextAsync="@MyMethodAsync"` (`Func<int, Task<bool>>`, `true` = procedi) — non vanno incorporati in metodi che auto-avanzano l'indice: l'avanzamento dell'indice è responsabilità esclusiva di `PrymStepperActions`/`EFStepperDialog`.
- Se la navigazione richiede salti non sequenziali (step condizionali), usare `ResolveNextIndex`/`ResolvePreviousIndex` (`Func<int, int>`) — non aggiungere `OnClick` manuali che assegnano `_activeStep` direttamente.
- **Eccezioni documentate**: quando lo stepper è annidato in una tab di un dialog più grande, o il footer richiede logica non riconducibile al pattern generico (es. testo bottone condizionale, nessuna navigazione step-by-step), usare `PrymStepper`/`PrymStep` diretti (senza `EFStepperDialog`) e documentare l'eccezione con un commento nel file.

**✅ Correct (caso standard):**
```razor
<EFStepperDialog Icon="@Icons.Material.Outlined.Save"
                  Title="Wizard di esempio"
                  ActiveIndex="@_activeStep"
                  ActiveIndexChanged="@(i => _activeStep = i)"
                  StepCount="3"
                  CanGoNext="@CanAdvance()"
                  BeforeNextAsync="@HandleBeforeNextAsync"
                  OnComplete="SaveAsync">
    <PrymStep Title="Passo 1" Icon="@Icons.Material.Outlined.Info">...</PrymStep>
    <PrymStep Title="Passo 2" Icon="@Icons.Material.Outlined.Settings">...</PrymStep>
    <PrymStep Title="Passo 3" Icon="@Icons.Material.Outlined.Check">...</PrymStep>
</EFStepperDialog>
```

**❌ Wrong:**
```razor
<MudStepper @bind-ActiveStepIndex="_activeStep">  <!-- dead binding, doppia barra nav -->
    <MudStep title="Passo 1">...</MudStep>         <!-- refuso: title minuscolo -->
</MudStepper>
```

---

### 2.8 MudAlert

- Never use inline `Style=` on `<MudAlert`
- Use `Severity=` to convey meaning (not color)
- Use `Dense="true"` inside forms/cards for compact display

**✅ Correct:**
```razor
<MudAlert Severity="Severity.Error">@_errorMessage</MudAlert>
<MudAlert Severity="Severity.Success" Dense="true" Class="mb-2">Salvato!</MudAlert>
```

**❌ Wrong:**
```razor
<MudAlert Style="background-color:red;">@_errorMessage</MudAlert>  <!-- Use Severity -->
<MudAlert Severity="Severity.Info" Style="margin-top:8px;">...</MudAlert>  <!-- Use Class |
```

---

## §3 Anti-Patterns

### Anti-pattern 1: Inline font-weight
```razor
@* ❌ WRONG *@
<MudText Typo="Typo.body1" Style="font-weight:600;">Total</MudText>

@* ✅ CORRECT *@
<MudText Typo="Typo.body1" Class="fw-semibold">Total</MudText>
@* OR use the right Typo: *@
<MudText Typo="Typo.subtitle1">Total</MudText>  @* subtitle1 = 600 weight in theme *@
```

### Anti-pattern 2: Inline text-align on MudText
```razor
@* ❌ WRONG *@
<MudText Typo="Typo.h5" Style="text-align:right;">€ 1.234,56</MudText>

@* ✅ CORRECT *@
<MudText Typo="Typo.h5" Align="Align.Right">€ 1.234,56</MudText>
```

### Anti-pattern 3: Color via Style on MudText
```razor
@* ❌ WRONG *@
<MudText Style="color:var(--mud-palette-success);">+12%</MudText>

@* ✅ CORRECT *@
<MudText Color="Color.Success">+12%</MudText>
```

### Anti-pattern 4: Form inputs without Variant
```razor
@* ❌ WRONG *@
<MudTextField @bind-Value="_name" Label="Nome" />

@* ✅ CORRECT *@
<MudTextField @bind-Value="_name" Label="Nome"
              Variant="Variant.Outlined"
              Margin="Margin.Dense" />
```

### Anti-pattern 5: Hardcoded colors in CSS
```css
/* ❌ WRONG */
.my-element { background-color: #1F2F46; color: white; }

/* ✅ CORRECT */
.my-element {
    background-color: var(--mud-palette-appbar-background);
    color: var(--mud-palette-appbar-text);
}
```

### Anti-pattern 6: Missing Typo on MudText
```razor
@* ❌ WRONG *@
<MudText>Welcome back</MudText>

@* ✅ CORRECT *@
<MudText Typo="Typo.body1">Welcome back</MudText>
```

---

## §4 Dark Mode Checklist

Before committing any UI change, verify:

- [ ] Component uses `Color=` parameters (not hardcoded colors)
- [ ] CSS uses `var(--mud-palette-*)` variables (not hex colors)
- [ ] No `background-color: white` or `color: black` hardcoded
- [ ] Images/icons use `Color.Inherit` or theme color
- [ ] Borders use `var(--mud-palette-divider)`
- [ ] Shadows use `rgba(0,0,0,0.N)` with alpha (not solid colors)
- [ ] Test page in both light (`prym-light`) and dark (`prym-dark`) mode

**Key CSS variables for dark-mode-safe code:**
| Purpose | Variable |
|---------|----------|
| Page background | `var(--mud-palette-background)` |
| Card/surface | `var(--mud-palette-surface)` |
| Primary text | `var(--mud-palette-text-primary)` |
| Secondary text | `var(--mud-palette-text-secondary)` |
| Disabled text | `var(--mud-palette-text-disabled)` |
| Primary color | `var(--mud-palette-primary)` |
| Dividers | `var(--mud-palette-divider)` |
| AppBar background | `var(--mud-palette-appbar-background)` |
| AppBar text | `var(--mud-palette-appbar-text)` |

---

## §5 Dark Mode Checklist (moved — see §4)

> Checklist moved to §4 above.

---

## §6 CSS Class Reference

| Class | Purpose |
|-------|---------|
| `.ef-input` | Standard text/numeric input styling |
| `.ef-select` | Standard select styling |
| `.ef-datepicker` | Standard date picker styling |
| `.ef-appbar` | AppBar with theme colors |
| `.ef-dialog` | Dialog with theme colors |
| `.ef-menu-button` | Menu toggle button in AppBar |
| `.ef-account-menu` | Account menu button |
| `.ef-toolbar-button` | 38×38px toolbar icon button |
| `.ef-row-actionbutton` | 24×24px row action button |
| `.ef-table-sticky-header` | Table with sticky header support |
| `.ef-scroll-to-top` | Fixed floating scroll-to-top button |
| `.fw-medium` | font-weight: 500 |
| `.fw-semibold` | font-weight: 600 |
| `.fw-bold` | font-weight: 700 |
| `.fw-extrabold` | font-weight: 800 |

---

## §7 Page Architecture Rules

### 7.1 Decision table

| Pattern | When to use |
|---------|-------------|
| `EntityManagementPage` | The table is the main content; page is a management list with create/edit/delete/view or equivalent row actions; needs search, paging, export, column config, row actions |
| `EFTable` | Specialized list pages that are NOT CRUD: storici, diagnostiche, riconciliazioni, dashboard con tabella principale; maintain standard search/header/loading/empty-state |
| `MudTable` direct | **Exception only**, documented in file: wizard embedded tables, nested secondary tables, scheduler/workspace hybrids, specialized viewers, layouts incompatible with shared wrappers |

### 7.2 Page archetypes

1. **ManagementListPage** — base: `EntityManagementPage` — full CRUD list (anagrafiche, configurazioni, documenti…)
2. **SpecializedListPage** — base: `EFTable` — read-only or semi-read-only list (storici, diagnostica, monitoraggi)
3. **DashboardListPage** — KPI section + `EFTable` — analisi, riconciliazioni, dashboard con table principale
4. **WorkspacePage** — standard header + full-height area — POS, calendario, designer, wizard
5. **WizardPage** — standard header + step content + embedded table rules (use `MudTable` direct with documentation)

### 7.2bis Riferimenti canonici

Per l'archetipo **ManagementListPage**, il riferimento diretto da cui copiare struttura e pattern è:

- `Prym.Web/Pages/Management/Products/ProductManagement.razor` — azioni di toolbar extra
  (filtro classificazione, aggiornamento in blocco) tramite lo slot `AdditionalToolbarContent`.
- `Prym.Web/Pages/Management/Business/BusinessPartyManagement.razor` — pagina unificata
  clienti/fornitori (route `/business/parties/*`; `CustomerManagement.razor` e
  `SupplierManagement.razor` sono redirect verso questa). Azioni di riga extra
  (es. "Gestisci Prodotti" per i fornitori, Modifica) tramite lo slot `AdditionalRowActions`,
  utile anche come esempio di entità con più stati/relazioni.

Ogni nuova `ManagementListPage` deve partire da uno di questi due file come modello concreto,
non essere scritta da zero solo sulla base della tabella delle regole.

Per l'archetipo **SpecializedListPage** (`EFTable`), al momento non esiste nel codebase un
riferimento pienamente conforme a tutte le regole della §7.4 — non assumere che una pagina
`EFTable` esistente sia automaticamente un buon esempio da copiare senza prima verificarla
contro la checklist §8. Se una pagina `EFTable` viene portata a piena conformità, va candidata
qui come riferimento per le successive.

### 7.3 Mandatory exception documentation

Every direct `<MudTable` usage inside `Pages/` **must** carry a comment block with all placeholders replaced:

```razor
@* Eccezione documentata (Categoria D — WizardPage):
   questa pagina usa MudTable diretto perché è un wizard multi-step per la
   configurazione guidata delle stampanti fiscali. Le step tables embedded
   non sono compatibili con EntityManagementPage/EFTable senza duplicazioni
   strutturali o regressioni UX del wizard.
   TODO: riesaminare se EFTable supporterà tabelle embedded in wizard. *@
```

The template fields to fill in:
- **Categoria** — one of `WizardPage`, `WorkspacePage`, `DashboardListPage`, `SpecializedListPage`
- **motivo** — concrete technical reason why the shared wrapper is not suitable
- **TODO** — (optional) condition under which the exception could be removed

### 7.4 Row actions

| Scenario | Pattern |
|----------|---------|
| ≤ 3 primary actions | `ActionButtonGroup` |
| > 3 actions or mixed primary/secondary | `ActionButtonGroup` + `AdditionalRowActions` slot or `MudMenu` |
| Manual groups of `MudIconButton` in list pages | ❌ Forbidden without exception documentation |
| Single icon-only action (row or toolbar) | `PrymIconButton` (facade su `MudIconButton`, `Prym.UI/Components/PrymIconButton.razor`) — mai `MudIconButton` diretto in `Pages/`, salvo eccezione documentata |

Esempio corretto di azione di riga extra: `BusinessPartyManagement.razor`, slot `AdditionalRowActions`.
Esempio corretto di azione di toolbar extra: `ProductManagement.razor`, slot `AdditionalToolbarContent`.

### 7.5 Toolbar rules

- List pages: use the built-in search/filter/export/refresh of `EntityManagementPage` or `EFTable`.
- Do **not** reinvent toolbar components for pages where the shared wrappers cover the requirement.
- `ManagementPageHeader` is the standard page header for all management and list pages.
- Avoid manual duplicated headers (inline `MudPaper` + icon + title) when `ManagementPageHeader` covers the need.

---

## §8 PR Review Checklist — Page Architecture

Add these checks to every PR that introduces or modifies a page in `Pages/`:

### Page structure
- [ ] Is the page a list/management page? If yes, does it use `EntityManagementPage`?
- [ ] Is the page a specialized list (non-CRUD)? If yes, does it use at least `EFTable`?
- [ ] If `MudTable` is used directly in a `Pages/` file, is an exception comment present with a concrete technical justification?
- [ ] Are row actions using `ActionButtonGroup` or the shared `AdditionalRowActions` slot?
- [ ] Is the header using `ManagementPageHeader` (not a manual `MudPaper`+icon+title duplicate)?
- [ ] I pulsanti icona (di riga o di toolbar) usano `PrymIconButton`, non `MudIconButton` diretto?
- [ ] Se la pagina è una `ManagementListPage`, ricalca la struttura di `ProductManagement.razor` / `BusinessPartyManagement.razor` (config/service, `AdditionalToolbarContent`, `AdditionalRowActions`)?

### Styling (existing checks)
- [ ] No new `Style="font-weight:N"` on `<MudText` — use `Class="fw-*"` or `Typo=`
- [ ] No new `Style="text-align:..."` on `<MudText` — use `Align=`
- [ ] No hardcoded hex colors in CSS — use `var(--mud-palette-*)`
- [ ] No new `!important` unless specifically overriding MudBlazor

### Components (existing checks)
- [ ] All `<MudTextField`, `<MudSelect`, `<MudNumericField` have `Variant="Variant.Outlined"`
- [ ] All `<MudTextField`, `<MudNumericField` have `Margin="Margin.Dense"`
- [ ] All `<MudButton` have explicit `Variant=` and `Color=`
- [ ] All `<MudText` have `Typo=` parameter
- [ ] No `Style=` on `<MudAlert`

### Theme awareness
- [ ] Tested in both light and dark mode
- [ ] New colors use CSS variables
- [ ] New components use established CSS classes (`ef-input`, `ef-select`, etc.)

### Performance
- [ ] No unnecessarily deep CSS selectors
- [ ] No duplicate CSS rules

---

## §6 CSS Class Reference

| Class | Purpose |
|-------|---------|
| `.ef-input` | Standard text/numeric input styling |
| `.ef-select` | Standard select styling |
| `.ef-datepicker` | Standard date picker styling |
| `.ef-appbar` | AppBar with theme colors |
| `.ef-dialog` | Dialog with theme colors |
| `.ef-menu-button` | Menu toggle button in AppBar |
| `.ef-account-menu` | Account menu button |
| `.ef-toolbar-button` | 38×38px toolbar icon button |
| `.ef-row-actionbutton` | 24×24px row action button |
| `.ef-table-sticky-header` | Table with sticky header support |
| `.ef-scroll-to-top` | Fixed floating scroll-to-top button |
| `.fw-medium` | font-weight: 500 |
| `.fw-semibold` | font-weight: 600 |
| `.fw-bold` | font-weight: 700 |
| `.fw-extrabold` | font-weight: 800 |

---

*Last updated: UI standardization plan Phase 0 — see `MUDBLAZOR_AUDIT.md` for full audit data.*
