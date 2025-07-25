using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.Warehouse;

/// <summary>
/// DTO for creating a new storage facility.
/// </summary>
public class CreateStorageFacilityDto
{
    /// <summary>
    /// Name of the warehouse.
    /// </summary>
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the warehouse.
    /// </summary>
    [Required(ErrorMessage = "Code is required.")]
    [MaxLength(30, ErrorMessage = "Code cannot exceed 30 characters.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the warehouse.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public string? Address { get; set; }

    /// <summary>
    /// Contact phone number for the warehouse.
    /// </summary>
    [MaxLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email for the warehouse.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }

    /// <summary>
    /// Warehouse manager or responsible person.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Manager cannot exceed 100 characters.")]
    public string? Manager { get; set; }

    /// <summary>
    /// Indicates if the warehouse is fiscal.
    /// </summary>
    public bool IsFiscal { get; set; }

    /// <summary>
    /// Additional notes or description for the warehouse.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Total area of the warehouse in square meters.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Area must be non-negative.")]
    public int? AreaSquareMeters { get; set; }

    /// <summary>
    /// Maximum storage capacity.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Capacity must be non-negative.")]
    public int? Capacity { get; set; }

    /// <summary>
    /// Indicates if the warehouse is refrigerated.
    /// </summary>
    public bool IsRefrigerated { get; set; }
}