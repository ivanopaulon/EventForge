using EventForge.DTOs.Common;
using System;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StoreUserPrivilege output/display operations.
    /// </summary>
    public class StoreUserPrivilegeDto
    {
        /// <summary>
        /// Unique identifier for the store user privilege.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Technical code of the privilege (for programmatic use).
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the privilege.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Privilege category (for grouping).
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Description of the privilege.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Status of the privilege.
        /// </summary>
        public CashierPrivilegeStatus Status { get; set; }

        /// <summary>
        /// Custom sort order for displaying privileges.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Number of groups that have this privilege.
        /// </summary>
        public int GroupCount { get; set; }

        // --- Issue #315: Permission System Fields ---

        /// <summary>
        /// Indicates if this is a system-defined privilege (cannot be deleted).
        /// </summary>
        public bool IsSystemPrivilege { get; set; }

        /// <summary>
        /// Indicates if this privilege should be assigned by default to new groups.
        /// </summary>
        public bool DefaultAssigned { get; set; }

        /// <summary>
        /// Resource that this privilege controls access to (e.g., "products", "sales", "reports").
        /// </summary>
        public string? Resource { get; set; }

        /// <summary>
        /// Action that this privilege permits (e.g., "read", "write", "delete", "manage").
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// Unique permission key in dot notation (e.g., "store.users.manage", "sales.refunds.process").
        /// </summary>
        public string? PermissionKey { get; set; }

        /// <summary>
        /// Date and time when the store user privilege was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the store user privilege.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the store user privilege was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the store user privilege.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
