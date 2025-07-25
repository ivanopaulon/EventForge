using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Audit;


/// <summary>
/// Represents a log entry for a single field change in an auditable entity.
/// </summary>
public class EntityChangeLog
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    [Key]
    [Display(Name = "ID", Description = "Unique identifier for the log entry.")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the entity class that was changed.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "The entity name cannot exceed 100 characters.")]
    [Display(Name = "Entity", Description = "Name of the entity class that was changed.")]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name for the entity (for UI purposes).
    /// </summary>
    [MaxLength(100, ErrorMessage = "The display name cannot exceed 100 characters.")]
    [Display(Name = "Display Name", Description = "Human-readable name of the entity (optional).")]
    public string? EntityDisplayName { get; set; }

    /// <summary>
    /// Primary key of the entity that was changed.
    /// </summary>
    [Required]
    [Display(Name = "Entity ID", Description = "Identifier of the changed entity.")]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Name of the property that was changed.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "The property name cannot exceed 100 characters.")]
    [Display(Name = "Property", Description = "Name of the changed property.")]
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Type of operation performed (Insert, Update, Delete).
    /// </summary>
    [Required]
    [MaxLength(20, ErrorMessage = "The operation type cannot exceed 20 characters.")]
    [Display(Name = "Operation", Description = "Type of operation (Insert, Update, Delete).")]
    public string OperationType { get; set; } = "Update";

    /// <summary>
    /// Previous value of the property.
    /// </summary>
    [Display(Name = "Old Value", Description = "Previous value of the property.")]
    public string? OldValue { get; set; }

    /// <summary>
    /// New value of the property.
    /// </summary>
    [Display(Name = "New Value", Description = "New value of the property.")]
    public string? NewValue { get; set; }

    /// <summary>
    /// User who performed the change.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "The username cannot exceed 100 characters.")]
    [Display(Name = "Changed By", Description = "User who performed the change.")]
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the change was made (UTC).
    /// </summary>
    [Required]
    [Display(Name = "Changed At", Description = "Date and time when the change was made (UTC).")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}