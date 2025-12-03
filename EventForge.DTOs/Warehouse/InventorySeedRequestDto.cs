using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse;

/// <summary>
/// DTO for requesting a bulk inventory seed operation.
/// </summary>
public class InventorySeedRequestDto
{
    /// <summary>
    /// Optional storage location ID to filter products.
    /// If null, uses default location or creates rows without specific location.
    /// </summary>
    public Guid? LocationId { get; set; }

    /// <summary>
    /// Quantity mode: "fixed", "random", or "fromProduct".
    /// </summary>
    [Required]
    public string Mode { get; set; } = "fixed";

    /// <summary>
    /// Fixed quantity value (used when Mode is "fixed").
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Minimum quantity for random mode.
    /// </summary>
    public decimal? MinQuantity { get; set; }

    /// <summary>
    /// Maximum quantity for random mode.
    /// </summary>
    public decimal? MaxQuantity { get; set; }

    /// <summary>
    /// Whether to create an inventory document (header) or just add rows to stock.
    /// </summary>
    public bool CreateDocument { get; set; } = true;

    /// <summary>
    /// Name/description for the inventory document if CreateDocument is true.
    /// </summary>
    public string? DocumentName { get; set; }

    /// <summary>
    /// Number of products to process in each batch for performance optimization.
    /// Default is 500.
    /// </summary>
    public int BatchSize { get; set; } = 500;
}
