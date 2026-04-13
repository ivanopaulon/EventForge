using Prym.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.PriceLists;

/// <summary>
/// Request DTO for resolving prices for multiple products in a single batch call.
/// </summary>
public class BatchPriceResolutionRequest
{
    /// <summary>
    /// List of price resolution items to process in batch.
    /// Maximum 100 items per request to prevent abuse.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    [MaxLength(100, ErrorMessage = "Cannot resolve more than 100 prices in a single batch.")]
    public List<BatchPriceResolutionItem> Items { get; set; } = new();
}

/// <summary>
/// A single item in a batch price resolution request.
/// </summary>
public class BatchPriceResolutionItem
{
    /// <summary>
    /// Unique key to identify this item in the response (e.g., row index or product ID string).
    /// Allows the caller to correlate response items with request items even when
    /// the same productId appears multiple times with different contexts.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// ID of the product to resolve the price for.
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Optional document header ID (to get the price list forced on the document).
    /// </summary>
    public Guid? DocumentHeaderId { get; set; }

    /// <summary>
    /// Optional business party ID (to get the default price list for the customer/supplier).
    /// </summary>
    public Guid? BusinessPartyId { get; set; }

    /// <summary>
    /// Optional forced price list ID (overrides all other price lists).
    /// </summary>
    public Guid? ForcedPriceListId { get; set; }

    /// <summary>
    /// Price list direction: Input = purchase, Output = sales.
    /// </summary>
    public PriceListDirection? Direction { get; set; }

    /// <summary>
    /// Quantity for MinQuantity/MaxQuantity bracket filtering. Defaults to 1.
    /// </summary>
    public decimal Quantity { get; set; } = 1m;

    /// <summary>
    /// Optional unit of measure ID to filter price list entries by UoM.
    /// When specified, entries matching this UoM are preferred; falls back to entries without UoM if none found.
    /// </summary>
    public Guid? UnitOfMeasureId { get; set; }
}

/// <summary>
/// Response DTO for a batch price resolution request.
/// Maps each request Key to its resolved PriceResolutionResult.
/// </summary>
public class BatchPriceResolutionResponse
{
    /// <summary>
    /// Dictionary mapping each request Key to the resolved price result.
    /// </summary>
    public Dictionary<string, PriceResolutionResult> Results { get; set; } = new();

    /// <summary>
    /// Keys for which resolution failed (e.g., product not found).
    /// </summary>
    public List<BatchPriceResolutionError> Errors { get; set; } = new();

    /// <summary>
    /// Total number of items processed.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Total number of items resolved successfully.
    /// </summary>
    public int TotalSucceeded { get; set; }

    /// <summary>
    /// Total number of items that failed.
    /// </summary>
    public int TotalFailed { get; set; }
}

/// <summary>
/// Represents a failed item in a batch price resolution.
/// </summary>
public class BatchPriceResolutionError
{
    /// <summary>
    /// Key of the item that failed.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Product ID of the item that failed.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Error message describing why resolution failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
