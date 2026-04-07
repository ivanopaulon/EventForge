# VAT Rate Management UI Mockup

## Main Page Layout

```
╔════════════════════════════════════════════════════════════════════════╗
║  Prym - Gestione Aliquote IVA                                   ║
╠════════════════════════════════════════════════════════════════════════╣
║                                                                        ║
║  📊 Gestione Aliquote IVA                                             ║
║  Gestisci le aliquote IVA per la tua organizzazione                  ║
║                                                                        ║
╠════════════════════════════════════════════════════════════════════════╣
║  ┌──────────────────────────────────────────────────────────────┐    ║
║  │ FILTRI                                                        │    ║
║  │                                                               │    ║
║  │ [🔍 Cerca aliquote IVA...]        [Stato ▼ Tutti]           │    ║
║  │                                                               │    ║
║  └──────────────────────────────────────────────────────────────┘    ║
╠════════════════════════════════════════════════════════════════════════╣
║  ┌──────────────────────────────────────────────────────────────┐    ║
║  │ 📋 Lista Aliquote IVA (5 elementi trovati)      [🔄][➕]    │    ║
║  ├──────────────────────────────────────────────────────────────┤    ║
║  │ ┌─────┬──────────┬──────┬──────────┬──────────┬─────────┐   │    ║
║  │ │Nome │Percent.  │Stato │Valido Da │Valido A  │Azioni   │   │    ║
║  │ ├─────┼──────────┼──────┼──────────┼──────────┼─────────┤   │    ║
║  │ │[%]  │          │      │          │          │         │   │    ║
║  │ │IVA  │ [22%]    │[✓]   │01/01/    │31/12/    │[👁][✏️]│   │    ║
║  │ │22%  │          │Attivo│2024      │2024      │[🗑️]    │   │    ║
║  │ ├─────┼──────────┼──────┼──────────┼──────────┼─────────┤   │    ║
║  │ │[%]  │          │      │          │          │         │   │    ║
║  │ │IVA  │ [10%]    │[✓]   │01/01/    │-         │[👁][✏️]│   │    ║
║  │ │10%  │          │Attivo│2024      │          │[🗑️]    │   │    ║
║  │ ├─────┼──────────┼──────┼──────────┼──────────┼─────────┤   │    ║
║  │ │[%]  │          │      │          │          │         │   │    ║
║  │ │IVA  │ [4%]     │[✓]   │01/01/    │-         │[👁][✏️]│   │    ║
║  │ │4%   │          │Attivo│2024      │          │[🗑️]    │   │    ║
║  │ └─────┴──────────┴──────┴──────────┴──────────┴─────────┘   │    ║
║  └──────────────────────────────────────────────────────────────┘    ║
╚════════════════════════════════════════════════════════════════════════╝
```

## Create/Edit Drawer (Opened from right side)

```
╔═══════════════════════════════════════════════╗
║ ← Crea Nuova Aliquota IVA                 [X]║
╠═══════════════════════════════════════════════╣
║                                               ║
║ ┌─────────────────────────────────────────┐   ║
║ │ Nome Aliquota IVA *                     │   ║
║ │ [IVA 22%                             ]  │   ║
║ │ Inserisci il nome dell'aliquota IVA     │   ║
║ └─────────────────────────────────────────┘   ║
║                                               ║
║ ┌─────────────────────────────────────────┐   ║
║ │ Percentuale *                           │   ║
║ │ [22                                   ]  │   ║
║ │ Percentuale dell'aliquota (0-100)       │   ║
║ └─────────────────────────────────────────┘   ║
║                                               ║
║ ┌─────────────────────────────────────────┐   ║
║ │ Stato *                           [▼]   │   ║
║ │ ┌───────────┐                            │   ║
║ │ │ Attivo    │                            │   ║
║ │ │ Sospeso   │                            │   ║
║ │ │ Eliminato │                            │   ║
║ │ └───────────┘                            │   ║
║ │ Stato dell'aliquota IVA                 │   ║
║ └─────────────────────────────────────────┘   ║
║                                               ║
║ ┌──────────────────┐  ┌──────────────────┐    ║
║ │ Valido Da    [📅]│  │ Valido A     [📅]│    ║
║ │ [01/01/2024    ] │  │ [            ]   │    ║
║ │ Data inizio      │  │ Data fine        │    ║
║ └──────────────────┘  └──────────────────┘    ║
║                                               ║
║ ┌─────────────────────────────────────────┐   ║
║ │ Note                                    │   ║
║ │ ┌─────────────────────────────────────┐ │   ║
║ │ │                                     │ │   ║
║ │ │ Aliquota standard per la maggior    │ │   ║
║ │ │ parte dei prodotti e servizi        │ │   ║
║ │ │                                     │ │   ║
║ │ └─────────────────────────────────────┘ │   ║
║ │ Note aggiuntive (max 200 caratteri)    │   ║
║ └─────────────────────────────────────────┘   ║
║                                               ║
║ ┌─────────────────────────────────────────┐   ║
║ │                                         │   ║
║ │  [Annulla]              [💾 Salva]     │   ║
║ │                                         │   ║
║ └─────────────────────────────────────────┘   ║
╚═══════════════════════════════════════════════╝
```

## View Mode Drawer

```
╔═══════════════════════════════════════════════╗
║ ← Visualizza Aliquota IVA: IVA 22%        [X]║
╠═══════════════════════════════════════════════╣
║                                               ║
║ Nome Aliquota IVA                             ║
║ ┌─────────────────────────────────────────┐   ║
║ │ IVA 22%                                 │   ║
║ └─────────────────────────────────────────┘   ║
║                                               ║
║ Percentuale                                   ║
║ ┌─────────────────────────────────────────┐   ║
║ │ 22%                                     │   ║
║ └─────────────────────────────────────────┘   ║
║                                               ║
║ Stato                                         ║
║ ┌─────────────────────────────────────────┐   ║
║ │ [✓ Attivo]                              │   ║
║ └─────────────────────────────────────────┘   ║
║                                               ║
║ Valido Da                  Valido A           ║
║ ┌──────────────────┐  ┌──────────────────┐    ║
║ │ 01/01/2024       │  │ 31/12/2024       │    ║
║ └──────────────────┘  └──────────────────┘    ║
║                                               ║
║ Note                                          ║
║ ┌─────────────────────────────────────────┐   ║
║ │ Aliquota standard per la maggior parte  │   ║
║ │ dei prodotti e servizi                  │   ║
║ └─────────────────────────────────────────┘   ║
║                                               ║
║ ID Aliquota IVA           Data Creazione      ║
║ ┌──────────────────┐  ┌──────────────────┐    ║
║ │ abc123...        │  │ 15/01/2024 10:30 │    ║
║ └──────────────────┘  └──────────────────┘    ║
║                                               ║
║ ┌─────────────────────────────────────────┐   ║
║ │                 [Chiudi]                │   ║
║ └─────────────────────────────────────────┘   ║
╚═══════════════════════════════════════════════╝
```

## Navigation Menu Integration

```
╔══════════════════════════════════════╗
║ Prym                           ║
╠══════════════════════════════════════╣
║                                      ║
║ 🔧 Super Amministrazione             ║
║   ├─ Gestione Tenant                ║
║   ├─ Gestione Utenti                ║
║   ├─ Gestione Licenze               ║
║   ├─ Switch Tenant                  ║
║   └─ ...                            ║
║                                      ║
║ 📊 Amministrazione              [▼]  ║
║   ├─ Dashboard Admin                ║
║   ├─ Gestione Lotti                 ║
║   ├─ Procedura Inventario           ║
║   ├─ Elenco Inventario              ║
║   ├─ Gestione Stampanti             ║
║   └─ 📈 Gestione Aliquote IVA ◄─    ║ ← NEW!
║                                      ║
║ 💬 Comunicazione                     ║
║   ├─ Notifiche                      ║
║   ├─ Feed Attività                  ║
║   └─ Chat                           ║
║                                      ║
║ 👤 Profilo                           ║
║                                      ║
║ ❓ Aiuto e Supporto                  ║
║                                      ║
╚══════════════════════════════════════╝
```

## Status Chips

```
Active:    [✓ Attivo]     (Green background)
Suspended: [⏸ Sospeso]    (Orange background)
Deleted:   [✗ Eliminato]  (Red background)
```

## Confirmation Dialog (Delete)

```
╔═════════════════════════════════════════════╗
║           Conferma                          ║
╠═════════════════════════════════════════════╣
║                                             ║
║  Sei sicuro di voler eliminare l'aliquota  ║
║  IVA 'IVA 22%'?                            ║
║                                             ║
║  Questa azione non può essere annullata.   ║
║                                             ║
╠═════════════════════════════════════════════╣
║                                             ║
║         [Annulla]      [Elimina]           ║
║                                             ║
╚═════════════════════════════════════════════╝
```

## Success Notification (Snackbar)

```
┌──────────────────────────────────────────┐
│ ✓ Aliquota IVA eliminata con successo!  │
└──────────────────────────────────────────┘
```

## Error Notification (Snackbar)

```
┌──────────────────────────────────────────┐
│ ✗ Errore nell'eliminazione dell'aliquota│
│   IVA: [error message]                   │
└──────────────────────────────────────────┘
```

## Empty State (No VAT Rates)

```
╔════════════════════════════════════════════════╗
║                                                ║
║              📈 (Large Icon)                   ║
║                                                ║
║        Nessuna aliquota IVA trovata            ║
║                                                ║
║   Crea la tua prima aliquota IVA per          ║
║   iniziare a gestire le imposte               ║
║                                                ║
║           [➕ Crea Aliquota IVA]              ║
║                                                ║
╚════════════════════════════════════════════════╝
```

## Filtered Empty State

```
╔════════════════════════════════════════════════╗
║                                                ║
║              📈 (Large Icon)                   ║
║                                                ║
║   Nessuna aliquota IVA corrisponde            ║
║   ai filtri applicati                         ║
║                                                ║
║           [🗑️ Cancella filtri]                ║
║                                                ║
╚════════════════════════════════════════════════╝
```

## Responsive Behavior

### Desktop (> 960px)
- Full table with all columns visible
- Drawer width: 700px
- Filters in one row

### Tablet (600px - 960px)
- Table with horizontal scroll
- Drawer width: 80% of screen
- Filters in one row (wrapped if needed)

### Mobile (< 600px)
- Table in card view (stacked)
- Drawer full width
- Filters stacked vertically
- Action buttons as dropdown menu

## Color Scheme

Based on Prym theme:
- Primary: Blue (#1976D2)
- Success: Green (#4CAF50) - Active status
- Warning: Orange (#FF9800) - Suspended status
- Error: Red (#F44336) - Deleted status
- Info: Light Blue (#2196F3) - Percentage chips
- Background: White/Light Grey
