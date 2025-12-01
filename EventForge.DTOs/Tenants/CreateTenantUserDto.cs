using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Tenants
{
    /// <summary>
    /// DTO for creating users within a tenant context.
    /// Used by tenant administrators to create new users in their tenant.
    /// </summary>
    public class CreateTenantUserDto
    {
        /// <summary>
        /// Unique username for the user.
        /// </summary>
        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User's email address.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's first name.
        /// </summary>
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name.
        /// </summary>
        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// List of role names to assign to the user.
        /// </summary>
        [Display(Name = "Roles")]
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Whether the user must change password on first login.
        /// </summary>
        [Display(Name = "Must Change Password")]
        public bool MustChangePassword { get; set; } = true;
    }
}
