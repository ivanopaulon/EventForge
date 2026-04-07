using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Request DTO for calculating product price with flexible application modes
/// </summary>
public record GetProductPriceRequestDto
{
    /// <summary>
    /// Product identifier
    /// </summary>
    [Required]
    public required Guid ProductId { get; init; }

    /// <summary>
    /// Optional business party for specific pricing and discounts
    /// </summary>
    public Guid? BusinessPartyId { get; init; }

    /// <summary>
    /// Quantity for price calculation (default: 1)
    /// </summary>
    public int Quantity { get; init; } = 1;

    /// <summary>
    /// Price application mode override (if not specified, uses BusinessParty default or Automatic)
    /// </summary>
    public PriceApplicationMode? PriceApplicationMode { get; init; }

    /// <summary>
    /// Forced price list ID (required when PriceApplicationMode is ForcedPriceList)
    /// </summary>
    public Guid? ForcedPriceListId { get; init; }

    /// <summary>
    /// Manual price (required when PriceApplicationMode is Manual)
    /// </summary>
    public decimal? ManualPrice { get; init; }

    /// <summary>
    /// Reference date for price validity evaluation (default: current UTC)
    /// </summary>
    public DateTime? ReferenceDate { get; init; }
}
