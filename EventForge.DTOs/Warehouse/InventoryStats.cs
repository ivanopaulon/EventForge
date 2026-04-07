namespace EventForge.DTOs.Warehouse;

/// <summary>
/// Statistics about an inventory document.
/// </summary>
public class InventoryStats
{
    /// <summary>
    /// Number of unique products in the inventory.
    /// </summary>
    public int UniqueProducts { get; set; }

    /// <summary>
    /// Number of unique locations in the inventory.
    /// </summary>
    public int UniqueLocations { get; set; }

    /// <summary>
    /// Estimated time to load the document in seconds.
    /// </summary>
    public double EstimatedLoadTimeSeconds { get; set; }
}
