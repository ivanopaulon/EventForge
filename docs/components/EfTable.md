# EFTable Component Documentation

## Overview

`EFTable` is a powerful, highly configurable Blazor table component built on top of MudBlazor's `MudTable`. It provides enterprise-grade features including:

- ✅ Client-side and server-side data support
- ✅ Built-in toolbar with title, search, filters, and export
- ✅ Column configuration and reordering with persistence
- ✅ Multi-level drag-and-drop grouping
- ✅ Custom actions and bulk operations
- ✅ Row selection (single/multiple)
- ✅ Debounced search
- ✅ Customizable via slots/RenderFragments
- ✅ Accessibility features (ARIA labels, keyboard navigation)

## Location

**Path**: `EventForge.Client/Shared/Components/EFTable.razor`
**Models**: `EventForge.Client/Shared/Components/EFTableModels.cs`

## Basic Usage

### Minimal Example

```razor
<EFTable TItem="ProductDto"
         Items="@_products"
         Title="Products"
         ComponentKey="ProductsList">
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Price</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd>@context.Price.ToString("C2")</MudTd>
    </RowTemplate>
</EFTable>

@code {
    private List<ProductDto> _products = new();
}
```

### With Built-in Toolbar Features

```razor
<EFTable TItem="VatRateDto"
         Items="@_vatRates"
         Title="VAT Rates Management"
         Subtitle="Manage your VAT rates"
         ShowSearch="true"
         SearchPlaceholder="Search VAT rates..."
         SearchDebounce="300"
         ShowExport="true"
         ExportFormats="@(new List<string> { "CSV", "Excel", "PDF" })"
         OnSearch="@HandleSearch"
         OnExport="@HandleExport"
         OnAdd="@CreateVatRate"
         ComponentKey="VatRateManagement"
         InitialColumnConfigurations="@_columns">
    <HeaderContent Context="columnConfigs">
        @foreach (var column in columnConfigs.Where(c => c.IsVisible).OrderBy(c => c.Order))
        {
            @if (column.PropertyName == "Name")
            {
                <MudTh>Name</MudTh>
            }
            @if (column.PropertyName == "Percentage")
            {
                <MudTh>Percentage</MudTh>
            }
        }
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd>@context.Percentage%</MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Outlined.Edit" 
                          OnClick="@(() => EditVatRate(context.Id))" />
            <MudIconButton Icon="@Icons.Material.Outlined.Delete" 
                          OnClick="@(() => DeleteVatRate(context))" />
        </MudTd>
    </RowTemplate>
</EFTable>

@code {
    private List<VatRateDto> _vatRates = new();
    private List<EFTableColumnConfiguration> _columns = new()
    {
        new() { PropertyName = "Name", DisplayName = "Name", IsVisible = true, Order = 0 },
        new() { PropertyName = "Percentage", DisplayName = "Percentage", IsVisible = true, Order = 1 }
    };

    private async Task HandleSearch(string searchTerm)
    {
        // Filter data based on search term
        await Task.CompletedTask;
    }

    private async Task HandleExport(string format)
    {
        // Export data in the specified format
        await Task.CompletedTask;
    }

    private void CreateVatRate() { }
    private void EditVatRate(Guid id) { }
    private void DeleteVatRate(VatRateDto item) { }
}
```

## Parameters

### Header Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string?` | `null` | Main title displayed in built-in toolbar |
| `Subtitle` | `string?` | `null` | Subtitle displayed below title |

### Data Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Items` | `IEnumerable<TItem>?` | `null` | Client-side data collection |
| `ServerData` | `Func<TableState, CancellationToken, Task<TableData<TItem>>>?` | `null` | Server-side data provider function |

### Selection Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `MultiSelection` | `bool` | `false` | Enable multiple row selection |
| `SelectedItems` | `HashSet<TItem>` | `new()` | Currently selected items |
| `SelectedItemsChanged` | `EventCallback<HashSet<TItem>>` | - | Event fired when selection changes |

### Search Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ShowSearch` | `bool` | `false` | Show built-in search field |
| `SearchPlaceholder` | `string?` | `null` | Placeholder text for search field |
| `SearchDebounce` | `int` | `300` | Debounce delay in milliseconds |
| `OnSearch` | `EventCallback<string>` | - | Event fired when search term changes (after debounce) |

### Filter Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ShowFilters` | `bool` | `false` | Show filter toggle button |
| `FiltersPanel` | `RenderFragment?` | `null` | Custom filters content |
| `OnToggleFilters` | `EventCallback` | - | Event fired when filters are toggled |

### Export Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ShowExport` | `bool` | `false` | Show export menu |
| `ExportFormats` | `List<string>` | `["CSV", "Excel"]` | Available export formats |
| `OnExport` | `EventCallback<string>` | - | Event fired when export format is selected |

### Action Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Actions` | `List<EFTableAction>?` | `null` | Custom action descriptors |
| `UseDefaultActions` | `bool` | `true` | Show default add action button |
| `OnAdd` | `EventCallback` | - | Event fired when add button is clicked |
| `OnEdit` | `EventCallback<TItem>` | - | Event fired when edit is requested |
| `OnDelete` | `EventCallback<TItem>` | - | Event fired when delete is requested |
| `OnView` | `EventCallback<TItem>` | - | Event fired when view is requested |
| `OnAction` | `EventCallback<EFTableActionEventArgs>` | - | Event fired for custom actions |

### Appearance Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Hover` | `bool` | `true` | Enable row hover effect |
| `Striped` | `bool` | `true` | Use striped rows |
| `Dense` | `bool` | `true` | Use dense/compact layout |
| `FixedHeader` | `bool` | `true` | Keep header fixed while scrolling |
| `Height` | `string?` | `null` | Fixed height (e.g., "400px") |
| `IsLoading` | `bool` | `false` | Show loading indicator |
| `LoadingProgressColor` | `Color` | `Color.Primary` | Color of loading bar |

### Column Configuration Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ShowColumnConfiguration` | `bool` | `true` | Show column configuration menu |
| `ComponentKey` | `string` | `"DefaultTable"` | Unique key for preference persistence |
| `InitialColumnConfigurations` | `List<EFTableColumnConfiguration>` | `new()` | Initial column setup |

### Grouping Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `GroupByProperties` | `List<string>` | `new()` | Initial grouping properties |
| `AllowDragDropGrouping` | `bool` | `true` | Enable drag-and-drop grouping |

### Content Parameters (Slots/RenderFragments)
| Parameter | Type | Description |
|-----------|------|-------------|
| `ToolBarContent` | `RenderFragment?` | Custom toolbar (overrides built-in toolbar) |
| `HeaderContent` | `RenderFragment<List<EFTableColumnConfiguration>>?` | Table header with column configs |
| `RowTemplate` | `RenderFragment<TItem>?` | Row rendering template |
| `NoRecordsContent` | `RenderFragment?` | Content shown when no data |
| `PagerContent` | `RenderFragment?` | Custom pagination content |
| `FiltersPanel` | `RenderFragment?` | Custom filters content |

## Models

### EFTableColumnConfiguration

```csharp
public class EFTableColumnConfiguration
{
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
}
```

### EFTableAction

```csharp
public class EFTableAction
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool RequiresSelection { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public string? Tooltip { get; set; }
}
```

### EFTableActionEventArgs

```csharp
public class EFTableActionEventArgs
{
    public string ActionId { get; set; } = string.Empty;
    public object? Payload { get; set; }
}
```

## Advanced Examples

### Custom Actions

```razor
<EFTable TItem="ProductDto"
         Items="@_products"
         Actions="@_customActions"
         OnAction="@HandleCustomAction">
    <!-- ... -->
</EFTable>

@code {
    private List<EFTableAction> _customActions = new()
    {
        new()
        {
            Id = "bulk-price-update",
            Label = "Update Prices",
            Icon = Icons.Material.Outlined.AttachMoney,
            Color = "Warning",
            RequiresSelection = true,
            Tooltip = "Update prices for selected products"
        },
        new()
        {
            Id = "bulk-activate",
            Label = "Activate All",
            Icon = Icons.Material.Outlined.CheckCircle,
            Color = "Success",
            RequiresSelection = false
        }
    };

    private async Task HandleCustomAction(EFTableActionEventArgs args)
    {
        switch (args.ActionId)
        {
            case "bulk-price-update":
                // Handle bulk price update
                break;
            case "bulk-activate":
                // Handle bulk activation
                break;
        }
    }
}
```

### Server-Side Data

```razor
<EFTable TItem="ProductDto"
         ServerData="@LoadServerData"
         Title="Products (Server-Side)">
    <!-- ... -->
</EFTable>

@code {
    private async Task<TableData<ProductDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
    {
        var data = await ProductService.GetProductsAsync(
            page: state.Page,
            pageSize: state.PageSize,
            sortBy: state.SortLabel,
            sortDirection: state.SortDirection == SortDirection.Ascending ? "asc" : "desc",
            cancellationToken: cancellationToken
        );

        return new TableData<ProductDto>
        {
            TotalItems = data.TotalCount,
            Items = data.Items
        };
    }
}
```

### With Filters Panel

```razor
<EFTable TItem="VatRateDto"
         Items="@_filteredVatRates"
         ShowFilters="true"
         OnToggleFilters="@HandleToggleFilters">
    <FiltersPanel>
        <MudStack Row="true" Spacing="2">
            <MudSelect @bind-Value="_statusFilter" Label="Status">
                <MudSelectItem Value="@("all")">All</MudSelectItem>
                <MudSelectItem Value="@("active")">Active</MudSelectItem>
                <MudSelectItem Value="@("suspended")">Suspended</MudSelectItem>
            </MudSelect>
            <MudButton OnClick="@ApplyFilters" Variant="Variant.Filled" Color="Color.Primary">
                Apply
            </MudButton>
        </MudStack>
    </FiltersPanel>
    <!-- ... -->
</EFTable>

@code {
    private string _statusFilter = "all";

    private void ApplyFilters()
    {
        // Apply filters and update _filteredVatRates
    }
}
```

### Custom Toolbar

```razor
<EFTable TItem="ProductDto"
         Items="@_products">
    <ToolBarContent>
        <MudText Typo="Typo.h5">My Custom Toolbar</MudText>
        <MudSpacer />
        <MudButton Color="Color.Primary">Custom Action</MudButton>
    </ToolBarContent>
    <!-- ... -->
</EFTable>
```

## Features

### Column Configuration

Users can:
- Show/hide columns
- Reorder columns via drag-and-drop
- Reset to default configuration

Configuration is persisted per user and per table using the `ComponentKey` parameter.

### Drag-and-Drop Grouping

When `AllowDragDropGrouping` is true (client-side only):
1. Drag column headers to the grouping panel
2. Support for multi-level grouping
3. Visual hierarchy with indentation
4. Item counts per group

### Search with Debounce

When `ShowSearch` is true:
- Built-in search field in toolbar
- Configurable debounce delay (default 300ms)
- `OnSearch` event fires after debounce period
- Parent component handles actual filtering

### Export

When `ShowExport` is true:
- Dropdown menu with configured formats
- `OnExport` event passes selected format
- Parent component handles actual export logic

### Row Selection

- Single or multi-selection via `MultiSelection`
- `SelectedItems` tracks current selection
- `SelectedItemsChanged` event for reactivity
- Actions can require selection via `RequiresSelection`

## Reference Implementation

See **VatRateManagement.razor** for a complete real-world example:
```
EventForge.Client/Pages/Management/Financial/VatRateManagement.razor
```

This page demonstrates:
- Column configuration
- Multi-selection
- Search integration
- Custom row actions
- Grouping
- Dashboard integration

## Accessibility

EFTable follows WCAG 2.1 AA standards:

- **Keyboard Navigation**: All interactive elements are keyboard accessible
- **ARIA Labels**: Proper labels on toolbar buttons and actions
- **Screen Reader Support**: Table structure announced correctly
- **Focus Management**: Clear focus indicators
- **Color Contrast**: Meets minimum 4.5:1 ratio

## Performance Considerations

- **Client-Side**: Efficient for up to ~1000 items
- **Server-Side**: Use `ServerData` for larger datasets
- **Virtualization**: Consider MudBlazor's virtualization for very large lists
- **Debounce**: Prevents excessive search requests
- **Memoization**: Column configurations are cached

## Browser Support

- ✅ Chrome/Edge (latest)
- ✅ Firefox (latest)
- ✅ Safari (latest)
- ⚠️ Mobile drag-and-drop has limited support

## Testing

Unit tests are available at:
```
EventForge.Tests/Components/EFTableTests.cs
```

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~EFTableTests"
```

## Troubleshooting

### Column configuration dialog is empty
- Ensure `InitialColumnConfigurations` is properly set
- Check that `ComponentKey` is unique for each table instance

### Grouping doesn't work
- Grouping only works with client-side data (`Items`, not `ServerData`)
- Ensure `AllowDragDropGrouping` is `true`

### Search doesn't filter data
- `EFTable` only emits the `OnSearch` event
- Parent component must implement actual filtering logic

### Preferences not persisting
- Check that `ComponentKey` is set
- Verify `ITablePreferencesService` is registered in DI

## Migration from Legacy Tables

To migrate an existing page to EFTable:

1. **Replace table markup** with `<EFTable>`
2. **Move toolbar** to EFTable parameters or `ToolBarContent`
3. **Configure columns** via `InitialColumnConfigurations`
4. **Wire events** (OnSearch, OnExport, OnAdd, etc.)
5. **Test thoroughly**, especially selection and actions

## Future Enhancements

Potential future features:
- [ ] Virtual scrolling integration
- [ ] Advanced filtering UI
- [ ] CSV/Excel export built-in
- [ ] Column resizing
- [ ] Touch-friendly grouping
- [ ] Column pinning (freeze columns)

## Contributing

When enhancing EFTable:
1. Maintain backward compatibility
2. Add unit tests for new features
3. Update this documentation
4. Follow existing code style
5. Test with VatRateManagement page

## License

Part of EventForge project. See root LICENSE file.
