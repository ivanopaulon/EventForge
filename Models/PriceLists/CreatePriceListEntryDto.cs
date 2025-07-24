using System.ComponentModel.DataAnnotations;

namespace EventForge.Models.PriceLists;

/// <summary>
/// DTO for PriceListEntry creation operations.
/// </summary>
public class CreatePriceListEntryDto
{
    /// <summary>
    /// Foreign key to the associated product.
    /// </summary>
    [Required]
    [Display(Name = "Product", Description = "Identifier of the product.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Foreign key to the associated price list.
    /// </summary>
    [Required]
    [Display(Name = "Price List", Description = "Identifier of the price list.")]
    public Guid PriceListId { get; set; }

    /// <summary>
    /// Product price for the price list.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "The price must be greater than or equal to zero.")]
    [Display(Name = "Price", Description = "Product price for the price list.")]
    public decimal Price { get; set; } = 0m;

    /// <summary>
    /// Currency code (e.g., EUR, USD).
    /// </summary>
    [MaxLength(3, ErrorMessage = "The currency code cannot exceed 3 characters.")]
    [Display(Name = "Currency", Description = "Currency code (ISO 4217).")]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Score assigned to the product.
    /// </summary>
    [Range(0, 100, ErrorMessage = "The score must be between 0 and 100.")]
    [Display(Name = "Score", Description = "Score assigned to the product.")]
    public int Score { get; set; } = 0;

    /// <summary>
    /// Indicates if the price is editable from the frontend.
    /// </summary>
    [Display(Name = "Editable in Frontend", Description = "Indicates if the price is editable from the frontend.")]
    public bool IsEditableInFrontend { get; set; } = false;

    /// <summary>
    /// Indicates if the item can be discounted.
    /// </summary>
    [Display(Name = "Discountable", Description = "Indicates if the item can be discounted.")]
    public bool IsDiscountable { get; set; } = true;

    /// <summary>
    /// Status of the price list entry.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the price list entry.")]
    public PriceListEntryStatus Status { get; set; } = PriceListEntryStatus.Attivo;

    /// <summary>
    /// Minimum quantity to apply this price.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "The minimum quantity must be at least 1.")]
    [Display(Name = "Minimum Quantity", Description = "Minimum quantity to apply this price.")]
    public int MinQuantity { get; set; } = 1;

    /// <summary>
    /// Maximum quantity to apply this price (0 = no limit).
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "The maximum quantity must be greater than or equal to zero.")]
    [Display(Name = "Maximum Quantity", Description = "Maximum quantity to apply this price (0 = no limit).")]
    public int MaxQuantity { get; set; } = 0;

    /// <summary>
    /// Additional notes for the price list entry.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Additional notes for the price list entry.")]
    public string? Notes { get; set; }
}