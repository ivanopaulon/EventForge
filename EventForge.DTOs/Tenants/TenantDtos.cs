using System;
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
}