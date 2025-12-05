using EventForge.DTOs.External;
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
public class ViesValidationService : IViesValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ViesValidationService> _logger;
    private const string BaseUrl = "https://ec.europa.eu/taxation_customs/vies/rest-api/ms";

    public ViesValidationService(
        HttpClient httpClient,
        ILogger<ViesValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure JSON options to handle property name case
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

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
            
            _logger.LogInformation("Validating VAT: {CountryCode}{VatNumber} via VIES REST API", 
                countryCode, cleanVat);

            // Make request
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VIES API returned status {StatusCode} for VAT {CountryCode}{VatNumber}", 
                    response.StatusCode, countryCode, cleanVat);
                return null;
            }

            // Parse response with case-insensitive property matching
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var result = await response.Content.ReadFromJsonAsync<ViesValidationResponseDto>(options, cancellationToken);
            
            if (result != null)
            {
                _logger.LogInformation("VIES validation result: {IsValid} for {CountryCode}{VatNumber} - {Name}", 
                    result.IsValid, countryCode, cleanVat, result.Name);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error validating VAT {CountryCode}{VatNumber}", 
                countryCode, vatNumber);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VAT {CountryCode}{VatNumber}", 
                countryCode, vatNumber);
            return null;
        }
    }

    private static string CleanVatNumber(string vatNumber, string countryCode)
    {
        // Remove spaces, dashes, dots
        var cleaned = vatNumber.Replace(" ", "").Replace("-", "").Replace(".", "");
        
        // Remove country code prefix if present
        if (cleaned.StartsWith(countryCode, StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(countryCode.Length);
        }
        
        return cleaned;
    }
}
