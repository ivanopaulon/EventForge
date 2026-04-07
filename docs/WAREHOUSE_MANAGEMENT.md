# Warehouse and Storage Location Management Implementation

## Overview
This implementation adds warehouse (storage facility) and storage location management functionality to EventForge, following the same pattern used for VAT rate management.

## Components Created

### 1. StorageFacilityDrawer Component
**Location:** `EventForge.Client/Shared/Components/StorageFacilityDrawer.razor`

A reusable drawer component for creating, viewing, and editing warehouses (storage facilities). Features:
- Full CRUD operations (Create, Read, Update, Delete)
- Multi-modal support (Create, Edit, View modes)
- Form validation
- Localized labels and messages
- Status indicators (Fiscal, Refrigerated)
- Notes field for additional information

**Fields:**
- Name (required): Name of the warehouse
- Code (required): Unique code for the warehouse (max 30 characters)
- Address (optional): Physical address
- Phone (optional): Contact phone number
- Email (optional): Contact email
- Manager (optional): Warehouse manager or responsible person
- Area (optional): Total area in square meters
- Capacity (optional): Maximum storage capacity
- Is Fiscal (checkbox): Indicates if the warehouse is fiscal
- Is Refrigerated (checkbox): Indicates if the warehouse is refrigerated
- Notes (optional): Additional notes

### 2. WarehouseManagement Page
**Location:** `EventForge.Client/Pages/Management/WarehouseManagement.razor`
**Route:** `/warehouse/facilities`

A full-featured management page for warehouses with:
- Data table with sortable columns
- Search functionality by name, code, or address
- Filter switches (Fiscal, Refrigerated)
- Action buttons (View, Edit, Delete)
- Refresh button
- Create new warehouse button
- Responsive design
- Display of warehouse properties (fiscal, refrigerated)
- Location count display (total and active)

### 3. StorageLocationDrawer Component
**Location:** `EventForge.Client/Shared/Components/StorageLocationDrawer.razor`

A reusable drawer component for creating, viewing, and editing storage locations. Features:
- Full CRUD operations (Create, Read, Update, Delete)
- Multi-modal support (Create, Edit, View modes)
- Form validation
- Localized labels and messages
- Warehouse selection dropdown
- Position tracking (Row, Column, Level)
- Status management (Active, Inactive, Refrigerated)
- Capacity and occupancy tracking

**Fields:**
- Code (required): Unique code for the location (max 30 characters)
- Warehouse (required): Parent warehouse selection
- Description (optional): Description of the location
- Zone (optional): Zone or area within warehouse
- Floor (optional): Floor or level
- Row (optional): Row identifier
- Column (optional): Column identifier
- Level (optional): Level identifier
- Capacity (optional): Maximum capacity
- Occupancy (optional): Current occupancy
- Is Refrigerated (checkbox): Indicates if location is refrigerated
- Is Active (checkbox): Indicates if location is active
- Notes (optional): Additional notes

### 4. StorageLocationManagement Page
**Location:** `EventForge.Client/Pages/Management/StorageLocationManagement.razor`
**Route:** `/warehouse/locations`

A full-featured management page for storage locations with:
- Data table with sortable columns
- Search functionality by code or description
- Filter by warehouse dropdown
- Filter switches (Active, Refrigerated)
- Action buttons (View, Edit, Delete)
- Refresh button
- Create new location button
- Responsive design
- Visual occupancy display with progress bars
- Position display (Row, Column, Level)

### 5. Navigation Integration
**Location:** `EventForge.Client/Layout/NavMenu.razor`

Added to the Administration section of the navigation menu:
- **Warehouse Management:**
  - Icon: Warehouse symbol
  - Translation key: `nav.warehouseManagement`
  - Route: `/warehouse/facilities`
- **Storage Location Management:**
  - Icon: Location pin symbol
  - Translation key: `nav.storageLocationManagement`
  - Route: `/warehouse/locations`

## Backend API Endpoints

The existing `WarehouseManagementController` already provides all necessary endpoints:

### Storage Facilities (Warehouses)
- `GET /api/v1/warehouse/facilities` - Get paginated list
- `GET /api/v1/warehouse/facilities/{id}` - Get single facility
- `POST /api/v1/warehouse/facilities` - Create facility
- `PUT /api/v1/warehouse/facilities/{id}` - Update facility
- `DELETE /api/v1/warehouse/facilities/{id}` - Delete facility

### Storage Locations
- `GET /api/v1/warehouse/locations` - Get paginated list
- `GET /api/v1/warehouse/locations/{id}` - Get single location
- `POST /api/v1/warehouse/locations` - Create location
- `PUT /api/v1/warehouse/locations/{id}` - Update location
- `DELETE /api/v1/warehouse/locations/{id}` - Delete location

## DTOs Used

### Storage Facility DTOs
- `StorageFacilityDto`: Output DTO for display
- `CreateStorageFacilityDto`: Input DTO for creation
- `UpdateStorageFacilityDto`: Input DTO for updates

### Storage Location DTOs
- `StorageLocationDto`: Output DTO for display
- `CreateStorageLocationDto`: Input DTO for creation
- `UpdateStorageLocationDto`: Input DTO for updates

## Translations

All Italian translations have been added to `EventForge.Client/wwwroot/i18n/it.json`:

### Navigation
- `nav.warehouseManagement`: "Gestione Magazzini"
- `nav.storageLocationManagement`: "Gestione Ubicazioni"

### Warehouse Page Translations
- `warehouse.facilityManagement`: "Gestione Magazzini"
- `warehouse.facilityManagementDescription`: "Gestisci i magazzini della tua organizzazione"
- `warehouse.searchFacilities`: "Cerca magazzini"
- `warehouse.searchPlaceholder`: "Inserisci nome o codice..."
- `warehouse.onlyFiscal`: "Solo fiscali"
- `warehouse.onlyRefrigerated`: "Solo refrigerati"
- `warehouse.facilityList`: "Lista Magazzini"
- `warehouse.itemsFound`: "elementi trovati"
- `warehouse.createNewFacility`: "Crea nuovo magazzino"
- `warehouse.noFacilitiesFound`: "Nessun magazzino trovato"
- `warehouse.noFacilitiesMatchFilters`: "Nessun magazzino corrisponde ai filtri applicati"
- `warehouse.clearFilters`: "Cancella filtri"
- `warehouse.confirmFacilityDelete`: "Sei sicuro di voler eliminare il magazzino '{0}'? Questa azione non può essere annullata."
- `warehouse.facilityDeleted`: "Magazzino eliminato con successo!"
- `warehouse.deleteFacilityError`: "Errore nell'eliminazione del magazzino"
- `warehouse.loadingPageError`: "Errore nel caricamento della pagina: {0}"
- `warehouse.loadingFacilitiesError`: "Errore nel caricamento dei magazzini: {0}"

### Storage Location Page Translations
- `warehouse.locationManagement`: "Gestione Ubicazioni"
- `warehouse.locationManagementDescription`: "Gestisci le ubicazioni nei magazzini"
- `warehouse.searchLocations`: "Cerca ubicazioni"
- `warehouse.searchLocationPlaceholder`: "Inserisci codice..."
- `warehouse.filterByWarehouse`: "Filtra per magazzino"
- `warehouse.onlyActive`: "Solo attive"
- `warehouse.locationList`: "Lista Ubicazioni"
- `warehouse.createNewLocation`: "Crea nuova ubicazione"
- `warehouse.noLocationsFound`: "Nessuna ubicazione trovata"
- `warehouse.noLocationsMatchFilters`: "Nessuna ubicazione corrisponde ai filtri applicati"
- `warehouse.confirmLocationDelete`: "Sei sicuro di voler eliminare l'ubicazione '{0}'? Questa azione non può essere annullata."
- `warehouse.locationDeleted`: "Ubicazione eliminata con successo!"
- `warehouse.deleteLocationError`: "Errore nell'eliminazione dell'ubicazione"
- `warehouse.loadingLocationsError`: "Errore nel caricamento delle ubicazioni: {0}"

### Drawer Field Translations
- `drawer.field.nomeMagazzino`: "Nome Magazzino"
- `drawer.field.codiceMagazzino`: "Codice Magazzino"
- `drawer.field.indirizzo`: "Indirizzo"
- `drawer.field.telefono`: "Telefono"
- `drawer.field.email`: "Email"
- `drawer.field.responsabile`: "Responsabile"
- `drawer.field.superficieMq`: "Superficie (m²)"
- `drawer.field.capacitaMassima`: "Capacità Massima"
- `drawer.field.magazzinoFiscale`: "Magazzino Fiscale"
- `drawer.field.magazzinoRefrigerato`: "Refrigerato"
- `drawer.field.idMagazzino`: "ID Magazzino"
- `drawer.field.totaleUbicazioni`: "Totale Ubicazioni"
- `drawer.field.ubicazioniAttive`: "Ubicazioni Attive"
- `drawer.field.codiceUbicazione`: "Codice Ubicazione"
- `drawer.field.magazzino`: "Magazzino"
- `drawer.field.zona`: "Zona"
- `drawer.field.piano`: "Piano"
- `drawer.field.fila`: "Fila"
- `drawer.field.colonna`: "Colonna"
- `drawer.field.livello`: "Livello"
- `drawer.field.capacita`: "Capacità"
- `drawer.field.occupazione`: "Occupazione"
- `drawer.field.ubicazioneRefrigerata`: "Refrigerata"
- `drawer.field.attiva`: "Attiva"
- `drawer.field.idUbicazione`: "ID Ubicazione"
- `drawer.field.storageFacility`: "Magazzino"
- `drawer.field.storageLocation`: "Ubicazione"

### Drawer Helper Text Translations
- `drawer.helperText.nomeMagazzino`: "Inserisci il nome del magazzino"
- `drawer.helperText.codiceMagazzino`: "Codice univoco del magazzino"
- `drawer.helperText.indirizzo`: "Indirizzo fisico del magazzino"
- `drawer.helperText.telefono`: "Numero di telefono del magazzino"
- `drawer.helperText.email`: "Indirizzo email del magazzino"
- `drawer.helperText.responsabile`: "Responsabile o manager del magazzino"
- `drawer.helperText.superficieMq`: "Superficie totale in metri quadrati"
- `drawer.helperText.capacitaMassima`: "Capacità massima di stoccaggio"
- `drawer.helperText.magazzinoFiscale`: "Indica se il magazzino è fiscale"
- `drawer.helperText.magazzinoRefrigerato`: "Indica se il magazzino è refrigerato"
- `drawer.helperText.noteMagazzino`: "Note aggiuntive sul magazzino"
- `drawer.helperText.codiceUbicazione`: "Codice univoco dell'ubicazione nel magazzino"
- `drawer.helperText.magazzino`: "Seleziona il magazzino di appartenenza"
- `drawer.helperText.descrizioneUbicazione`: "Descrizione dell'ubicazione"
- `drawer.helperText.zona`: "Zona o area nel magazzino"
- `drawer.helperText.piano`: "Piano o livello"
- `drawer.helperText.fila`: "Identificativo fila"
- `drawer.helperText.colonna`: "Identificativo colonna"
- `drawer.helperText.livello`: "Identificativo livello"
- `drawer.helperText.capacita`: "Capacità massima dell'ubicazione"
- `drawer.helperText.occupazione`: "Occupazione attuale dell'ubicazione"
- `drawer.helperText.ubicazioneRefrigerata`: "Indica se l'ubicazione è refrigerata"
- `drawer.helperText.ubicazioneAttiva`: "Indica se l'ubicazione è attiva"
- `drawer.helperText.noteUbicazione`: "Note aggiuntive sull'ubicazione"

### Drawer Status Translations
- `drawer.status.magazzinoFiscale`: "Fiscale"
- `drawer.status.magazzinoNonFiscale`: "Non Fiscale"
- `drawer.status.refrigerato`: "Refrigerato"
- `drawer.status.nonRefrigerato`: "Non Refrigerato"
- `drawer.status.attiva`: "Attiva"
- `drawer.status.nonAttiva`: "Non Attiva"

### Drawer Title Translations
- `drawer.title.modificaMagazzino`: "Modifica Magazzino: {0}"
- `drawer.title.visualizzaMagazzino`: "Visualizza Magazzino: {0}"
- `drawer.title.modificaUbicazione`: "Modifica Ubicazione: {0}"
- `drawer.title.visualizzaUbicazione`: "Visualizza Ubicazione: {0}"

### General Field Translations
- `field.code`: "Codice"
- `field.warehouse`: "Magazzino"
- `field.zone`: "Zona"
- `field.position`: "Posizione"
- `field.occupancy`: "Occupazione"
- `field.properties`: "Proprietà"
- `field.locations`: "Ubicazioni"
- `field.storageFacility`: "Magazzino"
- `field.storageLocation`: "Ubicazione"

## Service Integration

### Client-Side Services
New services have been created:
- `IWarehouseService` and `WarehouseService`: Provides client-side warehouse management operations
- Extended `IStorageLocationService` and `StorageLocationService`: Enhanced with full CRUD operations

### Server-Side Services
The existing services are used:
- `IStorageFacilityService` and `StorageFacilityService`: Backend warehouse management
- `IStorageLocationService` and `StorageLocationService`: Backend storage location management

Both services are already registered and provide:
- `GetStorageFacilitiesAsync()` / `GetStorageLocationsAsync()`
- `GetStorageFacilityAsync(Guid id)` / `GetStorageLocationAsync(Guid id)`
- `CreateStorageFacilityAsync(CreateStorageFacilityDto)` / `CreateStorageLocationAsync(CreateStorageLocationDto)`
- `UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto)` / `UpdateStorageLocationAsync(Guid id, UpdateStorageLocationDto)`
- `DeleteStorageFacilityAsync(Guid id)` / `DeleteStorageLocationAsync(Guid id)`

## Access Control

The warehouse and storage location management pages are accessible to:
- Super administrators
- Administrators
- Managers

Access is controlled through the `@attribute [Authorize]` directive and the navigation menu visibility logic. Additionally, the backend requires the `ProductManagement` license feature.

## Usage

### Warehouse Management
1. Navigate to the Administration section in the navigation menu
2. Click on "Gestione Magazzini" (Warehouse Management)
3. Use the search and filters to find specific warehouses
4. Click "Create" to add a new warehouse
5. Click action buttons (View/Edit/Delete) to manage existing warehouses

### Storage Location Management
1. Navigate to the Administration section in the navigation menu
2. Click on "Gestione Ubicazioni" (Storage Location Management)
3. Use the search and warehouse filter to find specific locations
4. Click "Create" to add a new storage location (requires selecting a warehouse)
5. Click action buttons (View/Edit/Delete) to manage existing locations

## Features

### Warehouse Management Features
- Create, view, edit, and delete warehouses
- Search by name, code, or address
- Filter by fiscal status
- Filter by refrigeration status
- View warehouse statistics (total locations, active locations)
- Track warehouse properties (area, capacity, manager)

### Storage Location Management Features
- Create, view, edit, and delete storage locations
- Search by code or description
- Filter by parent warehouse
- Filter by active status
- Filter by refrigeration status
- Visual occupancy tracking with progress bars
- Position tracking (row, column, level, zone, floor)
- Capacity and occupancy management

## Notes

- All warehouses and storage locations are tenant-scoped (multi-tenant support)
- The implementation follows the same patterns as VAT Rate Management for consistency
- The components are fully localized with Italian translations
- The UI is responsive and follows the EventForge design patterns
- All CRUD operations include proper error handling and user feedback via Snackbar
- The storage location drawer loads available warehouses from the warehouse service
- Occupancy is displayed with color-coded progress bars (green < 50%, blue < 75%, orange < 90%, red >= 90%)
