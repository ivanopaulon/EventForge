using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists
{

/// <summary>
/// DTO representing price history for a product across different price lists and time periods.
/// Part of Issue #245 price optimization implementation.
/// </summary>
public class PriceHistoryDto
{
    /// <summary>
    /// Product identifier.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Price list identifier that provided this price.
    /// </summary>
    public Guid PriceListId { get; set; }

    /// <summary>
    /// Name of the price list.
    /// </summary>
    public string PriceListName { get; set; } = string.Empty;

    /// <summary>
    /// Price value.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Start date when this price became effective.
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// End date when this price stopped being effective.
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Priority of the price list.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this was the default price list.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Date when this price entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created this price entry.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date when this price entry was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified this price entry.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Minimum quantity for this price tier.
    /// </summary>
    public int MinQuantity { get; set; } = 1;

    /// <summary>
    /// Maximum quantity for this price tier (0 = unlimited).
    /// </summary>
    public int MaxQuantity { get; set; } = 0;

    /// <summary>
    /// Whether this price was active during the evaluation period.
    /// </summary>
    public bool WasActive { get; set; }

    /// <summary>
    /// Notes or reason for price change (if available).
    /// </summary>
    public string? Notes { get; set; }
    }
}