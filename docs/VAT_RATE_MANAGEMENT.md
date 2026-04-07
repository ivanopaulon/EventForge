# VAT Rate Management Implementation

## Overview
This implementation adds VAT rate management functionality to EventForge, following the same pattern used for tenant management.

## Components Created

### 1. VatRateDrawer Component
**Location:** `EventForge.Client/Shared/Components/VatRateDrawer.razor`

A reusable drawer component for creating, viewing, and editing VAT rates. Features:
- Full CRUD operations (Create, Read, Update, Delete)
- Multi-modal support (Create, Edit, View modes)
- Form validation
- Localized labels and messages
- Status management (Active, Suspended, Deleted)
- Date range support (ValidFrom, ValidTo)
- Notes field for additional information

**Fields:**
- Name (required): Name of the VAT rate (e.g., "IVA 22%")
- Percentage (required): Percentage value (0-100)
- Status (required): Active, Suspended, or Deleted
- Valid From (optional): Start date of validity
- Valid To (optional): End date of validity
- Notes (optional): Additional notes

### 2. VatRateManagement Page
**Location:** `EventForge.Client/Pages/Management/VatRateManagement.razor`
**Route:** `/financial/vat-rates`

A full-featured management page for VAT rates with:
- Data table with sortable columns
- Search functionality by name
- Status filtering (All, Active, Suspended, Deleted)
- Action buttons (View, Edit, Delete)
- Refresh button
- Create new VAT rate button
- Responsive design

### 3. Navigation Integration
**Location:** `EventForge.Client/Layout/NavMenu.razor`

Added to the Administration section of the navigation menu:
- Icon: Percent symbol
- Translation key: `nav.vatRateManagement`
- Available to admin users and managers

## Backend API Endpoints

The following endpoints are already implemented in `FinancialManagementController`:

- `GET /api/v1/financial/vat-rates` - List all VAT rates (with pagination support)
- `GET /api/v1/financial/vat-rates/{id}` - Get a specific VAT rate
- `POST /api/v1/financial/vat-rates` - Create a new VAT rate
- `PUT /api/v1/financial/vat-rates/{id}` - Update an existing VAT rate
- `DELETE /api/v1/financial/vat-rates/{id}` - Delete a VAT rate

## DTOs Used

- `VatRateDto` - For reading VAT rates
- `CreateVatRateDto` - For creating VAT rates
- `UpdateVatRateDto` - For updating VAT rates
- `VatRateStatus` enum - Active, Suspended, Deleted

## Translations

All Italian translations have been added to `EventForge.Client/wwwroot/i18n/it.json`:

### Navigation
- `nav.vatRateManagement`: "Gestione Aliquote IVA"

### Page Translations (financial section)
- `financial.vatRateManagement`: "Gestione Aliquote IVA"
- `financial.vatRateManagementDescription`: "Gestisci le aliquote IVA per la tua organizzazione"
- `financial.searchVatRates`: "Cerca aliquote IVA"
- `financial.vatRateList`: "Lista Aliquote IVA"
- And many more...

### Drawer Translations
- Field labels: `drawer.field.nomeAliquotaIva`, `drawer.field.percentualeAliquotaIva`, etc.
- Helper texts: `drawer.helperText.nomeAliquotaIva`, etc.
- Error messages: `drawer.error.nomeAliquotaIvaObbligatorio`
- Titles: `drawer.title.modificaAliquotaIva`, `drawer.title.visualizzaAliquotaIva`
- Status labels: `drawer.status.attivo`, `drawer.status.sospeso`, `drawer.status.eliminato`

## Service Integration

The existing `IFinancialService` and `FinancialService` already provide all necessary methods for VAT rate management:
- `GetVatRatesAsync()`
- `GetVatRateAsync(Guid id)`
- `CreateVatRateAsync(CreateVatRateDto)`
- `UpdateVatRateAsync(Guid id, UpdateVatRateDto)`
- `DeleteVatRateAsync(Guid id)`

## Access Control

The VAT rate management page is accessible to:
- Super administrators
- Administrators
- Managers

Access is controlled through the `@attribute [Authorize]` directive and the navigation menu visibility logic.

## Usage

1. Navigate to the Administration section in the navigation menu
2. Click on "Gestione Aliquote IVA" (VAT Rate Management)
3. Use the search and filters to find specific VAT rates
4. Click "Create" to add a new VAT rate
5. Click action buttons (View/Edit/Delete) to manage existing VAT rates

## Notes

- All VAT rates are tenant-scoped (multi-tenant support)
- The implementation follows the same patterns as TenantManagement and TenantDrawer for consistency
- The component is fully localized with Italian translations
- The UI is responsive and follows the EventForge design patterns
- All CRUD operations include proper error handling and user feedback via Snackbar
