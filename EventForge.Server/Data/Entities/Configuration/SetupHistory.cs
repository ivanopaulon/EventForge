using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Configuration;

/// <summary>
/// Represents the history of setup wizard completions.
/// </summary>
public class SetupHistory : AuditableEntity
{
    /// <summary>
    /// Date and time when the setup was completed.
    /// </summary>
    [Required]
    [Display(Name = "Completed At", Description = "Date and time when the setup was completed.")]
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Username or identifier of who completed the setup.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Display(Name = "Completed By", Description = "Username or identifier of who completed the setup.")]
    public string CompletedBy { get; set; } = string.Empty;

    /// <summary>
    /// JSON snapshot of the configuration at the time of setup completion.
    /// </summary>
    [Required]
    [Display(Name = "Configuration Snapshot", Description = "JSON snapshot of the configuration.")]
    public string ConfigurationSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Application version at the time of setup.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Version", Description = "Application version.")]
    public string Version { get; set; } = string.Empty;
}
