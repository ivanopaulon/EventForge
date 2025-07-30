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
    }

    /// <summary>
    /// DTO for tenant response.
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
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
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
        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for admin tenant response.
    /// </summary>
    public class AdminTenantResponseDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; }
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
        
        public DateTime? CreatedFrom { get; set; }
        
        public DateTime? CreatedTo { get; set; }
        
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
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Domain { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public int MaxUsers { get; set; }
        public int CurrentUsers { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public TenantLimitsDto Limits { get; set; } = new TenantLimitsDto();
        public IEnumerable<TenantAdminResponseDto> Admins { get; set; } = new List<TenantAdminResponseDto>();
    }

    /// <summary>
    /// DTO for tenant limits.
    /// </summary>
    public class TenantLimitsDto
    {
        public int MaxUsers { get; set; }
        public int MaxStorage { get; set; } // In MB
        public int MaxApiCalls { get; set; } // Per day
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
        
        [Range(1, int.MaxValue)]
        public int MaxApiCalls { get; set; } // Per day
        
        public bool CanCreateSubTenants { get; set; }
        
        public bool CanAccessReports { get; set; }
        
        public bool CanExportData { get; set; }
        
        [Range(1, int.MaxValue)]
        public int MaxDocuments { get; set; }
        
        [Range(1, int.MaxValue)]
        public int MaxProducts { get; set; }
    }

    /// <summary>
    /// DTO for tenant usage statistics.
    /// </summary>
    public class TenantUsageStatsDto
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public int CurrentUsers { get; set; }
        public int MaxUsers { get; set; }
        public int CurrentStorage { get; set; } // In MB
        public int MaxStorage { get; set; } // In MB
        public int TodayApiCalls { get; set; }
        public int MaxApiCalls { get; set; }
        public int DocumentCount { get; set; }
        public int MaxDocuments { get; set; }
        public int ProductCount { get; set; }
        public int MaxProducts { get; set; }
        public double StorageUsagePercentage { get; set; }
        public double UserUsagePercentage { get; set; }
        public double ApiUsagePercentage { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}