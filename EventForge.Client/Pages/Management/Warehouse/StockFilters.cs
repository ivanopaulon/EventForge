namespace EventForge.Client.Pages.Management.Warehouse;

/// <summary>
/// Manages filter state for StockOverview page
/// </summary>
public class StockFilters
{
    public string SearchTerm { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    public bool ShowOnlyLowStock { get; set; }
    public bool ShowOnlyCritical { get; set; }
    public bool ShowOnlyOutOfStock { get; set; }
    public bool ShowOnlyInStock { get; set; }
    public bool ShowAllProducts { get; set; }
    public bool DetailedView { get; set; } = true;
    
    /// <summary>
    /// Indicates if any filter is currently active
    /// </summary>
    public bool HasActiveFilters => 
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        WarehouseId.HasValue ||
        LocationId.HasValue ||
        ShowOnlyLowStock ||
        ShowOnlyCritical ||
        ShowOnlyOutOfStock ||
        ShowOnlyInStock ||
        ShowAllProducts;
    
    /// <summary>
    /// Returns count of active filters for UI badge
    /// </summary>
    public int ActiveFilterCount
    {
        get
        {
            int count = 0;
            if (!string.IsNullOrWhiteSpace(SearchTerm)) count++;
            if (WarehouseId.HasValue) count++;
            if (LocationId.HasValue) count++;
            if (ShowOnlyLowStock) count++;
            if (ShowOnlyCritical) count++;
            if (ShowOnlyOutOfStock) count++;
            if (ShowOnlyInStock) count++;
            if (ShowAllProducts) count++;
            return count;
        }
    }
    
    /// <summary>
    /// Clears all filters to default state
    /// </summary>
    public void Clear()
    {
        SearchTerm = string.Empty;
        WarehouseId = null;
        LocationId = null;
        ShowOnlyLowStock = false;
        ShowOnlyCritical = false;
        ShowOnlyOutOfStock = false;
        ShowOnlyInStock = false;
        ShowAllProducts = false;
        DetailedView = true;
    }
}
