# EFTable Grouping Persistence Fix

## Problem Description (Italian)
"controlla nuovamente il componente eftable il salvataggio del grouping non avviene correttamente, infatti se raggruppa e per qualche motivo si aggiorna perdo il grouping"

**Translation:** 
"check again the eftable component, the grouping save is not happening correctly, in fact if I group and for some reason it refreshes I lose the grouping"

## Root Cause Analysis

The EFTable component has two lifecycle methods that manage grouping configuration:

1. **OnInitializedAsync()** (line 223-236)
   - Loads user preferences from localStorage via TablePreferencesService
   - Sets `_groupByProperties` from saved preferences

2. **OnParametersSet()** (line 238-249 - BEFORE FIX)
   - Runs after OnInitializedAsync and on every parameter change
   - Was unconditionally overwriting `_groupByProperties` with the `GroupByProperties` parameter value
   - This caused saved preferences to be lost on component updates/refreshes

## The Fix

Modified `OnParametersSet()` to intelligently handle grouping updates:

```csharp
protected override void OnParametersSet()
{
    // Only update grouping from parameter if:
    // 1. Preferences haven't been loaded yet (first initialization, _preferences is null)
    // 2. OR the parameter has a non-empty value that differs from current state
    // This ensures saved preferences take precedence over parameter defaults
    if (_preferences == null || (GroupByProperties.Any() && !_groupByProperties.SequenceEqual(GroupByProperties)))
    {
        _groupByProperties = new List<string>(GroupByProperties);
        _lastGroupValues.Clear();
    }
}
```

### Key Changes:
- Check if `_preferences` is null (not yet loaded) before updating
- Only update from parameter if it has non-empty values that differ from current state
- Preserves saved preferences during component refreshes

## Behavior Verification

### Scenario 1: First load without saved preferences
- _preferences = null
- Condition passes → uses parameter value
- **Result:** ✅ Uses default parameter

### Scenario 2: First load with saved grouping
- _preferences loaded with GroupByProperties: ["Status"]
- Parameter GroupByProperties is empty []
- Condition fails (GroupByProperties.Any() is false)
- **Result:** ✅ Uses saved preferences

### Scenario 3: Component refresh with saved grouping
- _preferences already loaded
- _groupByProperties = ["Status"]
- Parameter GroupByProperties is empty []
- Condition fails (GroupByProperties.Any() is false)
- **Result:** ✅ Preserves saved grouping

### Scenario 4: Explicit parameter override
- _preferences loaded
- _groupByProperties = ["Status"]
- Parameter GroupByProperties = ["Name"]
- Condition passes (non-empty AND different)
- **Result:** ✅ Allows parameter override

## Files Modified

- `EventForge.Client/Shared/Components/EFTable.razor` (7 lines changed)
  - Modified OnParametersSet() method to preserve loaded preferences

## Testing

- ✅ Client project builds successfully with 0 errors
- ✅ All lifecycle scenarios verified
- ✅ CodeQL security scan passed - no vulnerabilities

## Security Summary

No security vulnerabilities were introduced or discovered in this change. The modification is a pure logic fix in the component lifecycle that improves the persistence behavior of user preferences.
