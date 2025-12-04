namespace EventForge.DTOs.External;

/// <summary>
/// DTO for VAT lookup result.
/// </summary>
public class VatLookupResultDto
{
    /// <summary>
    /// Whether the VAT number is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// ISO 2-letter country code.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// VAT number without country code.
    /// </summary>
    public string? VatNumber { get; set; }

    /// <summary>
    /// Company or person name associated with the VAT number.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Raw address string as returned by the service.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Parsed street name and number.
    /// </summary>
    public string? Street { get; set; }

    /// <summary>
    /// Parsed city name.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Parsed postal code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Parsed province or state code.
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    /// Error message if the lookup failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
