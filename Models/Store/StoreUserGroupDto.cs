using EventForge.Data.Entities.Store;

namespace EventForge.Models.Store;

/// <summary>
/// DTO for StoreUserGroup output/display operations.
/// </summary>
public class StoreUserGroupDto
{
    /// <summary>
    /// Unique identifier for the store user group.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Technical code of the group (for programmatic use).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the group.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Status of the group.
    /// </summary>
    public CashierGroupStatus Status { get; set; }

    /// <summary>
    /// Number of cashiers in this group.
    /// </summary>
    public int CashierCount { get; set; }

    /// <summary>
    /// Number of privileges assigned to this group.
    /// </summary>
    public int PrivilegeCount { get; set; }

    /// <summary>
    /// Date and time when the store user group was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the store user group.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the store user group was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the store user group.
    /// </summary>
    public string? ModifiedBy { get; set; }
}