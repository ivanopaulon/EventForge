using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Store;


/// <summary>
/// Represents an operator/cashier who can use a cash register.
/// </summary>
public class StoreUser : AuditableEntity
{
    /// <summary>
    /// Display name of the operator/cashier.
    /// </summary>
    [Required(ErrorMessage = "The operator name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Operator Name", Description = "Display name of the operator/cashier.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Username for login.
    /// </summary>
    [Required(ErrorMessage = "The username is required.")]
    [MaxLength(50, ErrorMessage = "The username cannot exceed 50 characters.")]
    [Display(Name = "Username", Description = "Username for login.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the operator.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(100, ErrorMessage = "The email cannot exceed 100 characters.")]
    [Display(Name = "Email", Description = "Email address of the operator.")]
    public string? Email { get; set; }

    /// <summary>
    /// Password hash of the operator.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The password hash cannot exceed 200 characters.")]
    [Display(Name = "Password Hash", Description = "Password hash of the operator.")]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Role or permissions of the operator.
    /// </summary>
    [MaxLength(50, ErrorMessage = "The role cannot exceed 50 characters.")]
    [Display(Name = "Role", Description = "Role or permissions of the operator.")]
    public string? Role { get; set; }

    /// <summary>
    /// Status of the operator.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the operator.")]
    public CashierStatus Status { get; set; } = CashierStatus.Active;

    /// <summary>
    /// Date and time of the last login.
    /// </summary>
    [Display(Name = "Last Login At", Description = "Date and time of the last login.")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Additional notes about the operator.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes about the operator.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Cashier group the operator belongs to.
    /// </summary>
    [Display(Name = "Cashier Group", Description = "Cashier group the operator belongs to.")]
    public Guid? CashierGroupId { get; set; }

    /// <summary>
    /// Navigation property for the cashier group.
    /// </summary>
    public StoreUserGroup? CashierGroup { get; set; }

    // --- Issue #315: Image Management & Extended Fields ---

    /// <summary>
    /// Photo document identifier (references DocumentReference).
    /// </summary>
    [Display(Name = "Photo Document", Description = "Photo document identifier.")]
    public Guid? PhotoDocumentId { get; set; }

    /// <summary>
    /// Photo document navigation property.
    /// </summary>
    public DocumentReference? PhotoDocument { get; set; }

    /// <summary>
    /// Indicates if the operator has given consent for photo storage (GDPR compliance).
    /// </summary>
    [Display(Name = "Photo Consent", Description = "Photo storage consent (GDPR).")]
    public bool PhotoConsent { get; set; } = false;

    /// <summary>
    /// Date and time when photo consent was given.
    /// </summary>
    [Display(Name = "Photo Consent At", Description = "Date and time of photo consent.")]
    public DateTime? PhotoConsentAt { get; set; }

    /// <summary>
    /// Phone number of the operator.
    /// </summary>
    [MaxLength(20, ErrorMessage = "The phone number cannot exceed 20 characters.")]
    [Display(Name = "Phone Number", Description = "Phone number of the operator.")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Date and time of the last password change.
    /// </summary>
    [Display(Name = "Last Password Changed At", Description = "Date and time of last password change.")]
    public DateTime? LastPasswordChangedAt { get; set; }

    /// <summary>
    /// Indicates if two-factor authentication is enabled.
    /// </summary>
    [Display(Name = "Two Factor Enabled", Description = "Two-factor authentication enabled.")]
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// External ID for integration with external authentication providers.
    /// </summary>
    [Display(Name = "External ID", Description = "External authentication provider ID.")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Indicates if the operator is currently on shift.
    /// </summary>
    [Display(Name = "Is On Shift", Description = "Currently on shift.")]
    public bool IsOnShift { get; set; } = false;

    /// <summary>
    /// Current shift identifier (if on shift).
    /// </summary>
    [Display(Name = "Shift ID", Description = "Current shift identifier.")]
    public Guid? ShiftId { get; set; }
}

/// <summary>
/// Status for the cashier.
/// </summary>
public enum CashierStatus
{
    Active,      // Cashier is active and usable
    Suspended,   // Temporarily suspended
    Deleted      // Cashier is deleted/disabled
}