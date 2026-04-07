using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Dashboard;

/// <summary>
/// Represents a user's dashboard configuration.
/// </summary>
public class DashboardConfiguration : AuditableEntity
{
    /// <summary>
    /// Name of the configuration.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name of the configuration.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the configuration.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the configuration.")]
    public string? Description { get; set; }

    /// <summary>
    /// Entity type this configuration applies to (e.g., "VatRate", "Product").
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Entity type cannot exceed 100 characters.")]
    [Display(Name = "Entity Type", Description = "Entity type this configuration applies to.")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default configuration for the entity type.
    /// </summary>
    [Display(Name = "Is Default", Description = "Whether this is the default configuration for the entity type.")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// User ID who owns this configuration.
    /// </summary>
    [Required]
    [Display(Name = "User ID", Description = "User ID who owns this configuration.")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property: The user who owns this configuration.
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property: Metric configurations.
    /// </summary>
    public virtual ICollection<DashboardMetricConfig> Metrics { get; set; } = new List<DashboardMetricConfig>();
}
