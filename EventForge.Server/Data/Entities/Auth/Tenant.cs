using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Represents a tenant in the multi-tenant system.
/// </summary>
public class Tenant : AuditableEntity
{
    /// <summary>
    /// Unique tenant name/identifier.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Tenant name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Unique tenant name/identifier.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the tenant.
    /// </summary>
    [Required]
    [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters.")]
    [Display(Name = "Display Name", Description = "Display name for the tenant.")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Tenant description.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Tenant description.")]
    public string? Description { get; set; }

    /// <summary>
    /// Tenant domain/subdomain (optional).
    /// </summary>
    [MaxLength(100, ErrorMessage = "Domain cannot exceed 100 characters.")]
    [Display(Name = "Domain", Description = "Tenant domain/subdomain.")]
    public string? Domain { get; set; }

    /// <summary>
    /// Contact email for the tenant.
    /// </summary>
    [Required]
    [MaxLength(256, ErrorMessage = "Contact email cannot exceed 256 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [Display(Name = "Contact Email", Description = "Contact email for the tenant.")]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of users allowed for this tenant.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Max users must be at least 1.")]
    [Display(Name = "Max Users", Description = "Maximum number of users allowed for this tenant.")]
    public int MaxUsers { get; set; } = 100;

    /// <summary>
    /// Indicates if the tenant is enabled and can be accessed.
    /// </summary>
    [Display(Name = "Is Enabled", Description = "Indicates if the tenant is enabled and can be accessed.")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Date when the tenant subscription expires (UTC).
    /// </summary>
    [Display(Name = "Subscription Expires At", Description = "Date when the tenant subscription expires (UTC).")]
    public DateTime? SubscriptionExpiresAt { get; set; }

    /// <summary>
    /// Navigation property: Admin tenants mapping super admins to this tenant.
    /// </summary>
    public virtual ICollection<AdminTenant> AdminTenants { get; set; } = new List<AdminTenant>();

    /// <summary>
    /// Navigation property: Licenses assigned to this tenant.
    /// </summary>
    public virtual ICollection<TenantLicense> TenantLicenses { get; set; } = new List<TenantLicense>();
}