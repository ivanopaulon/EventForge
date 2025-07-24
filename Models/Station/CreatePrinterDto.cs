using System.ComponentModel.DataAnnotations;

namespace EventForge.Models.Station;

/// <summary>
/// DTO for Printer creation operations.
/// </summary>
public class CreatePrinterDto
{
    /// <summary>
    /// Display name of the printer.
    /// </summary>
    [Required(ErrorMessage = "The printer name is required.")]
    [MaxLength(50, ErrorMessage = "The printer name cannot exceed 50 characters.")]
    [Display(Name = "Printer Name", Description = "Name or label of the printer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type or role of the printer (e.g., Kitchen, Bar, Draft, Receipt).
    /// </summary>
    [Required(ErrorMessage = "The printer type is required.")]
    [MaxLength(30, ErrorMessage = "The printer type cannot exceed 30 characters.")]
    [Display(Name = "Type", Description = "Type or role of the printer (e.g., Kitchen, Bar, Draft, Receipt).")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Printer model (for technical reference).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The model cannot exceed 50 characters.")]
    [Display(Name = "Model", Description = "Printer model.")]
    public string? Model { get; set; }

    /// <summary>
    /// Printer location (physical or logical).
    /// </summary>
    [MaxLength(50, ErrorMessage = "The location cannot exceed 50 characters.")]
    [Display(Name = "Location", Description = "Physical or logical location of the printer.")]
    public string? Location { get; set; }

    /// <summary>
    /// Network address or identifier (e.g., IP, USB path).
    /// </summary>
    [MaxLength(100, ErrorMessage = "The address cannot exceed 100 characters.")]
    [Display(Name = "Address", Description = "IP address, USB path, or identifier of the printer.")]
    public string? Address { get; set; }

    /// <summary>
    /// Current status of the printer.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the printer.")]
    public PrinterStatus Status { get; set; } = PrinterStatus.Active;

    /// <summary>
    /// Foreign key to the associated station (optional).
    /// </summary>
    [Display(Name = "Station", Description = "Station associated with the printer.")]
    public Guid? StationId { get; set; }
}