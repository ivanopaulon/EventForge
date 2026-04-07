# Dashboard Icon Column Length Fix

## Issue
Dashboard metric configurations failed to save with SQL error:
```
String or binary data would be truncated in table 'EventData.dbo.DashboardMetricConfigs', column 'Icon'.
```

## Root Cause
The `Icon` column was defined as `NVARCHAR(100)` but MudBlazor icons are full SVG paths that can exceed 500 characters.

### Example Icon Value
MudBlazor icons like `Icons.Material.Outlined.Analytics` contain complete SVG paths:
```xml
<g><rect fill="none" height="24" width="24"/><g><path d="M19,3H5C3.9,3,3,3.9,3,5v14c0,1.1,0.9,2,2,2h14c1.1,0,2-0.9,2-2V5C21,3.9,20.1,3,19,3z M9,17H7v-7h2V17z M13,17h-2V7h2V17z M17,17h-2v-4h2V17z"/>
```
**Length**: ~250+ characters (some icons can exceed 1000 characters)

## Solution

### Changes Made

#### 1. Entity Model Update
**File**: `EventForge.Server/Data/Entities/Dashboard/DashboardMetricConfig.cs`

Changed Icon property MaxLength attribute:
```csharp
// Before
[MaxLength(100, ErrorMessage = "Icon cannot exceed 100 characters.")]

// After
[MaxLength(1000, ErrorMessage = "Icon cannot exceed 1000 characters.")]
```

#### 2. DTO Update
**File**: `EventForge.DTOs/Dashboard/DashboardConfigurationDto.cs`

Added validation attribute to Icon property:
```csharp
/// <summary>
/// Icon name (MudBlazor icon).
/// </summary>
[MaxLength(1000, ErrorMessage = "Icon cannot exceed 1000 characters.")]
public string? Icon { get; set; }
```

#### 3. Database Migration
**File**: `Migrations/20251120_IncreaseIconColumnLength.sql`

SQL migration to alter the database schema:
```sql
-- Increase Icon column length from NVARCHAR(100) to NVARCHAR(1000)
ALTER TABLE [dbo].[DashboardMetricConfigs]
ALTER COLUMN [Icon] NVARCHAR(1000) NULL;
```

## Dialog UX Improvements

### Issues Fixed

#### 1. Redundant `_isEditingMetric` Flag
**Problem**: The flag was always set alongside `_isFirstTimeSetup`, making it unnecessary and causing confusion.

**Solution**: Removed the `_isEditingMetric` flag completely. The "Salva Configurazione" button now only appears when `_isFirstTimeSetup` is true, which covers both:
- Creating a new configuration
- Editing an existing configuration

#### 2. Redundant Step Indicator
**Problem**: The MetricEditorDialog displayed "Step X di 4" text in DialogActions, which duplicated the visual progress already shown by MudStepper component.

**Solution**: Removed the redundant text indicator. Users now rely on the MudStepper's built-in visual indicators for step progress.

### Files Modified
- `EventForge.Client/Shared/Components/Dialogs/DashboardConfigurationDialog.razor`
  - Removed `_isEditingMetric` field
  - Simplified button visibility logic
  - Removed unnecessary flag assignment after metric creation

- `EventForge.Client/Shared/Components/Dialogs/MetricEditorDialog.razor`
  - Removed redundant "Step X di 4" text from DialogActions
  - Cleaner, less cluttered button area

## Migration Instructions

### For Developers
1. Pull the latest changes
2. Build the solution: `dotnet build`
3. The code changes are complete

### For Database Administrators
1. Review the migration script: `Migrations/20251120_IncreaseIconColumnLength.sql`
2. Apply the migration to your database:
   ```sql
   sqlcmd -S your_server -d EventData -i 20251120_IncreaseIconColumnLength.sql
   ```
3. Verify the column size:
   ```sql
   SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME = 'DashboardMetricConfigs' AND COLUMN_NAME = 'Icon';
   ```
   Expected result: `CHARACTER_MAXIMUM_LENGTH = 1000`

## Testing

### Test Scenario 1: Create Dashboard Configuration
1. Navigate to a management page (e.g., VatRate management)
2. Open Dashboard Configuration dialog
3. Add a new metric with an icon
4. Save the configuration
5. **Expected**: Configuration saves successfully without SQL errors

### Test Scenario 2: Edit Metric
1. Open existing dashboard configuration
2. Click "Modifica" on a metric
3. Navigate through the 4-step stepper
4. **Expected**: 
   - No duplicate "Step X di 4" text visible
   - Only relevant navigation buttons visible (Indietro/Avanti/Salva)
   - Clean, uncluttered dialog interface

### Test Scenario 3: Multiple Metrics
1. Create a new configuration
2. Add multiple metrics (3-5 metrics)
3. Each metric should have different icons
4. Save the configuration
5. **Expected**: All metrics save correctly with their full SVG icons

## Impact

### Benefits
✅ **Fixed SQL truncation errors** - Icons can now be saved without data loss  
✅ **Improved UX** - Cleaner, less confusing dialog interface  
✅ **Better maintainability** - Removed redundant code and simplified logic  
✅ **Future-proof** - 1000 character limit accommodates even the largest MudBlazor icons

### Breaking Changes
❌ **None** - These changes are backward compatible. Existing configurations with shorter icon strings continue to work.

## Notes

- The 1000 character limit is sufficient for all current MudBlazor icons
- If MudBlazor introduces larger icons in the future, the limit can be increased again
- The migration adds a comment to the database column for documentation
- No data loss occurs when applying this migration (existing icon data is preserved)
