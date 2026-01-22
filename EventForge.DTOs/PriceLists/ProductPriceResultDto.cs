using System;
using System.Collections.Generic;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Result DTO containing calculated product price information
/// </summary>
public record ProductPriceResultDto
{
    /// <summary>
    /// Product identifier
    /// </summary>
    public Guid ProductId { get; init; }
    
    /// <summary>
    /// Product name
    /// </summary>
    public string ProductName { get; init; } = string.Empty;
    
    /// <summary>
    /// Product code (SKU, barcode)
    /// </summary>
    public string? ProductCode { get; init; }
    
    /// <summary>
    /// Final calculated price
    /// </summary>
    public decimal FinalPrice { get; init; }
    
    /// <summary>
    /// Currency code (default: EUR)
    /// </summary>
    public string Currency { get; init; } = "EUR";
    
    /// <summary>
    /// Applied price application mode
    /// </summary>
    public PriceApplicationMode AppliedMode { get; init; }
    
    /// <summary>
    /// Applied price list ID (null if manual or no price list used)
    /// </summary>
    public Guid? AppliedPriceListId { get; init; }
    
    /// <summary>
    /// Applied price list name
    /// </summary>
    public string? AppliedPriceListName { get; init; }
    
    /// <summary>
    /// Base price from price list before discounts
    /// </summary>
    public decimal? BasePriceFromPriceList { get; init; }
    
    /// <summary>
    /// Applied discount percentage (BusinessParty global discount)
    /// </summary>
    public decimal? AppliedDiscountPercentage { get; init; }
    
    /// <summary>
    /// Price after discount application
    /// </summary>
    public decimal? PriceAfterDiscount { get; init; }
    
    /// <summary>
    /// Indicates if price was set manually
    /// </summary>
    public bool IsManual { get; init; }
    
    /// <summary>
    /// Indicates if a specific price list was forced
    /// </summary>
    public bool IsPriceListForced { get; init; }
    
    /// <summary>
    /// List of available price lists for this product (for UI selection)
    /// </summary>
    public List<AvailablePriceListDto> AvailablePriceLists { get; init; } = new();
    
    /// <summary>
    /// Search path used to find the price (for debugging/transparency)
    /// </summary>
    public List<string> SearchPath { get; init; } = new();
}
