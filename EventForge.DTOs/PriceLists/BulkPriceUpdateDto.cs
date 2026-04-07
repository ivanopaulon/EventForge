using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// DTO for bulk price update operations.
/// </summary>
public class BulkPriceUpdateDto
{
    /// <summary>
    /// Type of operation to perform (increase, decrease, set, multiply).
    /// </summary>
    [Required(ErrorMessage = "Operation is required.")]
    public BulkUpdateOperation Operation { get; set; }

    /// <summary>
    /// Value for the operation (percentage, amount, multiplier, or fixed price).
    /// </summary>
    [Required(ErrorMessage = "Value is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Value must be greater than zero.")]
    public decimal Value { get; set; }

    /// <summary>
    /// Rounding strategy to apply after calculation.
    /// </summary>
    public RoundingStrategy RoundingStrategy { get; set; } = RoundingStrategy.None;

    /// <summary>
    /// Optional filter: Category IDs to update (null = all categories).
    /// </summary>
    public List<Guid>? CategoryIds { get; set; }

    /// <summary>
    /// Optional filter: Brand IDs to update (null = all brands).
    /// </summary>
    public List<Guid>? BrandIds { get; set; }

    /// <summary>
    /// Optional filter: Specific product IDs to update (null = all products).
    /// </summary>
    public List<Guid>? ProductIds { get; set; }

    /// <summary>
    /// Optional filter: Minimum price threshold (null = no minimum).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "MinPrice must be non-negative.")]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Optional filter: Maximum price threshold (null = no maximum).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "MaxPrice must be non-negative.")]
    public decimal? MaxPrice { get; set; }
}
