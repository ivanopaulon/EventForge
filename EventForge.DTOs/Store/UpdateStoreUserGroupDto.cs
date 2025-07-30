using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StoreUserGroup update operations.
    /// </summary>
    public class UpdateStoreUserGroupDto
    {
        /// <summary>
        /// Technical code of the group (for programmatic use).
        /// </summary>
        [Required(ErrorMessage = "The group code is required.")]
        [MaxLength(50, ErrorMessage = "The code cannot exceed 50 characters.")]
        [Display(Name = "Code", Description = "Technical code of the group.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the group.
        /// </summary>
        [Required(ErrorMessage = "The group name is required.")]
        [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
        [Display(Name = "Group Name", Description = "Display name of the cashier group.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the group.
        /// </summary>
        [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
        [Display(Name = "Description", Description = "Description of the group.")]
        public string? Description { get; set; }

        /// <summary>
        /// Status of the group.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Current status of the group.")]
        public CashierGroupStatus Status { get; set; } = CashierGroupStatus.Active;
    }
}
