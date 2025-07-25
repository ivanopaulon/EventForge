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
}

/// <summary>
/// Status for the operator/cashier.
/// </summary>
public enum CashierStatus
{
    Active,     // Operator is active
    Suspended,  // Operator is temporarily suspended
    Locked,     // Operator is locked for security or administrative reasons
    Deleted     // Operator is deleted/disabled
}