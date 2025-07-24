using EventForge.Data.Entities.Products;

namespace EventForge.Models.Products;

/// <summary>
/// DTO for ProductUnit output/display operations.
/// </summary>
public class ProductUnitDto
{
    /// <summary>
    /// Unique identifier for the product unit.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Product identifier.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Unit of measure identifier.
    /// </summary>
    public Guid UnitOfMeasureId { get; set; }

    /// <summary>
    /// Conversion factor to the base unit.
    /// </summary>
    public int ConversionFactor { get; set; }

    /// <summary>
    /// Unit type (e.g., Base, Pack, Pallet).
    /// </summary>
    public string UnitType { get; set; } = string.Empty;

    /// <summary>
    /// Additional description for the unit.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Status of the product unit.
    /// </summary>
    public ProductUnitStatus Status { get; set; }

    /// <summary>
    /// Date and time when the product unit was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the product unit.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the product unit was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the product unit.
    /// </summary>
    public string? ModifiedBy { get; set; }
}