namespace EventForge.Models.Store;

/// <summary>
/// DTO for StoreUser output/display operations.
/// </summary>
public class StoreUserDto
{
    /// <summary>
    /// Unique identifier for the store user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the operator/cashier.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Username for login.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the operator.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Role or permissions of the operator.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Status of the operator.
    /// </summary>
    public CashierStatus Status { get; set; }

    /// <summary>
    /// Date and time of the last login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Additional notes about the operator.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Cashier group the operator belongs to.
    /// </summary>
    public Guid? CashierGroupId { get; set; }

    /// <summary>
    /// Cashier group name (for display purposes).
    /// </summary>
    public string? CashierGroupName { get; set; }

    /// <summary>
    /// Date and time when the store user was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the store user.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the store user was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the store user.
    /// </summary>
    public string? ModifiedBy { get; set; }
}