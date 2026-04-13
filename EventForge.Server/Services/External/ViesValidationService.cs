using Prym.DTOs.External;
using System.Text.Json;

namespace EventForge.Server.Services.External;

/// <summary>
/// Interface for VIES VAT validation service using the simplified REST API.
/// </summary>
public interface IViesValidationService
{
    /// <summary>
    /// Validates a VAT number using the VIES REST API.
    /// </summary>
    /// <param name="countryCode">Two-letter ISO country code (e.g., "IT", "FR")</param>
    /// <param name="vatNumber">VAT number without country code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VIES validation response or null if the service is unavailable</returns>
    Task<ViesValidationResponseDto?> ValidateVatAsync(string countryCode, string vatNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Simple implementation of VIES VAT validation service using the official REST API.
/// Uses direct GET requests to: https://ec.europa.eu/taxation_customs/vies/rest-api/ms/{country}/vat/{vat}
/// </summary>
public class ViesValidationService(
    HttpClient httpClient,
    ILogger<ViesValidationService> logger) : IViesValidationService
{

    private readonly bool _ctorInitialized = InitInstance(httpClient);

    private const string BaseUrl = "https://ec.europa.eu/taxation_customs/vies/rest-api/ms";

    public async Task<ViesValidationResponseDto?> ValidateVatAsync(
        string countryCode,
        string vatNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Clean VAT number (remove spaces, dashes, country code prefix)
            var cleanVat = CleanVatNumber(vatNumber, countryCode);

            // Build URL
            var url = $"{BaseUrl}/{countryCode.ToUpper()}/vat/{cleanVat}";

            logger.LogInformation("Validating VAT: {CountryCode}{VatNumber} via VIES REST API",
                countryCode, cleanVat);

            // Make request
            var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("VIES API returned status {StatusCode} for VAT {CountryCode}{VatNumber}",
                    response.StatusCode, countryCode, cleanVat);
                return null;
            }

            // Parse response with case-insensitive property matching
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = await response.Content.ReadFromJsonAsync<ViesValidationResponseDto>(options, cancellationToken);

            if (result is not null)
            {
                logger.LogInformation("VIES validation result: {IsValid} for {CountryCode}{VatNumber} - Name={Name} UserError={UserError}",
                    result.IsValid, countryCode, cleanVat, result.Name, result.UserError);

                // Transient errors (service/member-state unavailable, rate limit) must NOT be cached.
                // Return null so the caller treats this as a temporary failure.
                if (!result.IsValid && IsTransientViesError(result.UserError))
                {
                    logger.LogWarning("VIES transient error for {CountryCode}{VatNumber}: {UserError} — result will not be cached",
                        countryCode, cleanVat, result.UserError);
                    return null;
                }
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error validating VAT {CountryCode}{VatNumber}",
                countryCode, vatNumber);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating VAT {CountryCode}{VatNumber}",
                countryCode, vatNumber);
            return null;
        }
    }

    /// <summary>
    /// Returns true for VIES userError codes that indicate a transient service issue,
    /// not a genuine invalidity of the VAT number itself.
    /// These results must never be cached.
    /// </summary>
    private static bool IsTransientViesError(string? userError) =>
        userError switch
        {
            "MS_UNAVAILABLE" => true,   // Member state system temporarily down
            "SERVICE_UNAVAILABLE" => true,   // VIES itself unavailable
            "MS_MAX_CONCURRENT_REQ" => true,   // Rate limit hit
            "TIMEOUT" => true,   // Request timed out on VIES side
            _ => false
        };

    private static string CleanVatNumber(string vatNumber, string countryCode)
    {
        // Remove spaces, dashes, dots
        var cleaned = vatNumber.Replace(" ", "").Replace("-", "").Replace(".", "");

        // Remove country code prefix if present
        if (cleaned.StartsWith(countryCode, StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[countryCode.Length..];
        }

        return cleaned;
    }

    private static bool InitInstance(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return true;
    }

}
