using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a physical or virtual point of sale (POS).
/// </summary>
public class StorePos : AuditableEntity
{
    /// <summary>
    /// Name or identifier code of the POS.
    /// </summary>
    [Required(ErrorMessage = "The POS name is required.")]
    [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
    [Display(Name = "POS Name", Description = "Name or identifier code of the POS.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the POS.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
    [Display(Name = "Description", Description = "Description of the POS.")]
    public string? Description { get; set; }

    /// <summary>
    /// Status of the POS.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the POS.")]
    public CashRegisterStatus Status { get; set; } = CashRegisterStatus.Active;

    /// <summary>
    /// Physical or virtual location of the POS.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The location cannot exceed 100 characters.")]
    [Display(Name = "Location", Description = "Physical or virtual location of the POS.")]
    public string? Location { get; set; }

    /// <summary>
    /// Date and time of the last opening of the POS.
    /// </summary>
    [Display(Name = "Last Opened At", Description = "Date and time of the last opening of the POS.")]
    public DateTime? LastOpenedAt { get; set; }

    /// <summary>
    /// Additional notes about the POS.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes about the POS.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Receipts produced by this POS.
    /// </summary>
    [Display(Name = "Receipts", Description = "Receipts produced by this POS.")]
    public ICollection<DocumentHeader> Receipts { get; set; } = new List<DocumentHeader>();
}

/// <summary>
/// Status for the POS.
/// </summary>
public enum CashRegisterStatus
{
    Active,         // POS is active and usable
    Suspended,      // Temporarily suspended
    Maintenance,    // Under maintenance
    Disabled        // Disabled/not usable
}