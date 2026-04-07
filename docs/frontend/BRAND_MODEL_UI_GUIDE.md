# Visual Guide: Brand and Model Management UI

## Brand Management Page

### Page Layout
```
┌─────────────────────────────────────────────────────────────────┐
│  [☰ Menu]                Prym                  [Profile▾] │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  🏷️ Gestione Marchi                                             │
│  Gestisci i marchi dei prodotti                                 │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐│
│  │ 🔍 Cerca marchi [               ] [Clear]                  ││
│  └────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐│
│  │ 📋 Lista Marchi (3 elementi trovati)  [🔄][➕ Crea nuovo] ││
│  ├────────────────────────────────────────────────────────────┤│
│  │ Nome        │ Descrizione      │ Paese │ Sito Web │ Azioni ││
│  ├────────────────────────────────────────────────────────────┤│
│  │ Samsung     │ Electronics...   │ KR    │ Visita   │ 👁️✏️🗑️ ││
│  │ LG          │ Home applian...  │ KR    │ Visita   │ 👁️✏️🗑️ ││
│  │ Bosch       │ Engineering...   │ DE    │ Visita   │ 👁️✏️🗑️ ││
│  └────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────┘
```

### Brand Drawer (Create Mode)
```
                                    ┌──────────────────────────┐
                                    │ ➕ Crea Nuovo Marchio    │
                                    ├──────────────────────────┤
                                    │                          │
                                    │ Nome Marchio *           │
                                    │ [                      ] │
                                    │ Inserisci il nome...     │
                                    │                          │
                                    │ Descrizione              │
                                    │ [                      ] │
                                    │ [                      ] │
                                    │ [                      ] │
                                    │ Descrizione opzionale... │
                                    │                          │
                                    │ Sito Web                 │
                                    │ [                      ] │
                                    │ URL del sito web...      │
                                    │                          │
                                    │ Paese                    │
                                    │ [                      ] │
                                    │ Paese di origine...      │
                                    │                          │
                                    ├──────────────────────────┤
                                    │ [Chiudi]        [Salva] │
                                    └──────────────────────────┘
```

## Model Management Page

### Page Layout
```
┌─────────────────────────────────────────────────────────────────┐
│  [☰ Menu]                Prym                  [Profile▾] │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  📦 Gestione Modelli                                            │
│  Gestisci i modelli dei prodotti                                │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐│
│  │ 🔍 Cerca modelli [               ] [Clear]                 ││
│  └────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐│
│  │ 📋 Lista Modelli (5 elementi trovati) [🔄][➕ Crea nuovo] ││
│  ├────────────────────────────────────────────────────────────┤│
│  │ Nome      │ Marchio │ Descrizione │ MPN     │ Data │ Azioni││
│  ├────────────────────────────────────────────────────────────┤│
│  │ Galaxy S23│ Samsung │ Flagship... │ SM-S911 │ 2024 │ 👁️✏️🗑️││
│  │ OLED55C2  │ LG      │ 55" OLED... │ OLED55  │ 2024 │ 👁️✏️🗑️││
│  │ WAW28640  │ Bosch   │ Washing...  │ WAW28   │ 2023 │ 👁️✏️🗑️││
│  │ HRF1984   │ Bosch   │ Refriger... │ HRF1984 │ 2023 │ 👁️✏️🗑️││
│  │ A71       │ Samsung │ Mid-range...│ SM-A715 │ 2023 │ 👁️✏️🗑️││
│  └────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────┘
```

### Model Drawer (Create Mode)
```
                                    ┌──────────────────────────┐
                                    │ ➕ Crea Nuovo Modello    │
                                    ├──────────────────────────┤
                                    │                          │
                                    │ Marchio *                │
                                    │ [Samsung            ▾]   │
                                    │ • Samsung                │
                                    │ • LG                     │
                                    │ • Bosch                  │
                                    │ Seleziona il marchio...  │
                                    │                          │
                                    │ Nome Modello *           │
                                    │ [                      ] │
                                    │ Inserisci il nome...     │
                                    │                          │
                                    │ Descrizione              │
                                    │ [                      ] │
                                    │ [                      ] │
                                    │ [                      ] │
                                    │ Descrizione opzionale... │
                                    │                          │
                                    │ Codice Parte (MPN)       │
                                    │ [                      ] │
                                    │ Numero di parte...       │
                                    │                          │
                                    ├──────────────────────────┤
                                    │ [Chiudi]        [Salva] │
                                    └──────────────────────────┘
```

## Navigation Menu

The new menu items appear in the Administration section:

```
┌────────────────────────────────┐
│ 🏢 Amministrazione             │
├────────────────────────────────┤
│  📊 Dashboard Admin            │
│  📦 Gestione Lotti             │
│  🖨️ Gestione Stampanti          │
│  💶 Gestione Aliquote IVA      │
│  🏪 Gestione Magazzini         │
│  👥 Gestione Fornitori         │
│  👤 Gestione Clienti           │
│  🌳 Gestione Classificazione   │
│  📏 Gestione Unità di Misura   │
│  🏷️ Gestione Marchi           │  ⬅️ NEW!
│  📦 Gestione Modelli           │  ⬅️ NEW!
└────────────────────────────────┘
```

## Features Implemented

### Brand Management
✅ Create new brands
✅ Edit existing brands
✅ View brand details (read-only)
✅ Delete brands (with confirmation)
✅ Search brands by name, description, country
✅ Sort by name, creation date
✅ Responsive design

### Model Management
✅ Create new models with brand selection
✅ Edit existing models
✅ View model details (read-only)
✅ Delete models (with confirmation)
✅ Search models by name, brand, description, MPN
✅ Brand autocomplete in drawer
✅ Sort by name, brand, creation date
✅ Responsive design

### Technical Features
✅ Three-mode drawers (Create/Edit/View)
✅ Form validation
✅ Success/error notifications
✅ Loading states
✅ Italian translations
✅ Consistent UI patterns
✅ RESTful API integration

## Translation Keys Added

All Italian translations for:
- Navigation items
- Page titles and descriptions
- Field labels and placeholders
- Helper texts
- Error messages
- Success messages
- Confirmation dialogs
- Button labels

## API Endpoints Used

Brand:
- GET    /api/v1/product-management/brands (list)
- GET    /api/v1/product-management/brands/{id} (details)
- POST   /api/v1/product-management/brands (create)
- PUT    /api/v1/product-management/brands/{id} (update)
- DELETE /api/v1/product-management/brands/{id} (delete)

Model:
- GET    /api/v1/product-management/models (list)
- GET    /api/v1/product-management/models/{id} (details)
- POST   /api/v1/product-management/models (create)
- PUT    /api/v1/product-management/models/{id} (update)
- DELETE /api/v1/product-management/models/{id} (delete)
