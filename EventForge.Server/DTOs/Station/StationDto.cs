namespace EventForge.Server.DTOs.Station;

/// <summary>
/// DTO for Station output/display operations.
/// </summary>
public class StationDto
{
    /// <summary>
    /// Unique identifier for the station.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the station.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the station.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the station.
    /// </summary>
    public StationStatus Status { get; set; }

    /// <summary>
    /// Station location (physical or logical).
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Custom sort order for displaying stations.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Additional notes for the station.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Number of printers assigned to this station.
    /// </summary>
    public int PrinterCount { get; set; }

    /// <summary>
    /// Date and time when the station was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the station.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the station was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the station.
    /// </summary>
    public string? ModifiedBy { get; set; }
}