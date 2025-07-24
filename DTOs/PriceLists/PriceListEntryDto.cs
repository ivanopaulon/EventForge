namespace EventForge.DTOs.PriceLists;

/// <summary>
/// DTO for PriceListEntry output/display operations.
/// </summary>
public class PriceListEntryDto
{
    /// <summary>
    /// Unique identifier for the price list entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the associated product.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Product name (for display purposes).
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Foreign key to the associated price list.
    /// </summary>
    public Guid PriceListId { get; set; }

    /// <summary>
    /// Product price for the price list.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code (e.g., EUR, USD).
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Score assigned to the product.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Indicates if the price is editable from the frontend.
    /// </summary>
    public bool IsEditableInFrontend { get; set; }

    /// <summary>
    /// Indicates if the item can be discounted.
    /// </summary>
    public bool IsDiscountable { get; set; }

    /// <summary>
    /// Status of the price list entry.
    /// </summary>
    public PriceListEntryStatus Status { get; set; }

    /// <summary>
    /// Minimum quantity to apply this price.
    /// </summary>
    public int MinQuantity { get; set; }

    /// <summary>
    /// Maximum quantity to apply this price (0 = no limit).
    /// </summary>
    public int MaxQuantity { get; set; }

    /// <summary>
    /// Additional notes for the price list entry.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Date and time when the price list entry was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the price list entry.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the price list entry was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the price list entry.
    /// </summary>
    public string? ModifiedBy { get; set; }
}