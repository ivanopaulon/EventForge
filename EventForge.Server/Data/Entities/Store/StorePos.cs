using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Store;


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

    // --- Issue #315: Image Management & Extended Fields ---

    /// <summary>
    /// Image document identifier (references DocumentReference).
    /// </summary>
    [Display(Name = "Image Document", Description = "Image document identifier.")]
    public Guid? ImageDocumentId { get; set; }

    /// <summary>
    /// Image document navigation property.
    /// </summary>
    public DocumentReference? ImageDocument { get; set; }

    /// <summary>
    /// Terminal hardware identifier (e.g., serial number, MAC address).
    /// </summary>
    [MaxLength(100, ErrorMessage = "The terminal identifier cannot exceed 100 characters.")]
    [Display(Name = "Terminal Identifier", Description = "Terminal hardware identifier.")]
    public string? TerminalIdentifier { get; set; }

    /// <summary>
    /// IP address of the POS terminal (supports both IPv4 and IPv6).
    /// </summary>
    [MaxLength(45, ErrorMessage = "The IP address cannot exceed 45 characters.")]
    [Display(Name = "IP Address", Description = "IP address of the POS terminal.")]
    public string? IPAddress { get; set; }

    /// <summary>
    /// Indicates if the POS is currently online/connected.
    /// </summary>
    [Display(Name = "Is Online", Description = "POS is currently online.")]
    public bool IsOnline { get; set; } = false;

    /// <summary>
    /// Date and time of the last synchronization with the server.
    /// </summary>
    [Display(Name = "Last Sync At", Description = "Last synchronization date and time.")]
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Geographical latitude coordinate of the POS location (-90 to 90).
    /// </summary>
    [Display(Name = "Location Latitude", Description = "Geographical latitude (-90 to 90).")]
    public decimal? LocationLatitude { get; set; }

    /// <summary>
    /// Geographical longitude coordinate of the POS location (-180 to 180).
    /// </summary>
    [Display(Name = "Location Longitude", Description = "Geographical longitude (-180 to 180).")]
    public decimal? LocationLongitude { get; set; }

    /// <summary>
    /// Currency code (ISO 4217, e.g., EUR, USD, GBP).
    /// </summary>
    [MaxLength(3, ErrorMessage = "The currency code cannot exceed 3 characters.")]
    [Display(Name = "Currency Code", Description = "ISO 4217 currency code (e.g., EUR, USD).")]
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Time zone identifier (IANA time zone database, e.g., Europe/Rome).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The time zone cannot exceed 50 characters.")]
    [Display(Name = "Time Zone", Description = "IANA time zone (e.g., Europe/Rome).")]
    public string? TimeZone { get; set; }

    // --- Fiscal Printer Support ---

    /// <summary>
    /// Foreign key to the default fiscal printer for this POS.
    /// </summary>
    [Display(Name = "Default Fiscal Printer", Description = "Default fiscal printer for this POS.")]
    public Guid? DefaultFiscalPrinterId { get; set; }

    /// <summary>
    /// Navigation property for the default fiscal printer.
    /// </summary>
    public Printer? DefaultFiscalPrinter { get; set; }
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