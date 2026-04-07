# Management Dashboard Component

## Overview

The `ManagementDashboard` component is a reusable, generic dashboard component designed for management pages. It provides configurable metrics, filters, and visualization capabilities for data analysis and reporting.

## Features

- **Generic Support**: Works with any data type using `TItem` generic parameter
- **Client-Side Calculation**: Efficient client-side metric computation
- **Server-Side Support**: Optional server-side metric calculation via `ServerMetricProvider`
- **Multiple Metric Types**: Count, Sum, Average, Min, Max
- **Grouping & Filtering**: Group by properties and apply filters
- **Top-N Results**: Limit grouped results to top N items
- **Two-Way Filter Binding**: Synchronize filters with parent component
- **Charts**: Optional mini-charts using MudChart
- **Flexible Styling**: Customizable colors and icons

## Basic Usage

### 1. Define Dashboard Metrics

```razor
@code {
    private List<DashboardMetric<VatRateDto>> _dashboardMetrics = new()
    {
        new()
        {
            Title = "Total VAT Rates",
            Type = MetricType.Count,
            Icon = Icons.Material.Outlined.Percent,
            Color = "primary",
            Description = "Total number of VAT rates",
            Format = "N0"
        },
        new()
        {
            Title = "Active VAT Rates",
            Type = MetricType.Count,
            Filter = v => v.Status == VatRateStatus.Active,
            Icon = Icons.Material.Outlined.CheckCircle,
            Color = "success",
            Format = "N0"
        },
        new()
        {
            Title = "Average Percentage",
            Type = MetricType.Average,
            ValueSelector = v => v.Percentage,
            Filter = v => v.Status == VatRateStatus.Active,
            Icon = Icons.Material.Outlined.Analytics,
            Color = "info",
            Format = "N2"
        }
    };
}
```

### 2. Define Filter Definitions (Optional)

```razor
@code {
    private List<DashboardFilterDefinition> _dashboardFilterDefinitions = new()
    {
        new()
        {
            Id = "status",
            Label = "Status",
            Type = FilterType.Select,
            Options = new List<FilterOption>
            {
                new() { Value = "all", Label = "All" },
                new() { Value = "active", Label = "Active" },
                new() { Value = "suspended", Label = "Suspended" }
            },
            DefaultValue = "all"
        },
        new()
        {
            Id = "search",
            Label = "Search",
            Type = FilterType.Text,
            Placeholder = "Search by name..."
        }
    };
}
```

### 3. Use the Component

```razor
@using EventForge.Client.Shared.Components.Dashboard

<ManagementDashboard TItem="VatRateDto"
                     Items="_filteredVatRates"
                     Metrics="_dashboardMetrics"
                     FilterDefinitions="_dashboardFilterDefinitions"
                     @bind-Filters="_dashboardFilters"
                     ShowFilters="true"
                     UseServerSide="false" />
```

## Metric Types

### Count
Counts the number of items.

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Total Items",
    Type = MetricType.Count
}
```

### Sum
Sums a numeric property.

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Total Percentage",
    Type = MetricType.Sum,
    ValueSelector = v => v.Percentage
}
```

### Average
Calculates the average of a numeric property.

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Average Percentage",
    Type = MetricType.Average,
    ValueSelector = v => v.Percentage
}
```

### Min / Max
Finds the minimum or maximum value.

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Maximum Percentage",
    Type = MetricType.Max,
    ValueSelector = v => v.Percentage
}
```

## Filtering

### Applying Filters to Metrics

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Active Items",
    Type = MetricType.Count,
    Filter = v => v.Status == VatRateStatus.Active
}
```

### Filter Types

- **Text**: Text input field
- **Select**: Dropdown with predefined options
- **Date**: Date picker
- **DateRange**: Date range picker
- **Checkbox**: Boolean checkbox
- **Number**: Numeric input

## Grouping

### Group by Property

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Count by Status",
    Type = MetricType.Count,
    GroupBySelector = v => v.Status
}
```

### Top-N Results

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Top 5 by Status",
    Type = MetricType.Count,
    GroupBySelector = v => v.Status,
    TopN = 5
}
```

## Server-Side Metrics

For large datasets or complex calculations, use server-side metric calculation:

```razor
@code {
    private async Task<ServerMetricResponse> CalculateServerMetrics(ServerMetricRequest request)
    {
        // Call your API endpoint
        var response = await HttpClient.PostAsJsonAsync("/api/metrics/calculate", request);
        return await response.Content.ReadFromJsonAsync<ServerMetricResponse>();
    }
}

<ManagementDashboard TItem="VatRateDto"
                     Items="_filteredVatRates"
                     Metrics="_dashboardMetrics"
                     ServerMetricProvider="CalculateServerMetrics"
                     UseServerSide="true" />
```

## Formatting

Use standard .NET format strings:

- `N0`: Integer (no decimals)
- `N2`: Two decimal places
- `C2`: Currency with two decimals
- `P2`: Percentage with two decimals

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Average",
    Type = MetricType.Average,
    ValueSelector = v => v.Percentage,
    Format = "N2"  // 12.34
}
```

## Colors

Available colors (MudBlazor):
- `primary`
- `secondary`
- `success`
- `info`
- `warning`
- `error`
- `dark`
- `tertiary`

## Icons

Use MudBlazor icons:

```csharp
Icon = Icons.Material.Outlined.Analytics
Icon = Icons.Material.Outlined.CheckCircle
Icon = Icons.Material.Outlined.Percent
Icon = Icons.Material.Outlined.TrendingUp
```

## Charts

Enable mini-charts for visual representation:

```csharp
new DashboardMetric<VatRateDto>
{
    Title = "Distribution",
    Type = MetricType.Count,
    GroupBySelector = v => v.Status,
    ShowChart = true
}
```

## Complete Example

See `VatRateManagement.razor` for a complete working example that demonstrates:
- Multiple metric types
- Filter integration
- Two-way binding
- Status-based filtering
- Custom formatting

## API Reference

### ManagementDashboard Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Items` | `IEnumerable<TItem>?` | Items to calculate metrics from (client-side) |
| `Metrics` | `List<DashboardMetric<TItem>>?` | Metric definitions |
| `FilterDefinitions` | `List<DashboardFilterDefinition>?` | Filter definitions |
| `Filters` | `DashboardFilters?` | Current filter values (two-way binding) |
| `FiltersChanged` | `EventCallback<DashboardFilters>` | Event for filter changes |
| `ServerMetricProvider` | `Func<ServerMetricRequest, Task<ServerMetricResponse>>?` | Server-side metric provider |
| `ShowFilters` | `bool` | Whether to show filters (default: true) |
| `UseServerSide` | `bool` | Whether to use server-side calculation (default: false) |

### DashboardMetric<TItem> Properties

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string` | Display name of the metric |
| `Type` | `MetricType` | Type of calculation (Count, Sum, Average, Min, Max) |
| `ValueSelector` | `Expression<Func<TItem, decimal>>?` | Property selector for numeric operations |
| `GroupBySelector` | `Expression<Func<TItem, object>>?` | Property selector for grouping |
| `Filter` | `Func<TItem, bool>?` | Filter to apply before calculation |
| `TopN` | `int` | Number of top items when grouped (0 = all) |
| `Format` | `string?` | Format string for displaying value |
| `Icon` | `string?` | MudBlazor icon |
| `Color` | `string?` | MudBlazor color |
| `Description` | `string?` | Tooltip description |
| `ShowChart` | `bool` | Whether to show a mini chart |

## Performance Considerations

- **Client-Side**: Best for small to medium datasets (< 10,000 items)
- **Server-Side**: Recommended for large datasets or complex calculations
- **Caching**: Consider caching metric results if data doesn't change frequently
- **Lazy Loading**: Load dashboard data on demand if not immediately visible

## Testing

The component includes comprehensive unit tests covering:
- Model functionality
- Filter operations
- Metric calculations
- Grouping logic
- Format handling

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~EventForge.Tests.Components.Dashboard"
```

## Best Practices

1. **Keep Metrics Focused**: Each metric should answer a specific business question
2. **Use Meaningful Names**: Metric titles should be clear and descriptive
3. **Add Descriptions**: Use tooltips to explain what each metric represents
4. **Choose Appropriate Colors**: Use semantic colors (success for positive, warning for attention, etc.)
5. **Format Consistently**: Use consistent formatting across related metrics
6. **Test with Real Data**: Verify metrics with actual production-like data
7. **Optimize Filters**: Only include filters that users actually need

## Troubleshooting

### Metrics not showing
- Verify `Items` is not null or empty
- Check that `Metrics` list is properly configured
- Ensure metric filters don't exclude all items

### Filter not working
- Verify filter IDs match between definition and usage
- Check that `@bind-Filters` is properly set
- Ensure filter values are being applied to data source

### Performance issues
- Consider using server-side calculation for large datasets
- Reduce number of metrics or simplify calculations
- Implement pagination or virtual scrolling for large data

## Future Enhancements

Potential improvements for future versions:
- Custom chart types (line, pie, doughnut)
- Export to CSV/Excel
- Drill-down capabilities
- Time-series metrics
- Comparison with previous periods
- Alert thresholds
- Custom metric renderers
