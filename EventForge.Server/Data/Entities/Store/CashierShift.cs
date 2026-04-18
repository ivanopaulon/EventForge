using System.ComponentModel.DataAnnotations;
using Prym.DTOs.Store;

namespace EventForge.Server.Data.Entities.Store;

/// <summary>
/// Represents a scheduled or active shift for a cash register operator.
/// </summary>
public class CashierShift : AuditableEntity
{
    /// <summary>
    /// FK to the store user (operator/cashier) assigned to this shift.
    /// </summary>
    [Required]
    [Display(Name = "Store User ID", Description = "Operator assigned to the shift.")]
    public Guid StoreUserId { get; set; }

    /// <summary>
    /// Navigation property for the assigned store user.
    /// </summary>
    public StoreUser StoreUser { get; set; } = null!;

    /// <summary>
    /// Optional FK to the POS (cash register) assigned for this shift.
    /// </summary>
    [Display(Name = "POS ID", Description = "Cash register assigned for this shift.")]
    public Guid? PosId { get; set; }

    /// <summary>
    /// Navigation property for the assigned POS.
    /// </summary>
    public StorePos? Pos { get; set; }

    /// <summary>
    /// UTC date and time when the shift starts.
    /// </summary>
    [Required]
    [Display(Name = "Shift Start", Description = "UTC date and time when the shift starts.")]
    public DateTime ShiftStart { get; set; }

    /// <summary>
    /// UTC date and time when the shift ends.
    /// </summary>
    [Required]
    [Display(Name = "Shift End", Description = "UTC date and time when the shift ends.")]
    public DateTime ShiftEnd { get; set; }

    /// <summary>
    /// Current status of the shift.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the shift.")]
    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;

    /// <summary>
    /// Optional notes or remarks about the shift.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Optional notes about the shift.")]
    public string? Notes { get; set; }
}
