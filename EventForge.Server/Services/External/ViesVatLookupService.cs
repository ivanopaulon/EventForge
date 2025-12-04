using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace EventForge.Server.Services.External;

/// <summary>
/// Implementation of VAT lookup service using VIES (VAT Information Exchange System).
/// </summary>
public class ViesVatLookupService : IVatLookupService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ViesVatLookupService> _logger;

    private const string ViesApiUrl = "https://ec.europa.eu/taxation_customs/vies/rest-api/check-vat-number";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public ViesVatLookupService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<ViesVatLookupService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<VatLookupResult?> LookupAsync(string vatNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vatNumber))
        {
            return new VatLookupResult
            {
                IsValid = false,
                ErrorMessage = "VAT number is required"
            };
        }

        // Parse country code and VAT number
        var (countryCode, vatNumberOnly) = ParseVatNumber(vatNumber);

        // Check cache first
        var cacheKey = $"vat_lookup_{countryCode}_{vatNumberOnly}";
        if (_cache.TryGetValue<VatLookupResult>(cacheKey, out var cachedResult))
        {
            _logger.LogInformation("VAT lookup cache hit for {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
            return cachedResult;
        }

        try
        {
            _logger.LogInformation("Looking up VAT number {CountryCode}{VatNumber} via VIES", countryCode, vatNumberOnly);

            // Prepare request
            var request = new ViesRequest
            {
                CountryCode = countryCode,
                VatNumber = vatNumberOnly
            };

            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            // Call VIES API
            var response = await _httpClient.PostAsync(ViesApiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VIES API returned status code {StatusCode}", response.StatusCode);
                return new VatLookupResult
                {
                    IsValid = false,
                    ErrorMessage = "VIES service is temporarily unavailable"
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var viesResponse = JsonSerializer.Deserialize<ViesResponse>(responseJson);

            if (viesResponse == null)
            {
                _logger.LogWarning("Failed to deserialize VIES response");
                return new VatLookupResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid response from VIES service"
                };
            }

            // Build result
            var result = new VatLookupResult
            {
                IsValid = viesResponse.IsValid,
                CountryCode = viesResponse.CountryCode,
                VatNumber = viesResponse.VatNumber,
                Name = viesResponse.Name,
                Address = viesResponse.Address
            };

            // Parse address if available
            if (!string.IsNullOrWhiteSpace(viesResponse.Address))
            {
                result.ParsedAddress = ParseItalianAddress(viesResponse.Address);
            }

            // Cache the result
            _cache.Set(cacheKey, result, CacheDuration);

            _logger.LogInformation(
                "VAT lookup completed: {CountryCode}{VatNumber} - Valid: {IsValid}",
                countryCode, vatNumberOnly, result.IsValid);

            return result;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "VIES lookup timed out for {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
            return new VatLookupResult
            {
                IsValid = false,
                ErrorMessage = "Request timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up VAT number {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
            return new VatLookupResult
            {
                IsValid = false,
                ErrorMessage = "An error occurred during lookup"
            };
        }
    }

    /// <summary>
    /// Parses VAT number to extract country code and number.
    /// </summary>
    private static (string countryCode, string vatNumber) ParseVatNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ("IT", string.Empty);

        input = input.Trim().ToUpperInvariant();

        // Check if starts with 2-letter country code
        if (input.Length >= 2 && char.IsLetter(input[0]) && char.IsLetter(input[1]))
        {
            var countryCode = input.Substring(0, 2);
            var vatNumber = input.Substring(2);
            return (countryCode, vatNumber);
        }

        // Default to Italy
        return ("IT", input);
    }

    /// <summary>
    /// Parses Italian address format.
    /// Typical format: "VIA ROMA 123\n00100 ROMA RM"
    /// </summary>
    private static ParsedAddress? ParseItalianAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var lines = address.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToArray();

        if (lines.Length == 0)
            return null;

        var parsed = new ParsedAddress();

        // First line is typically the street
        if (lines.Length > 0)
        {
            parsed.Street = lines[0];
        }

        // Second line typically contains: PostalCode City Province
        if (lines.Length > 1)
        {
            var secondLine = lines[1];

            // Try to parse: "00100 ROMA RM" or "00100 ROMA" pattern
            var match = Regex.Match(secondLine, @"^(\d{5})\s+([A-Z\s]+?)(?:\s+([A-Z]{2}))?$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                parsed.PostalCode = match.Groups[1].Value;
                parsed.City = match.Groups[2].Value.Trim();
                if (match.Groups.Count > 3 && !string.IsNullOrWhiteSpace(match.Groups[3].Value))
                {
                    parsed.Province = match.Groups[3].Value;
                }
            }
            else
            {
                // Fallback: just store as city
                parsed.City = secondLine;
            }
        }

        return parsed;
    }

    /// <summary>
    /// Request model for VIES API.
    /// </summary>
    private class ViesRequest
    {
        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; } = string.Empty;

        [JsonPropertyName("vatNumber")]
        public string VatNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model from VIES API.
    /// </summary>
    private class ViesResponse
    {
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("vatNumber")]
        public string? VatNumber { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }
    }
}
