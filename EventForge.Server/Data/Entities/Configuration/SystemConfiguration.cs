using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Audit;

namespace EventForge.Server.Data.Entities.Configuration;

/// <summary>
/// Represents a system configuration setting.
/// </summary>
public class SystemConfiguration : AuditableEntity
{
    /// <summary>
    /// Configuration key (unique identifier).
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Key cannot exceed 100 characters.")]
    [Display(Name = "Key", Description = "Configuration key (unique identifier).")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Configuration value.
    /// </summary>
    [Required]
    [Display(Name = "Value", Description = "Configuration value.")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Description of the configuration setting.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the configuration setting.")]
    public string? Description { get; set; }

    /// <summary>
    /// Configuration category for grouping related settings.
    /// </summary>
    [Required]
    [MaxLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    [Display(Name = "Category", Description = "Configuration category.")]
    public string Category { get; set; } = "General";

    /// <summary>
    /// Indicates if the value is encrypted.
    /// </summary>
    [Display(Name = "Is Encrypted", Description = "Indicates if the value is encrypted.")]
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// Indicates if application restart is required when this setting changes.
    /// </summary>
    [Display(Name = "Requires Restart", Description = "Indicates if application restart is required when this setting changes.")]
    public bool RequiresRestart { get; set; } = false;

    /// <summary>
    /// Indicates if this setting is read-only (system managed).
    /// </summary>
    [Display(Name = "Is Read Only", Description = "Indicates if this setting is read-only.")]
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// Default value for the configuration (for reset purposes).
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Default value cannot exceed 1000 characters.")]
    [Display(Name = "Default Value", Description = "Default value for the configuration.")]
    public string? DefaultValue { get; set; }
}