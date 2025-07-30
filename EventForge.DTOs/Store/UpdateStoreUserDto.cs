using EventForge.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StoreUser update operations.
    /// Contains only fields that can be modified after user creation.
    /// </summary>
    public class UpdateStoreUserDto
    {
        /// <summary>
        /// Display name of the operator/cashier.
        /// </summary>
        [Required(ErrorMessage = "The operator name is required.")]
        [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
        [Display(Name = "Operator Name", Description = "Display name of the operator/cashier.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the operator.
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(100, ErrorMessage = "The email cannot exceed 100 characters.")]
        [Display(Name = "Email", Description = "Email address of the operator.")]
        public string? Email { get; set; }

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

        // Note: Removed fields that should not be updatable:
        // - Username: Should be immutable after creation for security/audit reasons
        // - PasswordHash: Should be handled through dedicated password change endpoints
    }
}
