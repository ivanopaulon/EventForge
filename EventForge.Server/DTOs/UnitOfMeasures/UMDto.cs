namespace EventForge.Server.DTOs.UnitOfMeasures;

/// <summary>
/// DTO for Unit of Measure output/display operations.
/// </summary>
public class UMDto
{
    /// <summary>
    /// Unique identifier for the unit of measure.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the unit of measure (e.g., "Kilogram", "Liter", "Piece").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Symbol of the unit of measure (e.g., "kg", "l", "pcs").
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Description of the unit of measure.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Status of the unit of measure.
    /// </summary>

    /// <summary>
    /// Indicates if this is the default unit of measure.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Date and time when the unit of measure was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the unit of measure.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the unit of measure was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the unit of measure.
    /// </summary>
    public string? ModifiedBy { get; set; }
}