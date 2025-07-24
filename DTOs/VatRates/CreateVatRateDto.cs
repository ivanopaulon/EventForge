using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.VatRates;

/// <summary>
/// DTO for VAT Rate creation operations.
/// </summary>
public class CreateVatRateDto
{
    /// <summary>
    /// Name of the VAT rate (e.g., "VAT 22%").
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
    [Display(Name = "Name", Description = "Name of the VAT rate.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Percentage of the VAT rate (e.g., 22).
    /// </summary>
    [Range(0, 100, ErrorMessage = "The percentage must be between 0 and 100.")]
    [Display(Name = "Percentage", Description = "Percentage of the VAT rate.")]
    public decimal Percentage { get; set; } = 0m;

    /// <summary>
    /// Status of the VAT rate.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the VAT rate.")]
    public ProductVatRateStatus Status { get; set; } = ProductVatRateStatus.Active;

    /// <summary>
    /// Start date of the VAT rate validity.
    /// </summary>
    [Display(Name = "Valid From", Description = "Start date of the VAT rate validity.")]
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// End date of the VAT rate validity.
    /// </summary>
    [Display(Name = "Valid To", Description = "End date of the VAT rate validity.")]
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Additional notes about the VAT rate.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes about the VAT rate.")]
    public string? Notes { get; set; }
}