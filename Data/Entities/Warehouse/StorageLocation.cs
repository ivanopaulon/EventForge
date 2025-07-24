using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Warehouse;

/// <summary>
/// Represents a specific location or area within a warehouse (e.g., shelf, bin, zone).
/// </summary>
public class StorageLocation : AuditableEntity
{
    // --- Identification and General Info ---

    /// <summary>
    /// Location code (unique within the warehouse).
    /// </summary>
    [Required(ErrorMessage = "Location code is required.")]
    [StringLength(30, ErrorMessage = "Location code cannot exceed 30 characters.")]
    [Display(Name = "Location Code", Description = "Unique code for the location within the warehouse.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Description of the location.
    /// </summary>
    [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
    [Display(Name = "Description", Description = "Description of the warehouse location.")]
    public string? Description { get; set; }

    /// <summary>
    /// Reference to the parent warehouse.
    /// </summary>
    [Required(ErrorMessage = "Warehouse is required.")]
    [Display(Name = "Warehouse", Description = "Reference to the parent wareho use.")]
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Parent warehouse navigation property.
    /// </summary>
    [Display(Name = "Warehouse", Description = "Parent warehouse navigation property.")]
    public StorageFacility? Warehouse { get; set; }

    // --- Logistics and Structure ---

    /// <summary>
    /// Maximum capacity of the location (number of items or units).
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Capacity must be non-negative.")]
    [Display(Name = "Capacity", Description = "Maximum capacity of the location.")]
    public int? Capacity { get; set; }

    /// <summary>
    /// Current occupancy of the location (number of items or units).
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Occupancy must be non-negative.")]
    [Display(Name = "Occupancy", Description = "Current occupancy of the location.")]
    public int? Occupancy { get; set; }

    /// <summary>
    /// Date of the last inventory check.
    /// </summary>
    [Display(Name = "Last Inventory Date", Description = "Date of the last inventory check.")]
    public DateTime? LastInventoryDate { get; set; }

    /// <summary>
    /// Indicates if the location is refrigerated.
    /// </summary>
    [Display(Name = "Is Refrigerated", Description = "Indicates if the location is refrigerated.")]
    public bool IsRefrigerated { get; set; }

    /// <summary>
    /// Additional notes or instructions for the location.
    /// </summary>
    [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes or instructions for the location.")]
    public string? Notes { get; set; }

    // --- Structure Details ---

    /// <summary>
    /// Zone or area within the warehouse (e.g., A, B, C).
    /// </summary>
    [StringLength(20, ErrorMessage = "Zone cannot exceed 20 characters.")]
    [Display(Name = "Zone", Description = "Zone or area within the warehouse.")]
    public string? Zone { get; set; }

    /// <summary>
    /// Floor or level of the location (if applicable).
    /// </summary>
    [StringLength(10, ErrorMessage = "Floor cannot exceed 10 characters.")]
    [Display(Name = "Floor", Description = "Floor or level of the location.")]
    public string? Floor { get; set; }

    /// <summary>
    /// Row identifier (e.g., for shelving systems).
    /// </summary>
    [StringLength(10, ErrorMessage = "Row cannot exceed 10 characters.")]
    [Display(Name = "Row", Description = "Row identifier for the location.")]
    public string? Row { get; set; }

    /// <summary>
    /// Column identifier (e.g., for shelving systems).
    /// </summary>
    [StringLength(10, ErrorMessage = "Column cannot exceed 10 characters.")]
    [Display(Name = "Column", Description = "Column identifier for the location.")]
    public string? Column { get; set; }

    /// <summary>
    /// Level identifier (e.g., shelf number).
    /// </summary>
    [StringLength(10, ErrorMessage = "Level cannot exceed 10 characters.")]
    [Display(Name = "Level", Description = "Level or shelf number for the location.")]
    public string? Level { get; set; }
}