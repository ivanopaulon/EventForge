namespace EventForge.Models.Warehouse;

/// <summary>
/// DTO for StorageFacility output/display operations.
/// </summary>
public class StorageFacilityDto
{
    /// <summary>
    /// Unique identifier for the storage facility.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the warehouse.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the warehouse.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the warehouse.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Contact phone number for the warehouse.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email for the warehouse.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Warehouse manager or responsible person.
    /// </summary>
    public string? Manager { get; set; }

    /// <summary>
    /// Indicates if the warehouse is fiscal.
    /// </summary>
    public bool IsFiscal { get; set; }

    /// <summary>
    /// Additional notes or description for the warehouse.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Total area of the warehouse in square meters.
    /// </summary>
    public int? AreaSquareMeters { get; set; }

    /// <summary>
    /// Maximum storage capacity.
    /// </summary>
    public int? Capacity { get; set; }

    /// <summary>
    /// Indicates if the warehouse is refrigerated.
    /// </summary>
    public bool IsRefrigerated { get; set; }

    /// <summary>
    /// Total number of locations in the warehouse.
    /// </summary>
    public int TotalLocations { get; set; }

    /// <summary>
    /// Number of active locations in the warehouse.
    /// </summary>
    public int ActiveLocations { get; set; }

    /// <summary>
    /// Date and time when the storage facility was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the storage facility.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the storage facility was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the storage facility.
    /// </summary>
    public string? ModifiedBy { get; set; }
}