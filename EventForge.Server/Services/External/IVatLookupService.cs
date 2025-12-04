namespace EventForge.Server.Services.External;

/// <summary>
/// Service for looking up VAT numbers using external validation services.
/// </summary>
public interface IVatLookupService
{
    /// <summary>
    /// Looks up a VAT number and returns validation result with company information.
    /// </summary>
    /// <param name="vatNumber">VAT number to lookup (with or without country code)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT lookup result or null if the service is unavailable</returns>
    Task<VatLookupResult?> LookupAsync(string vatNumber, CancellationToken cancellationToken = default);
}
