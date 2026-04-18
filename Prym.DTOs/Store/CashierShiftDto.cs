using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Store;

/// <summary>
/// Status values for a cashier shift.
/// </summary>
public enum ShiftStatus
{
    /// <summary>Shift is planned but not yet started.</summary>
    Scheduled,

    /// <summary>Shift is currently in progress.</summary>
    InProgress,

    /// <summary>Shift has been completed normally.</summary>
    Completed,

    /// <summary>Shift was cancelled before or during execution.</summary>
    Cancelled
}

/// <summary>
/// DTO for cashier shift output/display operations.
/// </summary>
public class CashierShiftDto
{
    /// <summary>Unique identifier for the shift.</summary>
    public Guid Id { get; set; }

    /// <summary>ID of the store user (operator) assigned to the shift.</summary>
    public Guid StoreUserId { get; set; }

    /// <summary>Display name of the store user.</summary>
    public string StoreUserName { get; set; } = string.Empty;

    /// <summary>Optional ID of the POS (register) assigned for the shift.</summary>
    public Guid? PosId { get; set; }

    /// <summary>Optional display name of the POS.</summary>
    public string? PosName { get; set; }

    /// <summary>UTC start date and time of the shift.</summary>
    public DateTime ShiftStart { get; set; }

    /// <summary>UTC end date and time of the shift.</summary>
    public DateTime ShiftEnd { get; set; }

    /// <summary>Current status of the shift.</summary>
    public ShiftStatus Status { get; set; }

    /// <summary>Optional notes about the shift.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new cashier shift.
/// </summary>
public class CreateCashierShiftDto
{
    /// <summary>ID of the store user (operator) to assign.</summary>
    [Required(ErrorMessage = "A store user must be assigned to the shift.")]
    public Guid StoreUserId { get; set; }

    /// <summary>Optional ID of the POS (register) to assign.</summary>
    public Guid? PosId { get; set; }

    /// <summary>UTC start date and time of the shift.</summary>
    [Required(ErrorMessage = "Shift start date/time is required.")]
    public DateTime ShiftStart { get; set; }

    /// <summary>UTC end date and time of the shift.</summary>
    [Required(ErrorMessage = "Shift end date/time is required.")]
    public DateTime ShiftEnd { get; set; }

    /// <summary>Optional notes about the shift.</summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing cashier shift.
/// </summary>
public class UpdateCashierShiftDto
{
    /// <summary>Optional updated POS (register) assignment.</summary>
    public Guid? PosId { get; set; }

    /// <summary>Updated UTC start date and time.</summary>
    [Required(ErrorMessage = "Shift start date/time is required.")]
    public DateTime ShiftStart { get; set; }

    /// <summary>Updated UTC end date and time.</summary>
    [Required(ErrorMessage = "Shift end date/time is required.")]
    public DateTime ShiftEnd { get; set; }

    /// <summary>Updated shift status.</summary>
    [Required]
    public ShiftStatus Status { get; set; }

    /// <summary>Updated notes.</summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}
