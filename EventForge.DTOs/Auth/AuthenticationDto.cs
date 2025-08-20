using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Auth
{
    /// <summary>
    /// Login request model.
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// Tenant code for login.
        /// </summary>
        [Required(ErrorMessage = "Tenant code is required.")]
        [MaxLength(50, ErrorMessage = "Tenant code cannot exceed 50 characters.")]
        [Display(Name = "field.tenantCode", Description = "Tenant code for login.")]
        public string TenantCode { get; set; } = string.Empty;

        /// <summary>
        /// Username for login.
        /// </summary>
        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
        [Display(Name = "field.username", Description = "Username for login.")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Password for login.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [Display(Name = "field.password", Description = "Password for login.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Remember this login session.
        /// </summary>
        [Display(Name = "auth.rememberMe", Description = "Remember this login session.")]
        public bool RememberMe { get; set; } = false;
    }

    /// <summary>
    /// Login response model.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// JWT access token.
        /// </summary>
        [Required]
        [Display(Name = "Access Token", Description = "JWT access token.")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Token type (usually "Bearer").
        /// </summary>
        [Required]
        [Display(Name = "Token Type", Description = "Token type.")]
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Token expiration time in seconds.
        /// </summary>
        [Display(Name = "Expires In", Description = "Token expiration time in seconds.")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// User information.
        /// </summary>
        [Required]
        [Display(Name = "User", Description = "User information.")]
        public UserDto User { get; set; } = null!;

        /// <summary>
        /// Tenant information.
        /// </summary>
        [Required]
        [Display(Name = "Tenant", Description = "Tenant information.")]
        public TenantDto Tenant { get; set; } = null!;

        /// <summary>
        /// Indicates if the user must change password.
        /// </summary>
        [Display(Name = "Must Change Password", Description = "Indicates if the user must change password.")]
        public bool MustChangePassword { get; set; } = false;
    }

    /// <summary>
    /// Change password request model.
    /// </summary>
    public class ChangePasswordRequestDto
    {
        /// <summary>
        /// Current password.
        /// </summary>
        [Required(ErrorMessage = "Current password is required.")]
        [Display(Name = "Current Password", Description = "Current password.")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// New password.
        /// </summary>
        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [Display(Name = "New Password", Description = "New password.")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Confirm new password.
        /// </summary>
        [Required(ErrorMessage = "Password confirmation is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password", Description = "Confirm new password.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// User information DTO.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// User ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        [Required]
        [Display(Name = "field.username", Description = "Username.")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "field.email", Description = "Email address.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// First name.
        /// </summary>
        [Required]
        [Display(Name = "field.firstName", Description = "First name.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name.
        /// </summary>
        [Required]
        [Display(Name = "field.lastName", Description = "Last name.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Full name.
        /// </summary>
        [Display(Name = "field.fullName", Description = "Full name.")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Tenant ID this user belongs to.
        /// </summary>
        [Display(Name = "field.tenantId", Description = "Tenant ID this user belongs to.")]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Indicates if the user is active.
        /// </summary>
        [Display(Name = "field.active", Description = "Indicates if the user is active.")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Date of last login.
        /// </summary>
        [Display(Name = "field.lastLogin", Description = "Date of last login.")]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// User roles.
        /// </summary>
        [Display(Name = "field.roles", Description = "User roles.")]
        public IList<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// User permissions.
        /// </summary>
        [Display(Name = "field.permissions", Description = "User permissions.")]
        public IList<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// Date and time when the user was created (UTC).
        /// </summary>
        [Display(Name = "field.createdAt", Description = "Date and time when the user was created (UTC).")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the user was last modified (UTC).
        /// </summary>
        [Display(Name = "field.modifiedAt", Description = "Date and time when the user was last modified (UTC).")]
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>
    /// Tenant information DTO for authentication responses.
    /// </summary>
    public class TenantDto
    {
        /// <summary>
        /// Tenant ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Tenant name.
        /// </summary>
        [Required]
        [Display(Name = "field.name", Description = "Tenant name.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tenant code.
        /// </summary>
        [Required]
        [Display(Name = "field.code", Description = "Tenant code.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name.
        /// </summary>
        [Required]
        [Display(Name = "field.displayName", Description = "Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the tenant is active.
        /// </summary>
        [Display(Name = "field.active", Description = "Indicates if the tenant is active.")]
        public bool IsActive { get; set; }
    }
}