using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.StationMonitor;


/// <summary>
/// Represents a station (e.g., bar, kitchen, cash register, etc.).
/// </summary>
public class Station : AuditableEntity
{
    /// <summary>
    /// Name of the station.
    /// </summary>
    [Required(ErrorMessage = "The station name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name of the station.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the station.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
    [Display(Name = "Description", Description = "Description of the station.")]
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the station.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the station.")]
    public StationStatus Status { get; set; } = StationStatus.Active;

    /// <summary>
    /// Station location (physical or logical).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The location cannot exceed 50 characters.")]
    [Display(Name = "Location", Description = "Physical or logical location of the station.")]
    public string? Location { get; set; }

    /// <summary>
    /// Custom sort order for displaying stations.
    /// </summary>
    [Display(Name = "Sort Order", Description = "Display order of the station.")]
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Additional notes for the station.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes for the station.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Collection of printers assigned to this station.
    /// </summary>
    [Display(Name = "Printers", Description = "Printers assigned to the station.")]
    [InverseProperty(nameof(Printer.Station))]
    public ICollection<Printer> Printers { get; set; } = new List<Printer>();

    // --- Printing & KDS Configuration ---

    /// <summary>
    /// Type of station (e.g. Kitchen, Bar, Cocktail, POS).
    /// </summary>
    [Display(Name = "Station Type", Description = "Functional type of the station.")]
    public StationType StationType { get; set; } = StationType.KDS;

    /// <summary>
    /// Printer assigned to this station for order printing / KDS output.
    /// </summary>
    [Display(Name = "Assigned Printer", Description = "Printer used for order/KDS printing at this station.")]
    public Guid? AssignedPrinterId { get; set; }

    /// <summary>
    /// Navigation property for the assigned printer.
    /// </summary>
    [ForeignKey(nameof(AssignedPrinterId))]
    public Printer? AssignedPrinter { get; set; }

    /// <summary>
    /// If true, items routed to this station also appear on the fiscal receipt.
    /// </summary>
    [Display(Name = "Prints Receipt Copy", Description = "Include items from this station on the fiscal receipt.")]
    public bool PrintsReceiptCopy { get; set; }
}