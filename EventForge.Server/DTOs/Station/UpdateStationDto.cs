using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.Station;

/// <summary>
/// DTO for Station update operations.
/// </summary>
public class UpdateStationDto
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
}