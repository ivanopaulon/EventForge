using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StoreUserPrivilege update operations.
    /// </summary>
    public class UpdateStoreUserPrivilegeDto
    {
        /// <summary>
        /// Technical code of the privilege (for programmatic use).
        /// </summary>
        [Required(ErrorMessage = "The privilege code is required.")]
        [MaxLength(50, ErrorMessage = "The code cannot exceed 50 characters.")]
        [Display(Name = "Code", Description = "Technical code of the privilege.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the privilege.
        /// </summary>
        [Required(ErrorMessage = "The privilege name is required.")]
        [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
        [Display(Name = "Privilege", Description = "Display name of the privilege.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Privilege category (for grouping).
        /// </summary>
        [MaxLength(50, ErrorMessage = "The category cannot exceed 50 characters.")]
        [Display(Name = "Category", Description = "Category or functional group of the privilege.")]
        public string? Category { get; set; }

        /// <summary>
        /// Description of the privilege.
        /// </summary>
        [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
        [Display(Name = "Description", Description = "Description of the privilege.")]
        public string? Description { get; set; }

        /// <summary>
        /// Status of the privilege.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Current status of the privilege.")]
        public CashierPrivilegeStatus Status { get; set; } = CashierPrivilegeStatus.Active;

        /// <summary>
        /// Custom sort order for displaying privileges.
        /// </summary>
        [Display(Name = "Sort Order", Description = "Display order of the privilege.")]
        public int SortOrder { get; set; } = 0;

        // --- Issue #315: Permission System Fields ---

        /// <summary>
        /// Resource that this privilege controls access to (e.g., "products", "sales", "reports").
        /// </summary>
        [MaxLength(100, ErrorMessage = "The resource cannot exceed 100 characters.")]
        [Display(Name = "Resource", Description = "Resource controlled by this privilege.")]
        public string? Resource { get; set; }

        /// <summary>
        /// Action that this privilege permits (e.g., "read", "write", "delete", "manage").
        /// </summary>
        [MaxLength(50, ErrorMessage = "The action cannot exceed 50 characters.")]
        [Display(Name = "Action", Description = "Action permitted by this privilege.")]
        public string? Action { get; set; }

        /// <summary>
        /// Unique permission key in dot notation (e.g., "store.users.manage", "sales.refunds.process").
        /// </summary>
        [MaxLength(200, ErrorMessage = "The permission key cannot exceed 200 characters.")]
        [Display(Name = "Permission Key", Description = "Unique permission key (e.g., store.users.manage).")]
        public string? PermissionKey { get; set; }
    }
}
