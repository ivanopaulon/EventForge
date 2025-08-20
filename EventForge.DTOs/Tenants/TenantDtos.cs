using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Tenants
{
    /// <summary>
    /// DTO for creating a new tenant.
    /// </summary>
    public class CreateTenantDto
    {
        [Required(ErrorMessage = "Tenant name is required.")]
        [MaxLength(100, ErrorMessage = "Tenant name cannot exceed 100 characters.")]
        [Display(Name = "field.name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tenant code is required.")]
        [MaxLength(50, ErrorMessage = "Tenant code cannot exceed 50 characters.")]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Tenant code can only contain lowercase letters, numbers, and hyphens.")]
        [Display(Name = "field.code")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Display name is required.")]
        [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters.")]
        [Display(Name = "field.displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "field.description")]
        public string? Description { get; set; }

        [MaxLength(100, ErrorMessage = "Domain cannot exceed 100 characters.")]
        [Display(Name = "field.domain")]
        public string? Domain { get; set; }

        [Required(ErrorMessage = "Contact email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(256, ErrorMessage = "Contact email cannot exceed 256 characters.")]
        [Display(Name = "field.contactEmail")]
        public string ContactEmail { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Max users must be at least 1.")]
        [Display(Name = "field.maxUsers")]
        public int MaxUsers { get; set; } = 100;

        public CreateTenantAdminDto? AdminUser { get; set; }
    }

    /// <summary>
    /// DTO for creating tenant admin user.
    /// </summary>
    public class CreateTenantAdminDto
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating tenant information.
    /// </summary>
    public class UpdateTenantDto
    {
        [Required(ErrorMessage = "Display name is required.")]
        [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters.")]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [MaxLength(100, ErrorMessage = "Domain cannot exceed 100 characters.")]
        public string? Domain { get; set; }

        [Required(ErrorMessage = "Contact email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(256, ErrorMessage = "Contact email cannot exceed 256 characters.")]
        public string ContactEmail { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Max users must be at least 1.")]
        public int MaxUsers { get; set; }



        public DateTime? SubscriptionExpiresAt { get; set; }
    }

    /// <summary>
    /// DTO for tenant response.
    /// </summary>
    public class TenantResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Domain { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public int MaxUsers { get; set; }
        public bool IsActive { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
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
        public string? GeneratedPassword { get; set; }
    }

    /// <summary>
    /// DTO for admin tenant response.
    /// </summary>
    public class AdminTenantResponseDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ManagedTenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string GrantedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for tenant search criteria.
    /// </summary>
    public class TenantSearchDto
    {
        [MaxLength(100)]
        public string? SearchTerm { get; set; }

        public bool? IsActive { get; set; }

        public string? Status { get; set; }

        public bool? NearUserLimit { get; set; }

        public DateTime? CreatedFrom { get; set; }

        public DateTime? CreatedTo { get; set; }

        public DateTime? CreatedAfter { get; set; }

        public DateTime? CreatedBefore { get; set; }

        public int? MinUsers { get; set; }

        public int? MaxUsers { get; set; }

        [MaxLength(100)]
        public string? Domain { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? SortBy { get; set; } = "CreatedAt";

        public string? SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// DTO for detailed tenant information.
    /// </summary>
    public class TenantDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Domain { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public int MaxUsers { get; set; }
        public int CurrentUsers { get; set; }
        public bool IsActive { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public TenantLimitsDto Limits { get; set; } = new TenantLimitsDto();
        public TenantUsageStatsDto? UsageStats { get; set; }
        public List<string> RecentActivities { get; set; } = new List<string>();
        public IEnumerable<TenantAdminResponseDto> Admins { get; set; } = new List<TenantAdminResponseDto>();
    }

    /// <summary>
    /// DTO for tenant limits.
    /// </summary>
    public class TenantLimitsDto
    {
        public Guid TenantId { get; set; }
        public int MaxUsers { get; set; }
        public int CurrentUsers { get; set; }
        public int MaxStorage { get; set; } // In MB
        public long MaxStorageBytes { get; set; }
        public long CurrentStorageBytes { get; set; }
        public int MaxApiCalls { get; set; } // Per day
        public int MaxEventsPerMonth { get; set; }
        public int CurrentEventsThisMonth { get; set; }
        public bool CanCreateSubTenants { get; set; }
        public bool CanAccessReports { get; set; }
        public bool CanExportData { get; set; }
        public int MaxDocuments { get; set; }
        public int MaxProducts { get; set; }
    }

    /// <summary>
    /// DTO for updating tenant limits.
    /// </summary>
    public class UpdateTenantLimitsDto
    {
        [Range(1, int.MaxValue)]
        public int MaxUsers { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxStorage { get; set; } // In MB

        [Range(1, long.MaxValue)]
        public long MaxStorageBytes { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxApiCalls { get; set; } // Per day

        [Range(1, int.MaxValue)]
        public int MaxEventsPerMonth { get; set; }

        public bool CanCreateSubTenants { get; set; }

        public bool CanAccessReports { get; set; }

        public bool CanExportData { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxDocuments { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxProducts { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for tenant usage statistics.
    /// </summary>
    public class TenantUsageStatsDto
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int CurrentUsers { get; set; }
        public int MaxUsers { get; set; }
        public int TotalEvents { get; set; }
        public int EventsThisMonth { get; set; }
        public int CurrentStorage { get; set; } // In MB
        public int MaxStorage { get; set; } // In MB
        public long StorageUsedBytes { get; set; }
        public int TodayApiCalls { get; set; }
        public int MaxApiCalls { get; set; }
        public int DocumentCount { get; set; }
        public int MaxDocuments { get; set; }
        public int ProductCount { get; set; }
        public int MaxProducts { get; set; }
        public double StorageUsagePercentage { get; set; }
        public double UserUsagePercentage { get; set; }
        public double ApiUsagePercentage { get; set; }
        public DateTime? LastActivity { get; set; }
        public int LoginAttemptsToday { get; set; }
        public int FailedLoginsToday { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}