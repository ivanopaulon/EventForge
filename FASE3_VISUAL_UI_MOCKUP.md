# FASE 3 - Visual UI Mockup and Component Structure

## 🎨 UI Layout Preview

### BusinessPartyDetail Page - Tab Navigation

```
┌─────────────────────────────────────────────────────────────────┐
│  ← Back    Business Party Name                    [Active] 🔧   │
│                                                          [Save]  │
├─────────────────────────────────────────────────────────────────┤
│  ℹ️ Generale  │  📞 Recapiti ⓿  │  📦 Operativo  │  🛒 Commerciale ⓿  │  💰 Contabilità  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  COMMERCIALE TAB CONTENT (see below)                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Commerciale Tab - Main Layout

```
┌──────────────────────────────────────────────────────────────────────┐
│  📋 Listini Prezzi Assegnati                                         │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌────────────────────┐         ┌────────────────────┐              │
│  │ 🏷️ Listini Vendita │         │ 🚚 Listini Acquisto│              │
│  ├────────────────────┤         ├────────────────────┤              │
│  │                    │         │                    │              │
│  │ ┌────────────────┐ │         │ ┌────────────────┐ │              │
│  │ │ Listino 2024   │ │         │ │ Fornitore XYZ  │ │              │
│  │ │ [⭐ Predefinito]│ │         │ │                │ │              │
│  │ │ 01/01-31/12/24 │ │         │ │ 01/01-31/12/24 │ │              │
│  │ │ [👁️] [🗗]       │ │         │ │ [👁️] [🗗]       │ │              │
│  │ └────────────────┘ │         │ └────────────────┘ │              │
│  │                    │         │                    │              │
│  │ ┌────────────────┐ │         │ ℹ️ Nessun listino  │              │
│  │ │ Listino VIP    │ │         │    acquisto        │              │
│  │ │ 01/06-31/08/24 │ │         │    assegnato       │              │
│  │ │ [👁️] [🗗]       │ │         │                    │              │
│  │ └────────────────┘ │         │                    │              │
│  └────────────────────┘         └────────────────────┘              │
│                                                                      │
├──────────────────────────────────────────────────────────────────────┤
│  💳 Card Fedeltà                               [+ Nuova Card] 🔒     │
├──────────────────────────────────────────────────────────────────────┤
│  ℹ️ 🚧 Funzionalità in Fase di Progettazione                         │
│     La gestione delle card fedeltà sarà disponibile in Fase 4       │
└──────────────────────────────────────────────────────────────────────┘
```

### Price List Assignment Card - Detail View

```
┌───────────────────────────────────────────┐
│  Listino Vendita 2024   [⭐ Predefinito]  │
│  ─────────────────────────────────────    │
│  Validità: 01/01/2024 - 31/12/2024       │
│  Listino generale per vendita al pubblico│
│                                           │
│  [👁️ Preview] [🗗 Apri Dettaglio]          │
└───────────────────────────────────────────┘

Legend:
  ⭐ = Default badge (green)
  👁️ = Preview button (info color)
  🗗 = Open in new tab button (primary color)
```

### Price List Preview Dialog

```
┌─────────────────────────────────────────────────┐
│  Anteprima Listino                          [X] │
├─────────────────────────────────────────────────┤
│                                                 │
│  Listino Vendita 2024                           │
│                                                 │
│  ┌─────────────┐  ┌─────────────┐              │
│  │ Tipo        │  │ Codice      │              │
│  │ Sales       │  │ VEN2024     │              │
│  └─────────────┘  └─────────────┘              │
│                                                 │
│  Validità                                       │
│  01/01/2024 - 31/12/2024                        │
│                                                 │
│  Descrizione                                    │
│  Listino generale per vendita al pubblico       │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ ℹ️ Questa è un'anteprima rapida. Per      │ │
│  │    visualizzare i prezzi e gestire il     │ │
│  │    listino, clicca "Vai al Dettaglio".    │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│          [Chiudi] [🗗 Vai al Dettaglio Completo]│
└─────────────────────────────────────────────────┘
```

---

## 📦 Component Hierarchy

```
BusinessPartyDetail.razor
  └─ MudTabs
      ├─ GeneralInfoTab
      ├─ RecapitiTab
      ├─ OperativoTab
      ├─ CommercialeTab ⭐ NEW
      │   ├─ Price Lists Section
      │   │   ├─ Sales Column
      │   │   │   └─ PriceListAssignmentCard (multiple)
      │   │   │       ├─ Preview Button → PriceListPreviewDialog
      │   │   │       └─ Detail Button → Opens new tab
      │   │   └─ Purchase Column
      │   │       └─ PriceListAssignmentCard (multiple)
      │   └─ Fidelity Section (placeholder)
      └─ AccountingTab (conditional)
```

---

## 🎨 Color Scheme

### Primary Elements
- **Tab Icon:** 🛒 Shopping Cart (Blue)
- **Sales Icon:** 🏷️ Sell (Primary Blue)
- **Purchase Icon:** 🚚 Local Shipping (Secondary Purple/Gray)

### Status Indicators
- **Default Badge:** ⭐ Star (Success Green)
- **Active Status:** ✅ Green
- **Inactive Status:** ⚫ Gray

### Action Buttons
- **Preview:** 👁️ Eye (Info Blue)
- **Open Detail:** 🗗 Open in New (Primary Blue)
- **Disabled Button:** 🔒 Locked (Gray)

### Paper Elevation
- All cards: `Elevation="1"`
- Main sections: `Elevation="1"`

---

## 📱 Responsive Behavior

### Desktop (≥960px)
```
┌─────────────────────────────────────┐
│  Sales Listini    │  Purchase Listini│
│  (50% width)      │  (50% width)     │
└─────────────────────────────────────┘
```

### Mobile (<960px)
```
┌───────────────────┐
│  Sales Listini    │
│  (100% width)     │
├───────────────────┤
│  Purchase Listini │
│  (100% width)     │
└───────────────────┘
```

---

## 🔄 User Interaction Flow

### Flow 1: Preview Price List
```
1. User clicks "Commerciale" tab
   ↓
2. CommercialeTab loads (lazy)
   ↓
3. Fetches price lists from API
   ↓
4. Displays cards in two columns
   ↓
5. User clicks 👁️ Preview button
   ↓
6. PriceListPreviewDialog opens
   ↓
7. Shows basic price list info
   ↓
8. User clicks "Chiudi" or X
   ↓
9. Dialog closes, stays on tab
```

### Flow 2: Open Full Detail
```
1. User on Commerciale tab
   ↓
2. User clicks 🗗 Detail button
   ↓
3. Opens new browser tab
   ↓
4. Navigates to /management/pricelists/{id}
   ↓
5. Includes return URL for navigation back
```

### Flow 3: Empty State
```
1. User clicks "Commerciale" tab
   ↓
2. No price lists assigned
   ↓
3. Shows info alert:
   "Nessun listino vendita assegnato"
   "Nessun listino acquisto assegnato"
```

---

## 🎭 State Management

### Loading States
```
┌─────────────────────┐
│  📋 Listini Prezzi  │
├─────────────────────┤
│  ▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬  │  ← Progress Bar
└─────────────────────┘
```

### Error States
```
┌─────────────────────┐
│  📋 Listini Prezzi  │
├─────────────────────┤
│  ⚠️ Errore nel      │
│     caricamento     │
└─────────────────────┘
```

### Empty States
```
┌─────────────────────┐
│  🏷️ Listini Vendita │
├─────────────────────┤
│  ℹ️ Nessun listino  │
│     vendita         │
│     assegnato       │
└─────────────────────┘
```

---

## 🔧 Technical Rendering Notes

### Lazy Loading Behavior
1. Tab initially shows: `TabLoadState.NotLoaded`
2. User clicks tab → `OnTabChanged(3)` triggered
3. State changes to: `TabLoadState.Loaded`
4. Component renders: `<CommercialeTab ... />`
5. Component fetches data in `OnInitializedAsync()`
6. Shows loading indicator during fetch
7. Renders cards when data arrives

### Badge Behavior
- **Badge Data:** `_priceListsCount` (currently always 0)
- **Badge Dot:** Shows if count > 0
- **Note:** Count not populated in current implementation

---

## 🎨 CSS Classes Used

### MudBlazor Components
- `MudGrid` with `Spacing="3"` or `Spacing="4"`
- `MudItem` with responsive breakpoints (xs="12", md="6")
- `MudPaper` with `Elevation="1"`
- `MudStack` with `Row="true"` or vertical
- `MudText` with various `Typo` values
- `MudIcon` with Material icons
- `MudAlert` with severity levels
- `MudButton` with variants
- `MudChip` for badges
- `MudDialog` for modals

### Custom Classes
- `.pa-4` - Padding all sides 16px
- `.pa-3` - Padding all sides 12px
- `.mb-4` - Margin bottom 16px
- `.mb-3` - Margin bottom 12px
- `.mt-2` - Margin top 8px
- `.mr-2` - Margin right 8px

---

## 📊 Data Flow Diagram

```
┌─────────────────┐
│ BusinessParty   │
│    Detail       │
│   (Parent)      │
└────────┬────────┘
         │
         │ Passes BusinessPartyId
         │ and PartyType
         ▼
┌─────────────────┐
│  Commerciale    │
│      Tab        │
└────────┬────────┘
         │
         │ Calls Service
         ▼
┌─────────────────┐       GET /api/v1/product-management/
│  PriceList      │  →    business-parties/{id}/price-lists
│    Service      │       ?type={Sales|Purchase}
└────────┬────────┘
         │
         │ Returns List<PriceListDto>
         ▼
┌─────────────────┐
│  Component      │
│   Renders:      │
│   - Sales       │
│   - Purchase    │
└─────────────────┘
```

---

## 🎯 Acceptance Criteria Visual Checklist

| Criteria | Visual Element | Status |
|----------|----------------|--------|
| 1. Tab visible | 🛒 Commerciale tab | ✅ |
| 2. Two columns | Sales │ Purchase | ✅ |
| 3. Badge count | ⓿ on tab | ⚠️ Always 0 |
| 4. Default badge | ⭐ Predefinito chip | ✅ |
| 5. Validity dates | DD/MM/YYYY - DD/MM/YYYY | ✅ |
| 6. Preview button | 👁️ icon | ✅ |
| 7. Detail button | 🗗 icon | ✅ |
| 8. Preview dialog | Modal with info | ✅ |
| 9. Fidelity section | Placeholder visible | ✅ |
| 10. Loading state | Progress bar | ✅ |
| 11. Empty state | Info alert | ✅ |
| 12. Error handling | Error messages | ✅ |

---

## 🚀 Future Enhancement Mockups

### Phase 4 - Fidelity Cards (Placeholder)

```
┌──────────────────────────────────────────────────┐
│  💳 Card Fedeltà                  [+ Nuova Card] │
├──────────────────────────────────────────────────┤
│  ┌────────────────┐  ┌────────────────┐         │
│  │ Gold Card      │  │ Silver Card    │         │
│  │ ⭐ VIP Status   │  │ Standard       │         │
│  │ 1000 punti     │  │ 250 punti      │         │
│  │ [Edit] [View]  │  │ [Edit] [View]  │         │
│  └────────────────┘  └────────────────┘         │
└──────────────────────────────────────────────────┘
```

---

This visual mockup provides a clear representation of the UI implementation without requiring actual screenshots or images.
