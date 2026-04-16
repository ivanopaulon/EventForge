using Prym.DTOs.External;

namespace Prym.Web.Services.External;

/// <summary>
/// Client service for VAT number lookup.
/// </summary>
public interface IVatLookupService
{
    /// <summary>
    /// Looks up a VAT number and returns validation result with company information.
    /// </summary>
    /// <param name="vatNumber">VAT number to lookup (with or without country code)</param>
    /// <returns>VAT lookup result or null if the service is unavailable</returns>
    Task<VatLookupResultDto?> LookupAsync(string vatNumber, CancellationToken ct = default);
}
