using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Sales;

/// <summary>
/// Represents a note attached to a sale session.
/// Uses fixed taxonomy configured from backend.
/// </summary>
public class SessionNote : AuditableEntity
{
    /// <summary>
    /// Note unique identifier.
    /// </summary>
    public new Guid Id { get; set; }

    /// <summary>
    /// Reference to the sale session.
    /// </summary>
    [Required]
    public Guid SaleSessionId { get; set; }

    /// <summary>
    /// Sale session navigation property.
    /// </summary>
    public SaleSession? SaleSession { get; set; }

    /// <summary>
    /// Note flag/category identifier (from backend taxonomy).
    /// </summary>
    [Required]
    public Guid NoteFlagId { get; set; }

    /// <summary>
    /// Note flag navigation property.
    /// </summary>
    public NoteFlag? NoteFlag { get; set; }

    /// <summary>
    /// Note text (free text).
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Created by user identifier.
    /// </summary>
    [Required]
    public Guid CreatedByUserId { get; set; }
}

/// <summary>
/// Represents a note flag/category with visual attributes.
/// Configured from backend admin interface.
/// </summary>
public class NoteFlag : AuditableEntity
{
    /// <summary>
    /// Flag unique identifier.
    /// </summary>
    public new Guid Id { get; set; }

    /// <summary>
    /// Flag code (e.g., URGENT, ALLERGY, SPECIAL_REQUEST).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the flag.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the flag.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Color for UI display (hex code).
    /// </summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// Icon identifier for UI display.
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Indicates if this flag is active.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }
}
