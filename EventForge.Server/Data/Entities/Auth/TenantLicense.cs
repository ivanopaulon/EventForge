using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Associates a license with a tenant.
/// </summary>
public class TenantLicense : AuditableEntity
{
    /// <summary>
    /// Foreign key to the tenant.
    /// </summary>
    [Required]
    [Display(Name = "Target Tenant ID", Description = "Tenant this license is assigned to.")]
    public Guid TargetTenantId { get; set; }

    /// <summary>
    /// Foreign key to the license.
    /// </summary>
    [Required]
    [Display(Name = "License ID", Description = "License assigned to the tenant.")]
    public Guid LicenseId { get; set; }

    /// <summary>
    /// Date when the license becomes active (UTC).
    /// </summary>
    [Required]
    [Display(Name = "Starts At", Description = "Date when the license becomes active (UTC).")]
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the license expires (UTC).
    /// </summary>
    [Display(Name = "Expires At", Description = "Date when the license expires (UTC).")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Indicates if the license assignment is currently active.
    /// </summary>
    [Display(Name = "License Assignment Active", Description = "Indicates if the license assignment is currently active.")]
    public bool IsLicenseActive { get; set; } = true;

    /// <summary>
    /// Number of API calls made this month.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "API calls must be non-negative.")]
    [Display(Name = "API Calls This Month", Description = "Number of API calls made this month.")]
    public int ApiCallsThisMonth { get; set; } = 0;

    /// <summary>
    /// Date when API call count was last reset (UTC).
    /// </summary>
    [Display(Name = "API Calls Reset At", Description = "Date when API call count was last reset (UTC).")]
    public DateTime ApiCallsResetAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the license is currently valid and active.
    /// </summary>
    public bool IsValid => IsLicenseActive && 
                          DateTime.UtcNow >= StartsAt && 
                          (!ExpiresAt.HasValue || DateTime.UtcNow <= ExpiresAt.Value);

    /// <summary>
    /// Navigation property: Tenant this license is assigned to.
    /// </summary>
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Navigation property: License assigned to the tenant.
    /// </summary>
    public virtual License License { get; set; } = null!;
}