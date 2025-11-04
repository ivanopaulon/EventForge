# Document Row Table and Dialog Improvements - Implementation Summary

## Overview
This document summarizes the improvements made to the document row management table and dialog in the EventForge application, as requested in the issue.

## Changes Implemented

### 1. DTOs Enhancement

#### Files Modified:
- `EventForge.DTOs/Documents/DocumentRowDto.cs`
- `EventForge.DTOs/Documents/CreateDocumentRowDto.cs`
- `EventForge.DTOs/Documents/UpdateDocumentRowDto.cs`

#### Changes:
- Added `UnitOfMeasureId` (Guid?) property to all three DTOs
- This allows proper tracking of unit of measure references instead of just text

### 2. Client-Side: GenericDocumentProcedure.razor

#### File Modified:
- `EventForge.Client/Pages/Management/Documents/GenericDocumentProcedure.razor`

#### Major Improvements:

##### Table Enhancements:
- **Multi-Selection**: Added `MultiSelection="true"` and `@bind-SelectedItems="_selectedRows"`
- **Fixed Header**: Added `FixedHeader="true"` with `Height="calc(100vh - 400px)"` for better scrolling
- **Unit of Measure Column**: Added new column to display unit of measure
- **Action Buttons**: Replaced simple delete icon with `ActionButtonGroup` component showing Edit and Delete buttons
- **Selection Management**: Added `HashSet<DocumentRowDto> _selectedRows` for tracking selected items

##### Toolbar Features:
- Integrated `ManagementTableToolbar` component with:
  - Selection badge showing count of selected rows
  - Create button for adding new rows
  - Delete button for batch deletion
  - Custom "Merge Rows" button (visible when 2+ rows of same product selected)

##### New Functions Implemented:

**EditRow(Guid rowId)**
```csharp
- Opens AddDocumentRowDialog in edit mode
- Passes RowId parameter to load existing row data
- Reloads document after dialog closes
```

**DeleteSelectedRowsAsync()**
```csharp
- Shows confirmation dialog with count
- Deletes all selected rows
- Clears selection after successful deletion
- Shows success/error messages
```

**MergeSelectedRowsAsync()**
```csharp
- Validates rows can be merged (same product, same UoM)
- Shows detailed confirmation with quantities and prices
- Sums quantities from all selected rows
- Uses maximum unit price among selected rows
- Concatenates notes separated by " | "
- Updates main (oldest) row and deletes others
- Logs all operations for audit trail
```

**CanMergeSelectedRows()**
```csharp
- Validates at least 2 rows selected
- Checks all rows have same ProductId (not null)
- Verifies all rows have same UnitOfMeasure
- Returns false if any validation fails
```

### 3. Client-Side: AddDocumentRowDialog.razor

#### File Modified:
- `EventForge.Client/Shared/Components/Dialogs/Documents/AddDocumentRowDialog.razor`

#### Major Improvements:

##### Edit Mode Support:
- Added `[Parameter] public Guid? RowId { get; set; }`
- Added `_isEditMode` computed property
- Dynamic dialog title and button text based on mode
- `LoadRowForEdit(Guid rowId)` method to populate form with existing data

##### Unit of Measure Selection:
**Replaced** text field with `MudSelect<Guid?>`:
```razor
<MudSelect T="Guid?" @bind-Value="_selectedUnitOfMeasureId">
  <!-- Shows product units if available -->
  <!-- Falls back to all active units if no product units -->
</MudSelect>
```

##### New Properties:
- `_availableUnits` - List of units specific to selected product
- `_allUnitsOfMeasure` - All available units in system
- `_selectedUnitOfMeasureId` - Currently selected unit

##### Enhanced PopulateFromProduct:
```csharp
- Loads product units via ProductService.GetProductUnitsAsync()
- Selects default unit (Base type or first available)
- Falls back to product's default UnitOfMeasureId if no units configured
- Shows informative alert if no units configured
- Sets both UnitOfMeasure text and UnitOfMeasureId
```

##### SaveAndContinue Updates:
- Checks `_isEditMode` to determine create vs update
- For edit: Calls `UpdateDocumentRowAsync()` and closes dialog
- For create: Calls `AddDocumentRowAsync()` and resets form for next entry
- Updates unit of measure fields from selected dropdown value

### 4. Server-Side: Entity Updates

#### File Modified:
- `EventForge.Server/Data/Entities/Documents/DocumentRow.cs`

#### Changes:
- Added `UnitOfMeasureId` (Guid?) property
- Added navigation property `UnitOfMeasureEntity` (type: `UM`)

### 5. Server-Side: Mapping Extensions

#### File Modified:
- `EventForge.Server/Extensions/MappingExtensions.cs`

#### Changes:
- Updated `ToDto()` extension to include `UnitOfMeasureId`
- Updated `ToEntity()` extension to include `UnitOfMeasureId`

### 6. Server-Side: Service Implementation

#### File Modified:
- `EventForge.Server/Services/Documents/DocumentHeaderService.cs`

#### New Methods Added:

**UpdateDocumentRowAsync()**
```csharp
- Finds existing row by ID
- Updates all properties from UpdateDocumentRowDto
- Includes UnitOfMeasureId in update
- Tracks changes in audit log
- Returns updated DTO or null if not found
```

**DeleteDocumentRowAsync()**
```csharp
- Finds existing row by ID
- Performs soft delete (sets IsDeleted = true)
- Updates ModifiedAt timestamp
- Tracks deletion in audit log
- Returns true/false based on success
```

### 7. Server-Side: Service Interface

#### File Modified:
- `EventForge.Server/Services/Documents/IDocumentHeaderService.cs`

#### Changes:
- Added `UpdateDocumentRowAsync` method signature
- Added `DeleteDocumentRowAsync` method signature

### 8. Server-Side: API Controller

#### File Modified:
- `EventForge.Server/Controllers/DocumentsController.cs`

#### New Endpoints Added:

**PUT /api/v1/documents/rows/{rowId}**
```csharp
[HttpPut("rows/{rowId:guid}")]
- Updates existing document row
- Validates model state and tenant access
- Returns 200 OK with updated row
- Returns 404 if row not found
- Returns 400 for validation errors
```

**DELETE /api/v1/documents/rows/{rowId}**
```csharp
[HttpDelete("rows/{rowId:guid}")]
- Deletes document row (soft delete)
- Validates tenant access
- Returns 204 No Content on success
- Returns 404 if row not found
```

## Technical Decisions

### Why HashSet for Selection?
- MudTable expects `ICollection<T>` for `SelectedItems`
- `HashSet<T>` provides O(1) lookup and prevents duplicates
- Perfect for tracking selected items

### Why Separate UnitOfMeasure and UnitOfMeasureId?
- `UnitOfMeasure` (string) for display and backward compatibility
- `UnitOfMeasureId` (Guid?) for proper foreign key relationship
- Allows easy migration without breaking existing data

### Why Merge Uses Oldest Row?
- Preserves creation timestamp and audit trail
- Maintains row order in document
- Updates quantity and keeps most relevant price

### Why Soft Delete?
- Maintains audit trail and data history
- Allows recovery if needed
- Consistent with rest of application

## Benefits

### User Experience:
1. **Faster Navigation**: Fixed header allows scrolling through many rows
2. **Bulk Operations**: Select multiple rows for deletion or merging
3. **Proper Unit Selection**: Dropdown prevents typos and ensures consistency
4. **Edit Capability**: Modify rows without deleting and re-creating
5. **Visual Consistency**: Matches ProductManagement table styling

### Data Quality:
1. **Referential Integrity**: UnitOfMeasureId links to actual units
2. **Validation**: Can only merge rows with same product and unit
3. **Audit Trail**: All changes tracked with user and timestamp
4. **No Duplicates**: Merge feature consolidates duplicate entries

### Developer Experience:
1. **Reusable Components**: Leverages existing ManagementTableToolbar and ActionButtonGroup
2. **Consistent Patterns**: Follows established service and controller patterns
3. **Type Safety**: Guid references instead of string comparisons
4. **Testable**: Methods are small and focused

## Migration Notes

### Database Schema:
A migration will be needed to add the `UnitOfMeasureId` column to the `DocumentRows` table:
```sql
ALTER TABLE DocumentRows 
ADD UnitOfMeasureId uniqueidentifier NULL;

-- Optional: Create foreign key constraint
ALTER TABLE DocumentRows
ADD CONSTRAINT FK_DocumentRows_UnitOfMeasure
FOREIGN KEY (UnitOfMeasureId) REFERENCES UnitOfMeasures(Id);
```

### Backward Compatibility:
- `UnitOfMeasureId` is nullable, so existing rows continue to work
- Existing `UnitOfMeasure` string field preserved
- New rows will populate both fields

## Testing Recommendations

### Unit Tests:
1. Test CanMergeSelectedRows with various scenarios
2. Test MergeSelectedRowsAsync calculations
3. Test UpdateDocumentRowAsync with various update scenarios
4. Test DeleteDocumentRowAsync with existing and non-existing IDs

### Integration Tests:
1. Create document, add rows, select multiple, delete batch
2. Create document, add duplicate product rows, merge them
3. Edit row, verify unit of measure selection works
4. Test with products that have no configured units

### Manual Testing Checklist:
- [ ] Create new document and add rows
- [ ] Edit existing row
- [ ] Delete single row
- [ ] Select multiple rows and batch delete
- [ ] Add duplicate product rows and merge them
- [ ] Try to merge rows with different units (should fail)
- [ ] Try to merge rows with different products (should fail)
- [ ] Verify unit of measure dropdown shows product units
- [ ] Verify unit of measure defaults to product's default unit
- [ ] Test with product that has no configured units
- [ ] Verify table scrolls correctly with many rows
- [ ] Verify selection badge updates correctly

## Future Enhancements

### Potential Improvements:
1. **Inline Editing**: Edit quantity/price directly in table
2. **Drag and Drop**: Reorder rows by dragging
3. **Export**: Export selected rows to Excel/CSV
4. **Templates**: Save common row combinations as templates
5. **Bulk Edit**: Edit multiple rows at once
6. **History**: View row change history
7. **Validation**: Warn if adding duplicate products
8. **Smart Merge**: Auto-detect and suggest rows to merge

## Conclusion

All requirements from the issue have been successfully implemented:
- ✅ Table follows ProductManagement standard layout
- ✅ Multi-selection with checkboxes
- ✅ Unit of measure column visible
- ✅ Edit and delete buttons for each row
- ✅ Toolbar with batch actions
- ✅ Merge functionality for duplicate products
- ✅ Unit of measure dropdown in dialog
- ✅ Edit mode support in dialog
- ✅ Server-side update and delete endpoints
- ✅ Proper validation and error handling

The implementation maintains consistency with the existing codebase while adding powerful new features for managing document rows.
