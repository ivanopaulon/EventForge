# Inventory Simulation Button - UI Mockup

## Visual Description

This document provides a text-based visual representation of the Simulate Inventory button UI changes.

---

## Before: Active Session Toolbar (Original)

```
┌────────────────────────────────────────────────────────────────────┐
│  ℹ️  Sessione di Inventario Attiva                                 │
│     Documento #INV-2025-001 - 5 articoli contati                   │
│     Iniziata il 03/12/2025 22:30                                   │
│                                                                     │
│                     [✅ Finalizza]  [❌ Annulla]                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## After: Active Session Toolbar (With Simulate Button)

```
┌────────────────────────────────────────────────────────────────────┐
│  ℹ️  Sessione di Inventario Attiva                                 │
│     Documento #INV-2025-001 - 5 articoli contati                   │
│     Iniziata il 03/12/2025 22:30                                   │
│                                                                     │
│     [🔬 Simula Inventario]  [✅ Finalizza]  [❌ Annulla]           │
└────────────────────────────────────────────────────────────────────┘
```

### Button Details:
- **Icon**: 🔬 (Science/Laboratory flask)
- **Color**: Orange/Yellow (Warning)
- **Style**: Outlined (not filled)
- **Position**: First button, before Finalizza and Annulla
- **Tooltip**: "Inserisce automaticamente una riga per ogni prodotto del database"

---

## During Simulation: Progress Indicator

```
┌────────────────────────────────────────────────────────────────────┐
│  ℹ️  Sessione di Inventario Attiva                                 │
│     Documento #INV-2025-001 - 5 articoli contati                   │
│     Iniziata il 03/12/2025 22:30                                   │
│                                                                     │
│     [⏳ Simula Inventario]  [✅ Finalizza]  [❌ Annulla]           │
│           (disabled)           (disabled)    (disabled)             │
└────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────┐
│  ⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜  45%   │
│  Elaborazione prodotti: 450/1000                                   │
└────────────────────────────────────────────────────────────────────┘
```

### Progress Bar Details:
- **Color**: Orange (Warning - matches button)
- **Size**: Medium
- **Text**: Shows current/total with percentage
- **Updates**: Every 10 products

---

## Confirmation Dialog

```
┌────────────────────────────────────────────────────────────────────┐
│  Conferma Simulazione                                              │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Questa operazione inserirà una riga per ogni prodotto attivo     │
│  nel documento di inventario corrente. L'operazione potrebbe      │
│  richiedere alcuni minuti. Continuare?                            │
│                                                                     │
│                          [No]        [Sì]                          │
└────────────────────────────────────────────────────────────────────┘
```

---

## Success Message

```
┌────────────────────────────────────────────────────────────────────┐
│  ✅ Simulazione completata! Aggiunte 1000 righe al documento.     │
└────────────────────────────────────────────────────────────────────┘
```

---

## Error Messages

### No Products Found
```
┌────────────────────────────────────────────────────────────────────┐
│  ⚠️ Nessun prodotto attivo trovato                                │
└────────────────────────────────────────────────────────────────────┘
```

### No Location Available
```
┌────────────────────────────────────────────────────────────────────┐
│  ❌ Nessuna ubicazione disponibile                                │
└────────────────────────────────────────────────────────────────────┘
```

### Partial Success
```
┌────────────────────────────────────────────────────────────────────┐
│  ⚠️ Simulazione completata con errori.                            │
│     Aggiunte 950 righe, 50 errori.                                │
└────────────────────────────────────────────────────────────────────┘
```

### General Error
```
┌────────────────────────────────────────────────────────────────────┐
│  ❌ Errore durante la simulazione                                 │
└────────────────────────────────────────────────────────────────────┘
```

---

## Full Page Context

```
┌──────────────────────────────────────────────────────────────────────┐
│  📦 Procedura Inventario                     [📋 Visualizza Inventario] │
├──────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │  ℹ️  Sessione di Inventario Attiva                            │ │
│  │     Documento #INV-2025-001 - 5 articoli contati              │ │
│  │     Iniziata il 03/12/2025 22:30                              │ │
│  │                                                                │ │
│  │     [🔬 Simula Inventario]  [✅ Finalizza]  [❌ Annulla]      │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │  🏢 Seleziona Magazzino                                        │ │
│  │                                                                │ │
│  │  Magazzino: [Magazzino Centrale - MC01     ▼]  [✅ Sessione  │ │
│  │                                                    Attiva]     │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │  📷 Scansiona Codice a Barre                                   │ │
│  │                                                                │ │
│  │  Codice a Barre: [_________________]          [🔍 Cerca]      │ │
│  │  Scansiona o digita il codice a barre e premi Invio          │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │  📋 Articoli nel Documento di Inventario (5)                  │ │
│  │                                           [Solo Differenze]    │ │
│  │                                                                │ │
│  │  ┌──────┬──────────┬─────────┬────────────┬──────┬─────────┐ │ │
│  │  │Prod. │Ubicaz.   │Quantità │Aggiustam.  │Note  │Azioni   │ │ │
│  │  ├──────┼──────────┼─────────┼────────────┼──────┼─────────┤ │ │
│  │  │Prod1 │A-01-01   │   50    │    +5      │  💬  │✏️  🗑️  │ │ │
│  │  │Prod2 │A-01-02   │   30    │    -2      │      │✏️  🗑️  │ │ │
│  │  │Prod3 │B-02-01   │   100   │    N/A     │  💬  │✏️  🗑️  │ │ │
│  │  │...   │...       │   ...   │    ...     │  ... │...      │ │ │
│  │  └──────┴──────────┴─────────┴────────────┴──────┴─────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Button States

### Normal State (Enabled)
```
┌─────────────────────────┐
│ 🔬 Simula Inventario    │  ← Orange outline, white background
└─────────────────────────┘
```

### Hover State
```
┌─────────────────────────┐
│ 🔬 Simula Inventario    │  ← Orange background, white text
└─────────────────────────┘
```

### Disabled State (During Simulation)
```
┌─────────────────────────┐
│ ⏳ Simula Inventario    │  ← Gray, with spinner icon
└─────────────────────────┘
```

### Disabled State (No Session)
```
[Button is not visible when no active session]
```

---

## Color Scheme

### Button Colors
- **Primary**: Orange/Yellow (`Color.Warning`)
- **Variant**: Outlined (not filled)
- **Border**: Orange outline, 1-2px
- **Background**: White/Transparent when not hovered
- **Hover**: Orange background, white text
- **Disabled**: Gray, semi-transparent

### Progress Bar Colors
- **Fill**: Orange (`Color.Warning`)
- **Background**: Light gray
- **Text**: Dark gray/black

---

## Responsive Behavior

### Desktop (> 960px)
```
[🔬 Simula Inventario]  [✅ Finalizza]  [❌ Annulla]
    (all buttons in a row)
```

### Tablet (600-960px)
```
[🔬 Simula Inventario]
[✅ Finalizza]
[❌ Annulla]
    (buttons stack vertically if needed)
```

### Mobile (< 600px)
```
[🔬 Simula Inventario]
[✅ Finalizza]
[❌ Annulla]
    (full-width buttons, stacked)
```

---

## Interaction Flow

### Step-by-Step User Journey

1. **Start**: User has active inventory session
   ```
   [Session Active Alert with 3 buttons visible]
   ```

2. **Click**: User clicks "Simula Inventario" button
   ```
   [Confirmation dialog appears]
   ```

3. **Confirm**: User clicks "Sì" in confirmation dialog
   ```
   [Button shows spinner, all buttons disabled]
   [Progress bar appears below session alert]
   ```

4. **Progress**: Simulation runs, progress updates
   ```
   [Progress bar fills: 0% → 10% → 20% → ... → 100%]
   [Text updates: "Elaborazione prodotti: 0/1000" → "1000/1000"]
   ```

5. **Complete**: Simulation finishes
   ```
   [Success message appears]
   [Progress bar disappears]
   [Buttons re-enable]
   [Table updates with all products]
   ```

---

## Accessibility Features

### ARIA Labels
- Button: `aria-label="Simula Inventario"`
- Progress: `role="progressbar"`, `aria-valuenow`, `aria-valuemin`, `aria-valuemax`
- Tooltip: `aria-describedby` for screen readers

### Keyboard Navigation
- **Tab**: Focus on button
- **Enter/Space**: Activate button
- **Esc**: Close confirmation dialog

### Screen Reader Announcements
1. "Simula Inventario, pulsante"
2. "Inserisce automaticamente una riga per ogni prodotto del database"
3. "Elaborazione prodotti: 450 di 1000"
4. "Simulazione completata! Aggiunte 1000 righe al documento"

---

## Animation & Transitions

### Button Click
- Ripple effect (MudBlazor default)
- Color transition: 200ms ease

### Progress Bar
- Fill animation: smooth, linear
- Updates every 10 products (not continuous)

### Dialog
- Fade in: 150ms
- Backdrop blur: 200ms

---

## Visual Hierarchy

1. **Session Alert** (Blue info banner - most prominent)
2. **Simulate Button** (Orange outline - secondary action)
3. **Finalize Button** (Green filled - primary action)
4. **Cancel Button** (Default outline - tertiary action)
5. **Progress Bar** (When visible - temporary, informational)

---

## Design Rationale

### Why Orange/Warning Color?
- Distinguishes test/development features from production actions
- Signals caution (mass operation)
- Matches MudBlazor Warning severity level

### Why Outlined Variant?
- Less prominent than filled buttons
- Indicates secondary/optional action
- Doesn't compete with primary "Finalizza" button

### Why Science Icon?
- Represents testing/experimentation
- Recognizable as development tool
- Matches other DevTools in the application

### Why Progress Bar?
- Long operation needs feedback
- Prevents user anxiety
- Shows operation is not frozen
- Provides estimate of completion

---

## Comparison with Existing Buttons

### Primary Action (Finalizza)
- **Color**: Green (`Color.Success`)
- **Variant**: Filled
- **Icon**: Checkmark
- **Message**: "Apply changes and close"

### Secondary Action (Annulla)
- **Color**: Default/Gray
- **Variant**: Outlined
- **Icon**: X mark
- **Message**: "Cancel without saving"

### Development Action (Simula)
- **Color**: Orange (`Color.Warning`)
- **Variant**: Outlined
- **Icon**: Science flask
- **Message**: "Test feature, populate automatically"

---

This UI design follows MudBlazor conventions and EventForge design patterns while clearly distinguishing the simulation feature as a development/testing tool.
