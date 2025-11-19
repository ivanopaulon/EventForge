# Remaining Management Pages Completion Guide

## Status
- ✅ Completed: 5/11 pages (VatNatureManagement, BrandManagement, UnitOfMeasureManagement, CustomerManagement, SupplierManagement)
- ✅ Fixed: ProductManagement
- 🔲 Remaining: 5/11 pages

## Remaining Pages

### 1. ClassificationNodeManagement.razor (605 lines)
**Location**: `EventForge.Client/Pages/Management/Products/ClassificationNodeManagement.razor`

**DTO**: `ClassificationNodeDto`
**Icon**: `Icons.Material.Outlined.AccountTree`
**EntityType**: "ClassificationNode"

**Required Changes**:
1. Add `@using EventForge.Client.Shared.Components.Dashboard`
2. Replace `<MudContainer>` with page root div structure:
   ```razor
   <div class="classification-node-page-root">
       <div class="classification-node-top">
           <ManagementDashboard ... />
       </div>
       <div class="eftable-wrapper">
           <EFTable ... />
       </div>
   </div>
   ```

3. Add Dashboard Metrics:
   ```csharp
   private List<DashboardMetric<ClassificationNodeDto>> _dashboardMetrics = new()
   {
       new() {
           Title = "Totale Nodi",
           Type = MetricType.Count,
           Icon = Icons.Material.Outlined.AccountTree,
           Color = "primary",
           Description = "Numero totale di nodi",
           Format = "N0"
       },
       new() {
           Title = "Nodi Radice",
           Type = MetricType.Count,
           Filter = n => n.ParentId == null,
           Icon = Icons.Material.Outlined.FolderOpen,
           Color = "success",
           Description = "Nodi radice (senza padre)",
           Format = "N0"
       },
       new() {
           Title = "Nodi Foglia",
           Type = MetricType.Count,
           Filter = n => !n.HasChildren,
           Icon = Icons.Material.Outlined.Label,
           Color = "info",
           Description = "Nodi foglia (senza figli)",
           Format = "N0"
       },
       new() {
           Title = "Ultimi Aggiunti",
           Type = MetricType.Count,
           Filter = n => n.CreatedAt >= DateTime.Now.AddDays(-30),
           Icon = Icons.Material.Outlined.NewReleases,
           Color = "warning",
           Description = "Nodi aggiunti negli ultimi 30 giorni",
           Format = "N0"
       }
   };
   ```

4. Add EFTable fields:
   - `private EFTable<ClassificationNodeDto> _efTable = null!;`
   - `private CancellationTokenSource? _searchDebounceCts;`
   - `private EventCallback<HashSet<ClassificationNodeDto>> _selectedItemsChangedCallback`

5. Add Column Configurations (typical fields: Code, Name, Description, ParentName, Level, etc.)

6. Update `ClearFilters()` to be synchronous (remove `async Task`, change to `void`)

7. Update `OnSearchChanged()` with proper cancellation token pattern

8. Use safe substring: `@(item.Id.ToString()[..Math.Min(8, item.Id.ToString().Length)])...`

---

### 2. DocumentTypeManagement.razor (404 lines)
**Location**: `EventForge.Client/Pages/Management/Documents/DocumentTypeManagement.razor`

**DTO**: `DocumentTypeDto`
**Icon**: `Icons.Material.Outlined.Category`
**EntityType**: "DocumentType"

**Dashboard Metrics**:
```csharp
private List<DashboardMetric<DocumentTypeDto>> _dashboardMetrics = new()
{
    new() {
        Title = "Totale Tipi",
        Type = MetricType.Count,
        Icon = Icons.Material.Outlined.Category,
        Color = "primary",
        Description = "Numero totale di tipi documento",
        Format = "N0"
    },
    new() {
        Title = "Documenti Fiscali",
        Type = MetricType.Count,
        Filter = dt => dt.IsFiscal,
        Icon = Icons.Material.Outlined.Receipt,
        Color = "success",
        Description = "Tipi documento fiscali",
        Format = "N0"
    },
    new() {
        Title = "Tipi Carico",
        Type = MetricType.Count,
        Filter = dt => dt.StockMovementType == StockMovementType.Increase,
        Icon = Icons.Material.Outlined.ArrowUpward,
        Color = "info",
        Description = "Tipi con movimento di carico",
        Format = "N0"
    },
    new() {
        Title = "Ultimi Aggiunti",
        Type = MetricType.Count,
        Filter = dt => dt.CreatedAt >= DateTime.Now.AddDays(-30),
        Icon = Icons.Material.Outlined.NewReleases,
        Color = "warning",
        Description = "Tipi aggiunti negli ultimi 30 giorni",
        Format = "N0"
    }
};
```

**Column Configurations**: Code, Name, Description, IsFiscal, StockMovementType, etc.

---

### 3. DocumentCounterManagement.razor (288 lines)
**Location**: `EventForge.Client/Pages/Management/Documents/DocumentCounterManagement.razor`

**DTO**: `DocumentCounterDto`
**Icon**: `Icons.Material.Outlined.Numbers`
**EntityType**: "DocumentCounter"

**Dashboard Metrics**:
```csharp
private List<DashboardMetric<DocumentCounterDto>> _dashboardMetrics = new()
{
    new() {
        Title = "Totale Contatori",
        Type = MetricType.Count,
        Icon = Icons.Material.Outlined.Numbers,
        Color = "primary",
        Description = "Numero totale di contatori",
        Format = "N0"
    },
    new() {
        Title = "Contatori Attivi",
        Type = MetricType.Count,
        Filter = dc => dc.IsActive,
        Icon = Icons.Material.Outlined.CheckCircle,
        Color = "success",
        Description = "Contatori attivi",
        Format = "N0"
    },
    new() {
        Title = "Anno Corrente",
        Type = MetricType.Count,
        Filter = dc => dc.Year == DateTime.Now.Year,
        Icon = Icons.Material.Outlined.CalendarToday,
        Color = "info",
        Description = "Contatori per l'anno corrente",
        Format = "N0"
    },
    new() {
        Title = "Ultimi Aggiunti",
        Type = MetricType.Count,
        Filter = dc => dc.CreatedAt >= DateTime.Now.AddDays(-30),
        Icon = Icons.Material.Outlined.NewReleases,
        Color = "warning",
        Description = "Contatori aggiunti negli ultimi 30 giorni",
        Format = "N0"
    }
};
```

**Column Configurations**: DocumentTypeName, Year, Prefix, CurrentValue, etc.

---

### 4. WarehouseManagement.razor (499 lines)
**Location**: `EventForge.Client/Pages/Management/Warehouse/WarehouseManagement.razor`

**DTO**: `StorageFacilityDto`
**Icon**: `Icons.Material.Outlined.Warehouse`
**EntityType**: "Warehouse"

**Dashboard Metrics**:
```csharp
private List<DashboardMetric<StorageFacilityDto>> _dashboardMetrics = new()
{
    new() {
        Title = "Totale Magazzini",
        Type = MetricType.Count,
        Icon = Icons.Material.Outlined.Warehouse,
        Color = "primary",
        Description = "Numero totale di magazzini",
        Format = "N0"
    },
    new() {
        Title = "Magazzini Fiscali",
        Type = MetricType.Count,
        Filter = w => w.IsFiscalWarehouse,
        Icon = Icons.Material.Outlined.Gavel,
        Color = "success",
        Description = "Magazzini fiscali",
        Format = "N0"
    },
    new() {
        Title = "Refrigerati",
        Type = MetricType.Count,
        Filter = w => w.IsRefrigerated,
        Icon = Icons.Material.Outlined.AcUnit,
        Color = "info",
        Description = "Magazzini refrigerati",
        Format = "N0"
    },
    new() {
        Title = "Ultimi Aggiunti",
        Type = MetricType.Count,
        Filter = w => w.CreatedAt >= DateTime.Now.AddDays(-30),
        Icon = Icons.Material.Outlined.NewReleases,
        Color = "warning",
        Description = "Magazzini aggiunti negli ultimi 30 giorni",
        Format = "N0"
    }
};
```

**Column Configurations**: Code, Name, Description, IsFiscalWarehouse, IsRefrigerated, etc.

---

### 5. LotManagement.razor (395 lines)
**Location**: `EventForge.Client/Pages/Management/Warehouse/LotManagement.razor`

**DTO**: `LotDto`
**Icon**: `Icons.Material.Outlined.QrCode`
**EntityType**: "Lot"

**Dashboard Metrics**:
```csharp
private List<DashboardMetric<LotDto>> _dashboardMetrics = new()
{
    new() {
        Title = "Totale Lotti",
        Type = MetricType.Count,
        Icon = Icons.Material.Outlined.QrCode,
        Color = "primary",
        Description = "Numero totale di lotti",
        Format = "N0"
    },
    new() {
        Title = "Lotti Attivi",
        Type = MetricType.Count,
        Filter = l => l.IsActive,
        Icon = Icons.Material.Outlined.CheckCircle,
        Color = "success",
        Description = "Lotti attivi",
        Format = "N0"
    },
    new() {
        Title = "In Scadenza",
        Type = MetricType.Count,
        Filter = l => l.ExpirationDate.HasValue && l.ExpirationDate.Value <= DateTime.Now.AddDays(30),
        Icon = Icons.Material.Outlined.Warning,
        Color = "warning",
        Description = "Lotti in scadenza nei prossimi 30 giorni",
        Format = "N0"
    },
    new() {
        Title = "Ultimi Aggiunti",
        Type = MetricType.Count,
        Filter = l => l.CreatedAt >= DateTime.Now.AddDays(-30),
        Icon = Icons.Material.Outlined.NewReleases,
        Color = "info",
        Description = "Lotti aggiunti negli ultimi 30 giorni",
        Format = "N0"
    }
};
```

**Column Configurations**: LotNumber, ProductName, ExpirationDate, Quantity, etc.

---

## Common Pattern for All Pages

### 1. HTML Structure Replacement
**Before**:
```razor
<MudContainer MaxWidth="MaxWidth.False" ...>
    <PageLoadingOverlay .../>
    @if (!_isLoading) {
        <MudPaper ...>
```

**After**:
```razor
<PageLoadingOverlay .../>
@if (!_isLoading) {
    <div class="[entity]-page-root">
        <div class="[entity]-top">
            <ManagementDashboard ... />
        </div>
        <div class="eftable-wrapper">
            <EFTable ... />
```

### 2. MudTable → EFTable Conversion
- Replace `<MudTable>` with `<EFTable>`
- Add `ComponentKey`, `InitialColumnConfigurations`, `AllowDragDropGrouping`
- Update HeaderContent to use `EFTableColumnHeader`
- Update RowTemplate to use dynamic columns with visible column check

### 3. Code Section Updates
Add these fields:
```csharp
private EFTable<TDto> _efTable = null!;
private CancellationTokenSource? _searchDebounceCts;
private List<EFTableColumnConfiguration> _initialColumns = new() { ... };
private List<DashboardMetric<TDto>> _dashboardMetrics = new() { ... };
private EventCallback<HashSet<TDto>> _selectedItemsChangedCallback => ...;
```

Add this method:
```csharp
private void OnSelectedItemsChanged(HashSet<TDto> items)
{
    _selected[Entities] = items;
    StateHasChanged();
}
```

Fix these methods:
```csharp
// Make synchronous
private void ClearFilters()
{
    _searchTerm = string.Empty;
    StateHasChanged();
}

// Add proper cancellation
private async Task OnSearchChanged()
{
    _searchDebounceCts?.Cancel();
    _searchDebounceCts = new CancellationTokenSource();
    var token = _searchDebounceCts.Token;
    
    try
    {
        await Task.Delay(300, token);
        if (!token.IsCancellationRequested)
        {
            StateHasChanged();
        }
    }
    catch (OperationCanceledException)
    {
        // Swallow cancellation
    }
}
```

### 4. Safe Operations
- Use safe substring: `@(item.Id.ToString()[..Math.Min(8, item.Id.ToString().Length)])...`
- Add null checks in filters: `Filter = x => x.Name != null && x.Name.Contains(...)`

---

## Testing Each Page

After completing each page:

```bash
cd /home/runner/work/EventForge/EventForge
dotnet build --no-incremental EventForge.Client/EventForge.Client.csproj
```

Verify:
- ✅ 0 Errors
- ✅ Dashboard shows 4 metrics
- ✅ EFTable loads correctly
- ✅ Drag-drop grouping works
- ✅ Column configuration persists
- ✅ All actions (Create, Edit, Delete, Audit) work

---

## Implementation Priority

1. **DocumentCounterManagement** (smallest, 288 lines)
2. **LotManagement** (395 lines)
3. **DocumentTypeManagement** (404 lines)
4. **WarehouseManagement** (499 lines)
5. **ClassificationNodeManagement** (largest, 605 lines)

**Total Estimated Time**: 2-3 hours for all 5 pages

---

## References

- **Completed Templates**: 
  - `EventForge.Client/Pages/Management/Financial/VatNatureManagement.razor`
  - `EventForge.Client/Pages/Management/Business/CustomerManagement.razor`
  - `EventForge.Client/Pages/Management/Business/SupplierManagement.razor`
  
- **Guide**: `MANAGEMENT_PAGES_REFACTORING_GUIDE.md`
- **PR Context**: PR #662 and issue #663

---

## Notes

- Always maintain existing functionality
- Keep the same translation keys
- Preserve any page-specific features (e.g., ManageProducts button in SupplierManagement)
- Test after each page completion
- Commit incrementally with clear messages
