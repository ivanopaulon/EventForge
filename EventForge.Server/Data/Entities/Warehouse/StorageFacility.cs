using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventForge.Server.Data.Entities.Warehouse;


/// <summary>
/// Represents a physical warehouse or storage facility in the domain.
/// This entity contains only domain invariants and business logic that must always be enforced,
/// regardless of the data source (API, UI, import, etc.).
/// </summary>
public class StorageFacility : AuditableEntity
{
    /// <summary>
    /// Name of the warehouse.
    /// </summary>
    [Required(ErrorMessage = "The warehouse name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name of the warehouse.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the warehouse.
    /// </summary>
    [Required(ErrorMessage = "The warehouse code is required.")]
    [MaxLength(30, ErrorMessage = "The code cannot exceed 30 characters.")]
    [Display(Name = "Code", Description = "Unique code for the warehouse.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the warehouse.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The address cannot exceed 200 characters.")]
    [Display(Name = "Address", Description = "Physical address of the warehouse.")]
    public string? Address { get; set; }

    /// <summary>
    /// Contact phone number for the warehouse.
    /// </summary>
    [MaxLength(30, ErrorMessage = "The phone number cannot exceed 30 characters.")]
    [Display(Name = "Phone", Description = "Contact phone number for the warehouse.")]
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email for the warehouse.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The email cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [Display(Name = "Email", Description = "Contact email for the warehouse.")]
    public string? Email { get; set; }

    /// <summary>
    /// Warehouse manager or responsible person.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The manager name cannot exceed 100 characters.")]
    [Display(Name = "Manager", Description = "Warehouse manager or responsible person.")]
    public string? Manager { get; set; }

    /// <summary>
    /// Indicates if the warehouse is fiscal (used for fiscal documents).
    /// </summary>
    [Display(Name = "Is Fiscal", Description = "Indicates if the warehouse is fiscal (used for fiscal documents).")]
    public bool IsFiscal { get; set; }

    /// <summary>
    /// Additional notes or description for the warehouse.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Additional notes or description for the warehouse.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Total area of the warehouse in square meters.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Area must be non-negative.")]
    [Display(Name = "Area (sqm)", Description = "Total area of the warehouse in square meters.")]
    public int? AreaSquareMeters { get; set; }

    /// <summary>
    /// Maximum storage capacity (number of items or pallets).
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Capacity must be non-negative.")]
    [Display(Name = "Capacity", Description = "Maximum storage capacity (number of items or pallets).")]
    public int? Capacity { get; set; }

    /// <summary>
    /// Indicates if the warehouse is refrigerated.
    /// </summary>
    [Display(Name = "Is Refrigerated", Description = "Indicates if the warehouse is refrigerated.")]
    public bool IsRefrigerated { get; set; }

    /// <summary>
    /// List of locations within the warehouse.
    /// </summary>
    [Display(Name = "Locations", Description = "List of locations within the warehouse.")]
    public List<StorageLocation> Locations { get; set; } = new();

    // --- Summary Properties (not mapped to DB) ---

    /// <summary>
    /// Total number of locations in the warehouse.
    /// </summary>
    [NotMapped]
    [Display(Name = "Total Locations", Description = "Total number of locations in the warehouse.")]
    public int TotalLocations => Locations?.Count ?? 0;

    /// <summary>
    /// Number of active locations in the warehouse.
    /// </summary>
    [NotMapped]
    [Display(Name = "Active Locations", Description = "Number of active locations in the warehouse.")]
    public int ActiveLocations => Locations?.Count(l => l.IsActive) ?? 0;

    /// <summary>
    /// Total storage capacity across all locations.
    /// </summary>
    [NotMapped]
    [Display(Name = "Total Capacity", Description = "Total storage capacity across all locations.")]
    public int TotalCapacity => Locations?.Sum(l => l.Capacity ?? 0) ?? 0;

    /// <summary>
    /// Total occupancy across all locations.
    /// </summary>
    [NotMapped]
    [Display(Name = "Total Occupancy", Description = "Total occupancy across all locations.")]
    public int TotalOccupancy => Locations?.Sum(l => l.Occupancy ?? 0) ?? 0;

    /// <summary>
    /// Available capacity (total capacity minus total occupancy).
    /// </summary>
    [NotMapped]
    [Display(Name = "Available Capacity", Description = "Available capacity (total capacity minus total occupancy).")]
    public int AvailableCapacity => TotalCapacity - TotalOccupancy;

    /// <summary>
    /// Number of refrigerated locations in the warehouse.
    /// </summary>
    [NotMapped]
    [Display(Name = "Refrigerated Locations", Description = "Number of refrigerated locations in the warehouse.")]
    public int RefrigeratedLocations => Locations?.Count(l => l.IsRefrigerated) ?? 0;
}