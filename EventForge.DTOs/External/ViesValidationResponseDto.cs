using System;

namespace EventForge.DTOs.External;

/// <summary>
/// Response DTO for VIES VAT validation using the simplified REST API.
/// Maps directly to the JSON response from https://ec.europa.eu/taxation_customs/vies/rest-api/ms/{country}/vat/{vat}
/// </summary>
public class ViesValidationResponseDto
{
    /// <summary>
    /// Whether the VAT number is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Date and time of the validation request.
    /// </summary>
    public DateTime RequestDate { get; set; }

    /// <summary>
    /// User error message or validation status (e.g., "VALID", "INVALID").
    /// </summary>
    public string UserError { get; set; } = string.Empty;

    /// <summary>
    /// Company or person name associated with the VAT number.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Raw address string as returned by the VIES service.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Request identifier from VIES.
    /// </summary>
    public string RequestIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Original VAT number as submitted.
    /// </summary>
    public string OriginalVatNumber { get; set; } = string.Empty;

    /// <summary>
    /// VAT number without country code.
    /// </summary>
    public string VatNumber { get; set; } = string.Empty;

    /// <summary>
    /// VIES approximate match information for address components.
    /// </summary>
    public ViesApproximateMatchDto? ViesApproximate { get; set; }
}

/// <summary>
/// VIES approximate match information for detailed address matching.
/// </summary>
public class ViesApproximateMatchDto
{
    /// <summary>
    /// Approximate company name.
    /// </summary>
    public string Name { get; set; } = "---";

    /// <summary>
    /// Approximate street address.
    /// </summary>
    public string Street { get; set; } = "---";

    /// <summary>
    /// Approximate postal code.
    /// </summary>
    public string PostalCode { get; set; } = "---";

    /// <summary>
    /// Approximate city name.
    /// </summary>
    public string City { get; set; } = "---";

    /// <summary>
    /// Approximate company type.
    /// </summary>
    public string CompanyType { get; set; } = "---";

    /// <summary>
    /// Match quality for name (0-3).
    /// </summary>
    public int MatchName { get; set; }

    /// <summary>
    /// Match quality for street (0-3).
    /// </summary>
    public int MatchStreet { get; set; }

    /// <summary>
    /// Match quality for postal code (0-3).
    /// </summary>
    public int MatchPostalCode { get; set; }

    /// <summary>
    /// Match quality for city (0-3).
    /// </summary>
    public int MatchCity { get; set; }

    /// <summary>
    /// Match quality for company type (0-3).
    /// </summary>
    public int MatchCompanyType { get; set; }
}
