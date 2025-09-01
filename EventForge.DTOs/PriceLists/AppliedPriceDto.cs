using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists
{
    /// <summary>
    /// DTO representing the applied price for a product after precedence logic and unit conversion.
    /// Part of Issue #245 price optimization implementation.
    /// </summary>
    public class AppliedPriceDto
    {
    /// <summary>
    /// Product identifier for which the price was calculated.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Event identifier where the price applies.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Final calculated price per unit.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code for the price.
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Unit of measure identifier for the price.
    /// </summary>
    public Guid UnitOfMeasureId { get; set; }

    /// <summary>
    /// Unit of measure name for display purposes.
    /// </summary>
    public string UnitOfMeasureName { get; set; } = string.Empty;

    /// <summary>
    /// Symbol of the unit of measure.
    /// </summary>
    public string UnitSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Conversion factor used for unit conversion (if any).
    /// </summary>
    public decimal? ConversionFactor { get; set; }

    /// <summary>
    /// Original price before unit conversion (if conversion was applied).
    /// </summary>
    public decimal? OriginalPrice { get; set; }

    /// <summary>
    /// Original unit of measure identifier before conversion (if conversion was applied).
    /// </summary>
    public Guid? OriginalUnitOfMeasureId { get; set; }

    /// <summary>
    /// Price list identifier that provided this price.
    /// </summary>
    public Guid PriceListId { get; set; }

    /// <summary>
    /// Name of the price list that provided this price.
    /// </summary>
    public string PriceListName { get; set; } = string.Empty;

    /// <summary>
    /// Priority of the price list that provided this price.
    /// </summary>
    public int PriceListPriority { get; set; }

    /// <summary>
    /// Quantity range this price applies to.
    /// </summary>
    public int MinQuantity { get; set; } = 1;

    /// <summary>
    /// Maximum quantity this price applies to (0 = unlimited).
    /// </summary>
    public int MaxQuantity { get; set; } = 0;

    /// <summary>
    /// Date when this price calculation was performed.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this price is editable in the frontend.
    /// </summary>
    public bool IsEditableInFrontend { get; set; }

    /// <summary>
    /// Whether this price is discountable.
    /// </summary>
    public bool IsDiscountable { get; set; } = true;

    /// <summary>
    /// Score assigned to the product in this price list.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Whether unit conversion was applied to calculate this price.
    /// </summary>
    public bool WasUnitConverted => ConversionFactor.HasValue && OriginalPrice.HasValue;

    /// <summary>
    /// Explanation of how this price was calculated (for debugging/transparency).
    /// </summary>
    public string? CalculationNotes { get; set; }
    }
}