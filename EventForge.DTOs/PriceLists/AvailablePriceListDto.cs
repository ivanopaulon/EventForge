using System;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// DTO for representing available price lists for a product
/// </summary>
public record AvailablePriceListDto
{
    /// <summary>
    /// Price list identifier
    /// </summary>
    public Guid PriceListId { get; init; }
    
    /// <summary>
    /// Price list name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Price list priority (higher value = higher priority)
    /// </summary>
    public int Priority { get; init; }
    
    /// <summary>
    /// Price from this price list
    /// </summary>
    public decimal Price { get; init; }
    
    /// <summary>
    /// Indicates if this price list is assigned to the BusinessParty
    /// </summary>
    public bool IsAssignedToBusinessParty { get; init; }
    
    /// <summary>
    /// Indicates if this is the default price list
    /// </summary>
    public bool IsDefault { get; init; }
}
