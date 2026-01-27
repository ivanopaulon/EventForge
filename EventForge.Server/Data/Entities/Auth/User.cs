using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

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

    // --- Profile Image Management ---
    /// <summary>
    /// Avatar document identifier (references DocumentReference).
    /// </summary>
    [Display(Name = "Avatar Document", Description = "Avatar document identifier.")]
    public Guid? AvatarDocumentId { get; set; }

    /// <summary>
    /// Avatar document navigation property.
    /// </summary>
    public DocumentReference? AvatarDocument { get; set; }

    // --- Contact Info ---
    /// <summary>
    /// User's phone number.
    /// </summary>
    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    [Display(Name = "Phone Number", Description = "User's phone number.")]
    public string? PhoneNumber { get; set; }

    // --- Preferences ---
    /// <summary>
    /// User's preferred language code (e.g., "it", "en").
    /// </summary>
    [MaxLength(10, ErrorMessage = "Language code cannot exceed 10 characters.")]
    [Display(Name = "Preferred Language", Description = "User's preferred language code.")]
    public string PreferredLanguage { get; set; } = "it";

    /// <summary>
    /// User's preferred timezone.
    /// </summary>
    [MaxLength(50, ErrorMessage = "Timezone cannot exceed 50 characters.")]
    [Display(Name = "Time Zone", Description = "User's preferred timezone.")]
    public string? TimeZone { get; set; }

    // --- Notification Preferences ---
    /// <summary>
    /// Enable email notifications.
    /// </summary>
    [Display(Name = "Email Notifications", Description = "Enable email notifications.")]
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Enable push notifications.
    /// </summary>
    [Display(Name = "Push Notifications", Description = "Enable push notifications.")]
    public bool PushNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Enable in-app notifications.
    /// </summary>
    [Display(Name = "In-App Notifications", Description = "Enable in-app notifications.")]
    public bool InAppNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Additional metadata in JSON format for extensibility (e.g., notification preferences).
    /// </summary>
    [MaxLength(4000, ErrorMessage = "Metadata cannot exceed 4000 characters.")]
    [Display(Name = "Metadata JSON", Description = "Additional metadata in JSON format.")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Tenant ID this user belongs to.
    /// </summary>
    [Required]
    [Display(Name = "Tenant ID", Description = "Tenant ID this user belongs to.")]
    public new Guid TenantId { get; set; }

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

    /// <summary>
    /// Navigation property: Admin tenant mappings for this user (if super admin).
    /// </summary>
    public virtual ICollection<AdminTenant> AdminTenants { get; set; } = new List<AdminTenant>();

    /// <summary>
    /// Navigation property: The tenant this user belongs to.
    /// </summary>
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Navigation property: Audit trail entries performed by this user.
    /// </summary>
    public virtual ICollection<AuditTrail> PerformedAuditTrails { get; set; } = new List<AuditTrail>();

    /// <summary>
    /// Navigation property: Audit trail entries targeting this user (impersonation).
    /// </summary>
    public virtual ICollection<AuditTrail> TargetedAuditTrails { get; set; } = new List<AuditTrail>();
}