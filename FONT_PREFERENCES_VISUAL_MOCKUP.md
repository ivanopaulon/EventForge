# Font Preferences Dialog - Visual Mockup Guide

## 🎨 UI Layout Overview

This document provides a visual description of the implemented Font Preferences Dialog since actual screenshots cannot be taken in the sandbox environment.

---

## Dialog Structure

### Header
```
┌────────────────────────────────────────────────────────────┐
│  Preferenze Font                                       [X] │
└────────────────────────────────────────────────────────────┘
```

---

### Section 1: Preset Veloci (Quick Presets)

```
┌────────────────────────────────────────────────────────────┐
│ PRESET VELOCI                                              │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────────────┐  ┌──────────────────────┐      │
│  │ 📝 TUTTO SANS        │  │ 📖 SERIF PER LETTURA │      │
│  │ Moderno e pulito     │  │ Classico e leggibile │      │
│  └──────────────────────┘  └──────────────────────┘      │
│                                                            │
│  ┌──────────────────────┐  ┌──────────────────────┐      │
│  │ 📰 TITOLI DISPLAY    │  │ 📄 EDITORIALE        │      │
│  │ Impatto visivo       │  │ Elegante, mix serif  │      │
│  └──────────────────────┘  └──────────────────────┘      │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Preset Details:**

1. **TUTTO SANS** (Icon: TextFields)
   - Body: Noto Sans
   - Headings: Noto Sans
   - Outlined button (turns Primary when selected)

2. **SERIF PER LETTURA** (Icon: MenuBook)
   - Body: Noto Serif
   - Headings: Noto Sans
   - Outlined button (turns Primary when selected)

3. **TITOLI DISPLAY** (Icon: Title)
   - Body: Noto Sans
   - Headings: Noto Sans Display ⭐ (default)
   - Outlined button (turns Primary when selected)

4. **EDITORIALE** (Icon: Article)
   - Body: Noto Serif
   - Headings: Noto Serif Display
   - Outlined button (turns Primary when selected)

---

### Section 2: Personalizza (Customize)

```
┌────────────────────────────────────────────────────────────┐
│ PERSONALIZZA                                               │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ Font per Testo Corpo                                       │
│ ┌────────────────────────────────────────────────────────┐ │
│ │ Noto Sans - Moderno, neutro                         ▼ │ │
│ └────────────────────────────────────────────────────────┘ │
│ Usato in paragrafi, descrizioni, labels                   │
│                                                            │
│ Font per Titoli                                            │
│ ┌────────────────────────────────────────────────────────┐ │
│ │ Noto Sans Display (Consigliato)                     ▼ │ │
│ └────────────────────────────────────────────────────────┘ │
│ Usato in H1-H6, card headers, page titles                 │
│                                                            │
│ Font per Codice                                            │
│ ┌────────────────────────────────────────────────────────┐ │
│ │ Noto Sans Mono                                    🔒  │ │
│ └────────────────────────────────────────────────────────┘ │
│ Unica opzione monospace                                   │
│                                                            │
│ Dimensione Font: 16 px                                    │
│ ●─────────────●─────────────                              │
│ 12           16           24                              │
│                                                            │
│ [ ] Abilita Supporto Lingue Estese                        │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Dropdown Options:**

**Font per Testo Corpo:**
- Noto Sans - Moderno, neutro, ottimo per UI
- Noto Serif - Classico, leggibile, elegante

**Font per Titoli:**
- Noto Sans
- Noto Sans Display (Consigliato) ⭐
- Noto Serif
- Noto Serif Display

**Font per Codice:**
- Noto Sans Mono (Read-only, locked)

---

### Section 3: Anteprima Live (Live Preview)

```
┌────────────────────────────────────────────────────────────┐
│ ANTEPRIMA LIVE                                             │
│ L'anteprima si aggiorna in tempo reale                     │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  [Generale] [Titoli] [Componenti]                         │
│  ─────────                                                 │
│                                                            │
│  TAB CONTENT APPEARS HERE                                  │
│  (See detailed tabs below)                                 │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## Tab 1: Generale (General Preview)

```
┌────────────────────────────────────────────────────────────┐
│  EventForge Platform                    ← H3 (Headings)   │
│                                                            │
│  Sistema di Gestione Eventi             ← H5 (Headings)   │
│                                                            │
│  EventForge è una piattaforma completa per la gestione    │
│  di eventi aziendali. Permette di organizzare,            │
│  tracciare e analizzare eventi in modo efficiente.        │
│  Con un'interfaccia intuitiva e strumenti potenti,        │
│  semplifica ogni aspetto della gestione eventi.           │
│                                         ← Body Font        │
│                                                            │
│  [Azione Principale] [Annulla]          ← Buttons (Body)  │
│                                                            │
│  ┌────────────────────────────────────────────────────┐   │
│  │ Esempio JSON:                                      │   │
│  │ {                                                  │   │
│  │   "event": "UserLogin",              ← Monospace   │   │
│  │   "timestamp": "2026-01-28T18:30:00Z",            │   │
│  │   "status": "success"                             │   │
│  │ }                                                  │   │
│  └────────────────────────────────────────────────────┘   │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Elements Shown:**
- H3 heading: "EventForge Platform" (uses HeadingsFont)
- H5 heading: "Sistema di Gestione Eventi" (uses HeadingsFont)
- Paragraph: Long description text (uses BodyFont)
- Buttons: Primary and Outlined (use BodyFont)
- Code block: JSON example (uses MonospaceFont)

---

## Tab 2: Titoli (Headings Preview)

```
┌────────────────────────────────────────────────────────────┐
│  H1: Titolo Principale                                     │
│  ════════════════════════════════════                      │
│                                                            │
│  H2: Titolo Sezione                                        │
│  ═══════════════════                                       │
│                                                            │
│  H3: Sottotitolo                                           │
│  ═══════════════                                           │
│                                                            │
│  H4: Intestazione                                          │
│  ════════════════                                          │
│                                                            │
│  H5: Titolo Card                                           │
│  ═══════════════                                           │
│                                                            │
│  H6: Sezione Minore                                        │
│  ══════════════════                                        │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Purpose:**
Shows the complete heading hierarchy (H1-H6) to demonstrate:
- Font style at different heading levels
- Relative size scaling
- Visual weight and impact
- All using the selected HeadingsFont

---

## Tab 3: Componenti (Components Preview)

```
┌────────────────────────────────────────────────────────────┐
│  ┌────────────────────────────────────────────────────┐   │
│  │ Card Header              ← H6 (Headings Font)      │   │
│  ├────────────────────────────────────────────────────┤   │
│  │                                                    │   │
│  │ Questo è il contenuto di una card MudBlazor.       │   │
│  │ Il testo usa il font body selezionato.            │   │
│  │                          ← Body Font               │   │
│  │                                                    │   │
│  └────────────────────────────────────────────────────┘   │
│                                                            │
│  ┌────────────────────────────────────────────────────┐   │
│  │ Nome         │ Tipo        │ Stato                │   │
│  │──────────────┼─────────────┼──────────────────────│   │
│  │ Evento 2026  │ Conferenza  │ Attivo               │   │
│  │ Meeting Q1   │ Riunione    │ Completato           │   │
│  │ Workshop     │ Formazione  │ Pianificato          │   │
│  └────────────────────────────────────────────────────┘   │
│  ↑ Headers use HeadingsFont, cells use BodyFont           │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Components Shown:**
1. **MudCard:**
   - Card Header: Uses HeadingsFont
   - Card Content: Uses BodyFont

2. **MudTable:**
   - Table Headers (Nome, Tipo, Stato): Use HeadingsFont
   - Table Cells (data rows): Use BodyFont
   - 3 sample rows demonstrating real-world usage

---

### Footer Actions

```
┌────────────────────────────────────────────────────────────┐
│                                                            │
│  [🔄 Reset]              [Annulla]  [💾 Salva]           │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Buttons:**
- **Reset** (left): Restores defaults (Noto Sans, Noto Sans Display, 16px)
- **Annulla** (right): Closes without saving
- **Salva** (right, primary): Saves to localStorage + server, applies globally

---

## Real-Time Behavior

### When User Changes Dropdown:
1. User selects "Noto Serif" for Body Font
2. **INSTANTLY** (no save needed):
   - Tab 1 paragraph text changes to Noto Serif
   - Tab 1 button text changes to Noto Serif
   - Tab 3 card content changes to Noto Serif
   - Tab 3 table cells change to Noto Serif

### When User Moves Slider:
1. User drags slider from 16px to 20px
2. **INSTANTLY**:
   - All preview text scales proportionally
   - Headers become larger
   - Body text becomes larger
   - Code block text becomes larger (with offset)

### When User Clicks Preset:
1. User clicks "EDITORIALE" preset
2. **INSTANTLY**:
   - Preset button highlights in primary color
   - Body Font dropdown changes to "Noto Serif"
   - Headings Font dropdown changes to "Noto Serif Display"
   - All preview tabs update with new fonts
   - User can still customize further

---

## Color Scheme

### Default State:
- Preset buttons: Outlined, default color
- Dropdowns: Outlined variant
- Background: Light grey (`var(--mud-palette-background-grey)`)

### Active State:
- Selected preset: Primary color (blue)
- Focused fields: Primary color outline

### Typography:
- Section headers: Typo.h6
- Labels: Default MudBlazor labels
- Helper text: Caption, secondary color

---

## Responsive Behavior

### Desktop (>960px):
- Presets: 2x2 grid
- All controls full width
- Preview tabs horizontally arranged

### Tablet (600-960px):
- Presets: 2x2 grid (smaller)
- Controls stack vertically
- Preview maintains tabs

### Mobile (<600px):
- Presets: 1 column (4 rows)
- All controls full width, stacked
- Preview tabs remain functional

---

## Accessibility Features

### WCAG Compliance:
- Font size range: 12-24px (meets contrast requirements)
- All buttons: Minimum 44x44px touch targets
- Helper text: Clear descriptions for screen readers
- Focus indicators: Visible on all interactive elements

### Keyboard Navigation:
- Tab through all controls
- Arrow keys in dropdowns
- Space to toggle switches
- Enter to click buttons

---

## Font Application Flow

```
User opens dialog
    ↓
Shows current preferences
    ↓
User modifies (preset or custom)
    ↓
Preview updates INSTANTLY
    ↓
User clicks "Salva"
    ↓
┌─────────────────────────┐
│ 1. Save to localStorage │
│ 2. Send to server       │
│ 3. Apply CSS globally   │
│ 4. Close dialog         │
└─────────────────────────┘
    ↓
Entire app uses new fonts
```

---

## Expected Visual Impact

### Preset: "Tutto Sans" (Modern)
- Clean, modern look
- Uniform appearance
- Best for UI-heavy pages
- Example: ProductManagement, Dashboard

### Preset: "Serif per Lettura" (Classic)
- Professional, readable
- Serif body with Sans headers
- Best for content-heavy pages
- Example: Help pages, Documentation

### Preset: "Titoli Display" (Default)
- Balanced design
- Eye-catching headers
- Professional body text
- Best for most use cases

### Preset: "Editoriale" (Elegant)
- Sophisticated appearance
- Magazine-style feel
- Serif throughout
- Best for editorial content

---

## Technical Notes

### CSS Variables Applied:
```css
--font-family-body: /* User selection */
--font-family-headings: /* User selection */
--font-family-monospace: 'Noto Sans Mono'
--font-family-content: /* User selection */
```

### Global Impact:
- All `<h1>` through `<h6>` tags
- All MudBlazor typography components
- All card headers
- All table headers
- All body text
- All code blocks
- **Zero manual updates needed**

---

## Testing Checklist

When testing the actual UI:

- [ ] Open dialog from user menu
- [ ] Click each of 4 presets, verify button highlights
- [ ] Verify preview updates instantly for each preset
- [ ] Select custom Body Font, verify preview updates
- [ ] Select custom Headings Font, verify preview updates
- [ ] Move slider, verify all text scales
- [ ] Switch between all 3 preview tabs
- [ ] Click Reset, verify defaults restored
- [ ] Click Save, verify persistence
- [ ] Refresh page, verify settings persist
- [ ] Navigate to another page, verify fonts applied
- [ ] Test on mobile, tablet, desktop sizes
- [ ] Test keyboard navigation
- [ ] Test screen reader compatibility

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-28  
**Status:** Implementation Complete - Ready for Visual Testing
