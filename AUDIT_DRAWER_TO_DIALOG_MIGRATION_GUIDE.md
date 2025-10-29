# Audit Drawer to Dialog Migration Guide

**Related Issue**: #542 - Migrazione Audit: Drawer → Dialog fullscreen
**Status**: Pilot implementation complete
**Last Updated**: October 29, 2025

## Overview

This guide provides step-by-step instructions for migrating pages from the legacy `AuditHistoryDrawer` to the new fullscreen `AuditHistoryDialog` component.

## Why Migrate?

The `AuditHistoryDialog` provides several advantages over the drawer approach:

✅ **Fullscreen Experience**: More screen real estate for viewing audit history
✅ **Better UX**: Improved readability and navigation
✅ **Consistent Pattern**: Aligns with modern dialog-based UI patterns
✅ **Mobile Friendly**: Better responsive behavior on smaller screens
✅ **Enhanced Features**: All drawer features (filters, pagination, timeline) in a cleaner layout

## Components

### Old: AuditHistoryDrawer
- **Location**: `EventForge.Client/Shared/Components/Drawers/AuditHistoryDrawer.razor`
- **Status**: ⚠️ DEPRECATED (see deprecation comment in file)
- **Display**: Side drawer (700px wide)

### New: AuditHistoryDialog
- **Location**: `EventForge.Client/Shared/Components/Dialogs/AuditHistoryDialog.razor`
- **Status**: ✅ ACTIVE
- **Display**: Fullscreen dialog

## Migration Steps

### Step 1: Update the Component Reference

**Before** (Drawer):
```razor
<AuditHistoryDrawer @bind-IsOpen="_auditDrawerOpen"
                    EntityType="ClassificationNode"
                    EntityId="@(_isCreateMode ? null : _entity?.Id)"
                    EntityName="@(_isCreateMode ? null : _entity?.Name)" />
```

**After** (Dialog):
```razor
<AuditHistoryDialog @bind-IsOpen="_auditDialogOpen"
                    EntityType="ClassificationNode"
                    EntityId="@(_isCreateMode ? null : _entity?.Id)"
                    EntityName="@(_isCreateMode ? null : _entity?.Name)" />
```

### Step 2: Update the State Variable

**Before**:
```csharp
private bool _auditDrawerOpen = false;
```

**After**:
```csharp
private bool _auditDialogOpen = false;
```

### Step 3: Update the Open Method

**Before**:
```csharp
private void OpenAuditDrawer()
{
    _auditDrawerOpen = true;
}
```

**After**:
```csharp
private void OpenAuditDialog()
{
    _auditDialogOpen = true;
}
```

### Step 4: Update UI Call Sites

**Before**:
```razor
<MudButton OnClick="@OpenAuditDrawer">
    @TranslationService.GetTranslation("entity.viewAudit", "Apri cronologia")
</MudButton>
```

**After**:
```razor
<MudButton Variant="Variant.Filled" 
           Color="Color.Primary"
           StartIcon="@Icons.Material.Outlined.History"
           OnClick="@OpenAuditDialog">
    @TranslationService.GetTranslation("entity.viewAudit", "Apri cronologia")
</MudButton>
```

### Step 5: Update Translation Keys (Optional)

If your page uses `messages.auditInDrawer`, consider updating it:

**Before**:
```razor
<MudText>@TranslationService.GetTranslation("messages.auditInDrawer", "Storia modifiche disponibile nell'audit drawer")</MudText>
```

**After**:
```razor
<MudText>@TranslationService.GetTranslation("messages.auditFullscreen", "Storia modifiche disponibile in modalità fullscreen")</MudText>
```

**Note**: The TranslationService.GetTranslation method uses fallback text (second parameter) if the translation key is not found, so the application will continue to work even if new keys haven't been added to translation resources yet.

## Complete Example: ClassificationNodeDetail Migration

### Before Migration

```razor
@page "/classification-nodes/{NodeId:guid}"

<!-- ... other content ... -->

<MudTabPanel Text="@TranslationService.GetTranslation("classificationNode.audit", "Audit")" 
             Icon="@Icons.Material.Outlined.History">
    <MudText Typo="Typo.body2">
        @TranslationService.GetTranslation("messages.auditInDrawer", "Storia modifiche disponibile nell'audit drawer")
    </MudText>
    <MudButton Class="mt-3" OnClick="@OpenAuditDrawer">
        @TranslationService.GetTranslation("classificationNode.viewAudit", "Apri cronologia")
    </MudButton>
</MudTabPanel>

<AuditHistoryDrawer @bind-IsOpen="_auditDrawerOpen"
                    EntityType="ClassificationNode"
                    EntityId="@(_isCreateMode ? null : _node?.Id)"
                    EntityName="@(_isCreateMode ? null : _node?.Name)" />

@code {
    private bool _auditDrawerOpen = false;
    
    private void OpenAuditDrawer()
    {
        _auditDrawerOpen = true;
    }
}
```

### After Migration

```razor
@page "/classification-nodes/{NodeId:guid}"

<!-- ... other content ... -->

<MudTabPanel Text="@TranslationService.GetTranslation("classificationNode.audit", "Audit")" 
             Icon="@Icons.Material.Outlined.History">
    <MudText Typo="Typo.body2">
        @TranslationService.GetTranslation("messages.auditFullscreen", "Storia modifiche disponibile in modalità fullscreen")
    </MudText>
    <MudButton Class="mt-3" 
               Variant="Variant.Filled" 
               Color="Color.Primary"
               StartIcon="@Icons.Material.Outlined.History"
               OnClick="@OpenAuditDialog">
        @TranslationService.GetTranslation("classificationNode.viewAudit", "Apri cronologia")
    </MudButton>
</MudTabPanel>

<AuditHistoryDialog @bind-IsOpen="_auditDialogOpen"
                    EntityType="ClassificationNode"
                    EntityId="@(_isCreateMode ? null : _node?.Id)"
                    EntityName="@(_isCreateMode ? null : _node?.Name)" />

@code {
    private bool _auditDialogOpen = false;
    
    private void OpenAuditDialog()
    {
        _auditDialogOpen = true;
    }
}
```

## Pages to Migrate

### ✅ Migrated (Detail Pages)
- [x] `ClassificationNodeDetail.razor` - **Pilot implementation complete**

### 📋 Pending Migration (Management Pages)

These management pages still use AuditHistoryDrawer:

1. `CustomerManagement.razor`
2. `SupplierManagement.razor`
3. `WarehouseManagement.razor`
4. `ProductManagement.razor`
5. `BrandManagement.razor`
6. `UnitOfMeasureManagement.razor`
7. `DocumentTypeManagement.razor`
8. `VatRateManagement.razor`
9. `ClassificationNodeManagement.razor`
10. `SuperAdmin/TenantManagement.razor`
11. `SuperAdmin/UserManagement.razor`

**Note**: A comprehensive code search may reveal additional management pages using AuditHistoryDrawer.

**Note**: Management pages have a slightly different pattern - the audit button is typically in a toolbar action area rather than in a tab panel.

## Management Page Migration Pattern

For management pages, the audit button is usually in the toolbar:

### Before (Management Page):
```razor
<ManagementTableToolbar ShowAudit="true"
                        OnAudit="@(() => _auditDrawerOpen = true)" />

<AuditHistoryDrawer @bind-IsOpen="_auditDrawerOpen" ... />

@code {
    private bool _auditDrawerOpen = false;
}
```

### After (Management Page):
```razor
<ManagementTableToolbar ShowAudit="true"
                        OnAudit="@(() => _auditDialogOpen = true)" />

<AuditHistoryDialog @bind-IsOpen="_auditDialogOpen" ... />

@code {
    private bool _auditDialogOpen = false;
}
```

## Features Preserved

The new `AuditHistoryDialog` includes all features from the drawer:

✅ **Advanced Filters**
- Filter by operation type (Created, Updated, Deleted, Activated, Deactivated)
- Filter by user
- Filter by field name
- Date range filtering (from/to)

✅ **Pagination**
- Configurable page size (default: 10 items)
- First/Last/Previous/Next page navigation
- Shows current page and total pages

✅ **Timeline View**
- Chronological timeline display
- Color-coded actions
- Action icons
- Detailed change tracking

✅ **Enhanced Display**
- Fullscreen layout
- AppBar with entity context
- Loading overlay
- Empty state handling
- Results count display

## Testing Checklist

After migrating a page, verify:

- [ ] Audit button/link opens the dialog correctly
- [ ] Dialog displays in fullscreen mode
- [ ] Entity information (type, ID, name) is correctly passed
- [ ] Filters work correctly
- [ ] Pagination works correctly
- [ ] Timeline displays audit entries
- [ ] Close button works
- [ ] ESC key closes the dialog
- [ ] Loading state displays correctly
- [ ] Empty state displays when no audit history

## Troubleshooting

### Dialog doesn't open
- Check that `_auditDialogOpen` state variable is being set to `true`
- Verify `@bind-IsOpen` binding is correct
- Ensure the dialog component is included in the page markup

### Entity information not displaying
- Verify `EntityId` has a value (not null)
- Check that `EntityType` and `EntityName` are being passed correctly
- For create mode, ensure EntityId is null or not passed

### Build errors
- Ensure you've updated all references from drawer to dialog
- Check that variable names are consistent
- Verify no typos in method names

## Best Practices

1. **Always test after migration**: Verify the dialog opens and functions correctly
2. **Update related documentation**: If the page has specific documentation, update it
3. **Consider user flow**: Ensure the new fullscreen dialog fits well in the user experience
4. **Consistent naming**: Use `_auditDialogOpen` pattern across all pages
5. **Translation keys**: Update translation keys to reflect "fullscreen" rather than "drawer"

## Future Enhancements

Potential improvements to the dialog:

- [ ] Export audit history to CSV/PDF
- [ ] Print functionality
- [ ] Compare two audit entries side-by-side
- [ ] Highlight specific field changes
- [ ] Advanced search with regular expressions
- [ ] Bookmark/save filter presets

## References

- **Issue #542**: Migrazione Audit: Drawer → Dialog fullscreen
- **DRAWER_DEPRECATION_STATUS.md**: Overall drawer deprecation status
- **Component**: `EventForge.Client/Shared/Components/Dialogs/AuditHistoryDialog.razor`

## Support

If you encounter issues during migration:

1. Check this guide for common patterns
2. Review the pilot implementation in `ClassificationNodeDetail.razor`
3. Ensure all steps are completed correctly
4. Test in a development environment first

---

**Note**: This is an ongoing migration. Update this document as new patterns emerge or additional pages are migrated.
