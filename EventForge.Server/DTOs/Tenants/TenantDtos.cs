using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.Tenants;

/// <summary>
/// DTO for creating a new tenant.
/// </summary>
public class CreateTenantDto
{
    /// <summary>
    /// Unique tenant name/identifier.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the tenant.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Tenant description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Tenant domain/subdomain (optional).
    /// </summary>
    [MaxLength(100)]
    public string? Domain { get; set; }

    /// <summary>
    /// Contact email for the tenant.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of users allowed for this tenant.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxUsers { get; set; } = 100;

    /// <summary>
    /// Admin user details for the tenant.
    /// </summary>
    [Required]
    public CreateTenantAdminDto AdminUser { get; set; } = new();
}

/// <summary>
/// DTO for creating a tenant admin user.
/// </summary>
public class CreateTenantAdminDto
{
    /// <summary>
    /// Admin username.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Admin email address.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Admin first name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Admin last name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating tenant information.
/// </summary>
public class UpdateTenantDto
{
    /// <summary>
    /// Display name for the tenant.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Tenant description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Tenant domain/subdomain (optional).
    /// </summary>
    [MaxLength(100)]
    public string? Domain { get; set; }

    /// <summary>
    /// Contact email for the tenant.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of users allowed for this tenant.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxUsers { get; set; } = 100;

    /// <summary>
    /// Indicates if the tenant is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Subscription expiry date for the tenant.
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }
}

/// <summary>
/// DTO for tenant response data.
/// </summary>
public class TenantResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Domain { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Admin user details (included only when creating tenant).
    /// </summary>
    public TenantAdminResponseDto? AdminUser { get; set; }
}

/// <summary>
/// DTO for tenant admin user response.
/// </summary>
public class TenantAdminResponseDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public string? GeneratedPassword { get; set; } // Only included when creating
}

/// <summary>
/// DTO for admin tenant mapping response.
/// </summary>
public class AdminTenantResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ManagedTenantId { get; set; }
    public AdminAccessLevel AccessLevel { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for audit trail response.
/// </summary>
public class AuditTrailResponseDto
{
    public Guid Id { get; set; }
    public AuditOperationType OperationType { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string PerformedByUsername { get; set; } = string.Empty;
    public Guid? SourceTenantId { get; set; }
    public string? SourceTenantName { get; set; }
    public Guid? TargetTenantId { get; set; }
    public string? TargetTenantName { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetUsername { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public bool WasSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime PerformedAt { get; set; }
}