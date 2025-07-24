namespace EventForge.DTOs.VatRates;

/// <summary>
/// DTO for VAT Rate output/display operations.
/// </summary>
public class VatRateDto
{
    /// <summary>
    /// Unique identifier for the VAT rate.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the VAT rate (e.g., "VAT 22%").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Percentage of the VAT rate (e.g., 22).
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Status of the VAT rate.
    /// </summary>
    public ProductVatRateStatus Status { get; set; }

    /// <summary>
    /// Start date of the VAT rate validity.
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// End date of the VAT rate validity.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Additional notes about the VAT rate.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Date and time when the VAT rate was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the VAT rate.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the VAT rate was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the VAT rate.
    /// </summary>
    public string? ModifiedBy { get; set; }
}