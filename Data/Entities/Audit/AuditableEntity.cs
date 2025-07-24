using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Audit;

/// <summary>
/// Base class for auditable entities, providing common audit fields and concurrency control.
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    [Key]
    [Display(Name = "ID", Description = "Unique identifier for the entity.")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Date and time when the entity was created (UTC).
    /// </summary>
    [Required]
    [Display(Name = "Created At", Description = "Date and time when the entity was created (UTC).")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the entity.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The username cannot exceed 100 characters.")]
    [Display(Name = "Created By", Description = "User who created the entity.")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the entity was last modified (UTC).
    /// </summary>
    [Display(Name = "Modified At", Description = "Date and time when the entity was last modified (UTC).")]
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the entity.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The username cannot exceed 100 characters.")]
    [Display(Name = "Modified By", Description = "User who last modified the entity.")]
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Indicates whether the entity is soft-deleted.
    /// </summary>
    [Display(Name = "Deleted", Description = "Indicates whether the entity is soft-deleted.")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Date and time when the entity was soft-deleted (UTC).
    /// </summary>
    [Display(Name = "Deleted At", Description = "Date and time when the entity was soft-deleted (UTC).")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who soft-deleted the entity.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The username cannot exceed 100 characters.")]
    [Display(Name = "Deleted By", Description = "User who soft-deleted the entity.")]
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Indicates whether the entity is logically active.
    /// </summary>
    [Display(Name = "Active", Description = "Indicates whether the entity is logically active.")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Token used for optimistic concurrency control.
    /// </summary>
    [Timestamp]
    [Display(Name = "Version", Description = "Token used for optimistic concurrency control.")]
    public byte[]? RowVersion { get; set; }
}