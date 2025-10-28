# Document Counter Management Implementation - Summary

## Overview
This implementation completes the integration of the automatic document numbering system introduced in PR #509. It adds the necessary UI components and fixes critical bugs in the document creation flow.

## Problem Statement
After PR #509 introduced document counter functionality on the backend, the following issues needed to be addressed:

1. **Missing UI for Counter Management**: No pages existed to create, view, update, or delete document counters
2. **Document Creation Bug**: Business parties (clients/suppliers) were not appearing in the autocomplete after selecting a document type
3. **Manual Number Required**: The document number field was still required, preventing automatic generation

## Implementation Details

### 1. Client Services (EventForge.Client/Services)

#### IDocumentCounterService.cs
- Interface defining CRUD operations for document counters
- Methods: GetAll, GetByType, GetById, Create, Update, Delete

#### DocumentCounterService.cs
- Full implementation of IDocumentCounterService
- Communicates with backend API at `api/v1/DocumentCounters`
- Proper error handling and logging

### 2. UI Components

#### DocumentCounterManagement.razor
A complete management page featuring:
- **List View**: Table showing all counters with filtering by document type
- **Create/Edit**: Dialog-based interface for managing counters
- **Delete**: Confirmation dialog with soft-delete functionality
- **Filtering**: Filter counters by document type
- **Authorization**: Restricted to SuperAdmin, Admin, and Manager roles

Key Features:
```razor
- Document type selection
- Series configuration (e.g., "A", "B", "2025")
- Year-based counters with optional year reset
- Customizable prefix and padding length
- Format patterns supporting placeholders: {PREFIX}, {SERIES}, {YEAR}, {NUMBER}
- Current value tracking
- Notes for documentation
```

#### DocumentCounterDialog.razor
A reusable dialog component for creating/editing counters:
- **Create Mode**: Full form for new counter configuration
- **Edit Mode**: Allows updating current value, prefix, padding, pattern, and notes
- **Validation**: Required fields and range validation
- **Helper Text**: Contextual help for each field
- **Pattern Preview**: Shows available placeholders

### 3. Bug Fixes

#### GenericDocumentProcedure.razor
**Issue 1: Number Field Required**
```razor
BEFORE:
<MudTextField T="string" ... @bind-Value="_model.Number" Required="true" />

AFTER:
<MudTextField T="string" ... 
              @bind-Value="_model.Number" 
              Required="false"
              Placeholder="Auto-generato"
              HelperText="Lascia vuoto per generazione automatica" />
```

**Issue 2: Business Party Autocomplete Not Working**
```csharp
BEFORE:
- No minimum search term length
- Used only 50 results
- Filtering logic present but ineffective

AFTER:
private async Task<IEnumerable<BusinessPartyDto>> SearchBusinessPartiesAsync(...)
{
    // Require minimum 2 characters for better UX
    if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        return Array.Empty<BusinessPartyDto>();
    
    // Get up to 100 results
    var result = await BusinessPartyService.GetBusinessPartiesAsync(1, 100);
    
    // Filter by search term
    var filtered = result.Items.Where(bp => 
        (bp.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (bp.TaxCode?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
    );

    // Apply document type filtering
    if (selectedDocType != null && selectedDocType.RequiredPartyType != BusinessPartyType.Both)
    {
        filtered = filtered.Where(bp => 
            bp.PartyType == selectedDocType.RequiredPartyType || 
            bp.PartyType == BusinessPartyType.Both
        );
    }
    
    return filtered;
}
```

**Issue 3: CanSaveHeader() Validation**
```csharp
BEFORE:
private bool CanSaveHeader()
{
    return _model.DocumentTypeId != Guid.Empty
           && !string.IsNullOrWhiteSpace(_model.Number)  // ← Required
           && _selectedBusinessParty != null
           && _documentDate.HasValue;
}

AFTER:
private bool CanSaveHeader()
{
    return _model.DocumentTypeId != Guid.Empty
           && _selectedBusinessParty != null
           && _documentDate.HasValue;
}
```

#### DocumentTypeDetail.razor
Added a new section to link to counter management:
```razor
@if (!_isCreateMode && _documentType != null)
{
    <MudPaper Elevation="2" Class="pa-4 mt-4">
        <div class="d-flex justify-space-between align-center mb-4">
            <MudText Typo="Typo.h6">
                <MudIcon Icon="@Icons.Material.Outlined.Pin" Class="mr-2" />
                Contatori Numerazione
            </MudText>
            <MudButton ... OnClick="Navigate to /documents/counters">
                Gestisci Contatori
            </MudButton>
        </div>
    </MudPaper>
}
```

### 4. Service Registration

#### Program.cs
```csharp
// Add document management services
builder.Services.AddScoped<IDocumentHeaderService, DocumentHeaderService>();
builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
builder.Services.AddScoped<IDocumentCounterService, DocumentCounterService>();  // ← NEW
```

## Testing Results

All 14 existing document counter tests pass successfully:

```
✓ DocumentCounterIntegrationTests (3 tests)
  - CreateDocumentHeader_WithoutNumber_AutoGeneratesNumber
  - MultipleDocuments_SameSeries_GeneratesSequentialNumbers
  - DifferentSeries_MaintainSeparateCounters

✓ DocumentCounterServiceTests (11 tests)
  - CreateAsync_WithValidData_CreatesCounter
  - CreateAsync_WithDuplicateCounter_ThrowsException
  - GenerateDocumentNumberAsync_WithNewCounter_CreatesAndIncrements
  - GenerateDocumentNumberAsync_WithExistingCounter_Increments
  - GenerateDocumentNumberAsync_WithFormatPattern_UsesPattern
  - GenerateDocumentNumberAsync_WithDefaultFormat_UsesDefaultPattern
  - GenerateDocumentNumberAsync_ConcurrentCalls_GeneratesUniqueNumbers
  - GenerateDocumentNumberAsync_YearChange_ResetsCounter
  - GetByDocumentTypeAsync_ReturnsAllCountersForType
  - UpdateAsync_WithValidData_UpdatesCounter
  - DeleteAsync_WithValidId_SoftDeletesCounter

Total: 14/14 passed (100%)
```

## User Guide

### Creating a Document Counter

1. Navigate to **Documents → Counter Management** (`/documents/counters`)
2. Click **"Nuovo Contatore"** (New Counter)
3. Fill in the form:
   - **Document Type**: Select the document type (e.g., "Invoice")
   - **Series**: Enter a series identifier (e.g., "A", "B", or "2025")
   - **Year**: (Optional) Specify a year for year-specific counters
   - **Prefix**: (Optional) Add a prefix to all numbers (e.g., "INV")
   - **Padding Length**: Number of digits for zero-padding (default: 5)
   - **Format Pattern**: (Optional) Custom pattern using {PREFIX}, {SERIES}, {YEAR}, {NUMBER}
   - **Reset on Year Change**: Enable to reset counter to 0 each year
   - **Notes**: Add any documentation
4. Click **"Salva"** (Save)

### Example Counter Configurations

**Simple Sequential Counter**
- Series: "A"
- Year: 2025
- Padding: 5
- Reset on Year Change: Yes
- **Result**: A/2025/00001, A/2025/00002, ...

**Custom Pattern Counter**
- Series: "B"
- Year: 2025
- Prefix: "INV"
- Format Pattern: `{PREFIX}-{SERIES}/{YEAR}/{NUMBER}`
- Padding: 4
- **Result**: INV-B/2025/0001, INV-B/2025/0002, ...

**General Counter (No Year)**
- Series: "GEN"
- Year: (empty)
- Prefix: "DOC"
- Reset on Year Change: No
- **Result**: DOC/GEN/00001, DOC/GEN/00002, ... (never resets)

### Creating a Document with Auto-Numbering

1. Navigate to **Documents → Create New** (`/documents/create`)
2. Select **Document Type** from dropdown
3. Enter **Series** (optional, uses "" if empty)
4. **Leave Number field empty** - it will be auto-generated
5. Select **Business Party** from autocomplete (type at least 2 characters)
   - The list will be filtered based on the document type's required party type
6. Fill in other required fields
7. Click **"Salva"** (Save)
8. The system will automatically generate the document number based on the counter

### Backward Compatibility

If you want to manually specify a document number:
1. Simply enter the number in the Number field
2. The auto-generation will be skipped
3. The counter will not be incremented

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                       Client Layer                           │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  DocumentCounterManagement.razor                             │
│  ├─ DocumentCounterDialog.razor (Create/Edit)                │
│  └─ IDocumentCounterService → DocumentCounterService         │
│                                                               │
│  GenericDocumentProcedure.razor                              │
│  ├─ Auto-complete for Business Parties                       │
│  └─ Optional Number field                                    │
│                                                               │
│  DocumentTypeDetail.razor                                    │
│  └─ Link to Counter Management                               │
│                                                               │
└─────────────────────────────────────────────────────────────┘
                            ↓ HTTP
┌─────────────────────────────────────────────────────────────┐
│                       Server Layer (PR #509)                 │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  DocumentCountersController                                  │
│  └─ /api/v1/DocumentCounters/*                              │
│                                                               │
│  IDocumentCounterService → DocumentCounterService            │
│  ├─ CreateAsync()                                            │
│  ├─ UpdateAsync()                                            │
│  ├─ DeleteAsync()                                            │
│  └─ GenerateDocumentNumberAsync()                            │
│                                                               │
│  DocumentHeaderService                                       │
│  └─ CreateDocumentHeaderAsync()                              │
│      └─ Auto-generates number if not provided                │
│                                                               │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                       Data Layer                             │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  DocumentCounter Entity                                      │
│  ├─ DocumentTypeId (FK)                                      │
│  ├─ Series                                                   │
│  ├─ Year (nullable)                                          │
│  ├─ CurrentValue                                             │
│  ├─ Prefix                                                   │
│  ├─ PaddingLength                                            │
│  ├─ FormatPattern                                            │
│  ├─ ResetOnYearChange                                        │
│  └─ Unique constraint: (DocumentTypeId, Series, Year)        │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Security Considerations

1. **Authorization**: Counter management is restricted to SuperAdmin, Admin, and Manager roles
2. **Tenant Isolation**: All operations respect tenant context from ITenantContext
3. **Soft Delete**: Counters are soft-deleted (IsDeleted flag) for audit trail
4. **Audit Logging**: All CRUD operations are logged via IAuditLogService
5. **Transaction Safety**: Number generation uses transactions to prevent duplicates
6. **Validation**: Server-side validation prevents duplicate counters

## Files Modified/Created

### Created Files (7)
1. `EventForge.Client/Services/IDocumentCounterService.cs`
2. `EventForge.Client/Services/DocumentCounterService.cs`
3. `EventForge.Client/Pages/Management/Documents/DocumentCounterManagement.razor`
4. `EventForge.Client/Shared/Components/Dialogs/Documents/DocumentCounterDialog.razor`

### Modified Files (3)
1. `EventForge.Client/Program.cs` - Service registration
2. `EventForge.Client/Pages/Management/Documents/GenericDocumentProcedure.razor` - Bug fixes
3. `EventForge.Client/Pages/Management/Documents/DocumentTypeDetail.razor` - Counter section

## Next Steps (Optional Enhancements)

1. Add navigation menu entry for `/documents/counters`
2. Add bulk counter creation for multiple series
3. Add counter preview/simulation feature
4. Add export/import functionality for counter configurations
5. Add dashboard widget showing counter statistics
6. Add counter history/audit trail visualization
7. Add validation to prevent counter value decrease (unless explicitly allowed)

## Conclusion

This implementation successfully integrates the document counter system from PR #509 with the client application, providing a complete user interface for managing automatic document numbering. All critical bugs have been fixed, and the system maintains backward compatibility with manual numbering when needed.
