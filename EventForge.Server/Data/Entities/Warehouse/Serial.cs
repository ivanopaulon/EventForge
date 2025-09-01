using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents an individual serial number/matricola for specific product units.
/// Used for unique identification and tracking of individual items.
/// </summary>
public class Serial : AuditableEntity
{
    /// <summary>
    /// Unique serial number/matricola.
    /// </summary>
    [Required(ErrorMessage = "Serial number is required.")]
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters.")]
    [Display(Name = "Serial Number", Description = "Unique serial number/matricola.")]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Product this serial belongs to.
    /// </summary>
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product", Description = "Product this serial belongs to.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Lot this serial belongs to (optional, for lot-managed products).
    /// </summary>
    [Display(Name = "Lot", Description = "Lot this serial belongs to.")]
    public Guid? LotId { get; set; }

    /// <summary>
    /// Navigation property for the lot.
    /// </summary>
    public Lot? Lot { get; set; }

    /// <summary>
    /// Current location of this serial.
    /// </summary>
    [Display(Name = "Location", Description = "Current location of this serial.")]
    public Guid? CurrentLocationId { get; set; }

    /// <summary>
    /// Navigation property for the current location.
    /// </summary>
    public StorageLocation? CurrentLocation { get; set; }

    /// <summary>
    /// Status of the serial.
    /// </summary>
    [Display(Name = "Status", Description = "Current status of the serial.")]
    public SerialStatus Status { get; set; } = SerialStatus.Available;

    /// <summary>
    /// Manufacturing date for this specific serial.
    /// </summary>
    [Display(Name = "Manufacturing Date", Description = "Manufacturing date for this specific serial.")]
    public DateTime? ManufacturingDate { get; set; }

    /// <summary>
    /// Warranty expiry date for this serial.
    /// </summary>
    [Display(Name = "Warranty Expiry", Description = "Warranty expiry date for this serial.")]
    public DateTime? WarrantyExpiry { get; set; }

    /// <summary>
    /// Customer who owns this serial (if sold).
    /// </summary>
    [Display(Name = "Owner", Description = "Customer who owns this serial.")]
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Navigation property for the owner.
    /// </summary>
    public BusinessParty? Owner { get; set; }

    /// <summary>
    /// Date when this serial was sold/assigned.
    /// </summary>
    [Display(Name = "Sale Date", Description = "Date when this serial was sold/assigned.")]
    public DateTime? SaleDate { get; set; }

    /// <summary>
    /// Notes or additional information about the serial.
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes", Description = "Notes or additional information about the serial.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Barcode for this specific serial.
    /// </summary>
    [StringLength(50, ErrorMessage = "Barcode cannot exceed 50 characters.")]
    [Display(Name = "Barcode", Description = "Barcode for this specific serial.")]
    public string? Barcode { get; set; }

    /// <summary>
    /// RFID tag identifier for this serial.
    /// </summary>
    [StringLength(50, ErrorMessage = "RFID tag cannot exceed 50 characters.")]
    [Display(Name = "RFID Tag", Description = "RFID tag identifier for this serial.")]
    public string? RfidTag { get; set; }

    /// <summary>
    /// Stock movements involving this serial.
    /// </summary>
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    /// <summary>
    /// Maintenance records for this serial.
    /// </summary>
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}

/// <summary>
/// Status for individual serials.
/// </summary>
public enum SerialStatus
{
    Available,    // Serial is available in stock
    Sold,         // Serial has been sold
    InUse,        // Serial is currently in use
    Maintenance,  // Serial is under maintenance
    Defective,    // Serial is defective
    Recalled,     // Serial has been recalled
    Scrapped      // Serial has been scrapped/disposed
}