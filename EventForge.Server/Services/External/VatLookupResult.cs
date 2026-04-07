namespace EventForge.Server.Services.External;

/// <summary>
/// Result of a VAT number lookup.
/// </summary>
public class VatLookupResult
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
    /// Parsed address components (if parsing was successful).
    /// </summary>
    public ParsedAddress? ParsedAddress { get; set; }

    /// <summary>
    /// Error message if the lookup failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Parsed address components.
/// </summary>
public class ParsedAddress
{
    /// <summary>
    /// Street name and number.
    /// </summary>
    public string? Street { get; set; }

    /// <summary>
    /// City name.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Postal code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Province or state code.
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    /// Country name.
    /// </summary>
    public string? Country { get; set; }
}
