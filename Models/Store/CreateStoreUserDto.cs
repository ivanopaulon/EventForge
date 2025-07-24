using System.ComponentModel.DataAnnotations;
using EventForge.Data.Entities.Store;

namespace EventForge.Models.Store;

/// <summary>
/// DTO for StoreUser creation operations.
/// </summary>
public class CreateStoreUserDto
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
}