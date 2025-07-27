using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Tenants
{
    /// <summary>
    /// DTO for creating a new tenant.
    /// </summary>
    public class CreateTenantDto
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "field.name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Display(Name = "field.displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "field.description")]
        public string? Description { get; set; }

        [MaxLength(100)]
        [Display(Name = "field.domain")]
        public string? Domain { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        [Display(Name = "field.contactEmail")]
        public string ContactEmail { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        [Display(Name = "field.maxUsers")]
        public int MaxUsers { get; set; } = 100;
    }

    /// <summary>
    /// DTO for updating tenant information.
    /// </summary>
    public class UpdateTenantDto
    {
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Domain { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string ContactEmail { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
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
}