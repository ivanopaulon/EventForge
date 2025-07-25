using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Auth;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : AuditableEntity
{
    /// <summary>
    /// Unique username for the user.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
    [Display(Name = "Username", Description = "Unique username for the user.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    [Required]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [Display(Name = "Email", Description = "User's email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    [Display(Name = "First Name", Description = "User's first name.")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    [Display(Name = "Last Name", Description = "User's last name.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password (Argon2).
    /// </summary>
    [Required]
    [Display(Name = "Password Hash", Description = "Hashed password.")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for password hashing.
    /// </summary>
    [Required]
    [Display(Name = "Password Salt", Description = "Salt used for password hashing.")]
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the user must change password on next login.
    /// </summary>
    [Display(Name = "Must Change Password", Description = "Indicates if the user must change password on next login.")]
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// Date when the password was last changed (UTC).
    /// </summary>
    [Display(Name = "Password Changed At", Description = "Date when the password was last changed (UTC).")]
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>
    /// Number of failed login attempts.
    /// </summary>
    [Display(Name = "Failed Login Attempts", Description = "Number of failed login attempts.")]
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Date when the account was locked (UTC).
    /// </summary>
    [Display(Name = "Locked Until", Description = "Date when the account was locked until (UTC).")]
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Date of last successful login (UTC).
    /// </summary>
    [Display(Name = "Last Login At", Description = "Date of last successful login (UTC).")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Date of last failed login attempt (UTC).
    /// </summary>
    [Display(Name = "Last Failed Login At", Description = "Date of last failed login attempt (UTC).")]
    public DateTime? LastFailedLoginAt { get; set; }

    /// <summary>
    /// Indicates if the user is locked out.
    /// </summary>
    public bool IsLockedOut => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Navigation property: Roles assigned to this user.
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Navigation property: Login history for this user.
    /// </summary>
    public virtual ICollection<LoginAudit> LoginAudits { get; set; } = new List<LoginAudit>();
}