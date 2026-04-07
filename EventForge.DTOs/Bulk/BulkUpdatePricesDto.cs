using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Bulk;

/// <summary>
/// Request DTO for bulk price updates.
/// </summary>
public class BulkUpdatePricesDto
{
    /// <summary>
    /// List of product IDs to update.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one product ID is required.")]
    [MaxLength(500, ErrorMessage = "Maximum 500 products can be updated at once.")]
    public List<Guid> ProductIds { get; set; } = new();

    /// <summary>
    /// Type of price update operation.
    /// </summary>
    [Required]
    public PriceUpdateType UpdateType { get; set; } = PriceUpdateType.Replace;

    /// <summary>
    /// New price value (used for Replace operation).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
    public decimal? NewPrice { get; set; }

    /// <summary>
    /// Percentage value (used for percentage-based operations).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100.")]
    public decimal? Percentage { get; set; }

    /// <summary>
    /// Amount value (used for amount-based operations).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Amount must be a positive value.")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Optional reason for the price update.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }
}
