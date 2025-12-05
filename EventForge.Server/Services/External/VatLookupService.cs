using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EventForge.Server.Services.External;

/// <summary>
/// Robust implementation of VAT lookup service with multiple providers and fallback mechanisms.
/// </summary>
public class VatLookupService : IVatLookupService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VatLookupService> _logger;
    private readonly IConfiguration _configuration;

    private const string ViesSoapUrl = "https://ec.europa.eu/taxation_customs/vies/services/checkVatService";
    private static readonly TimeSpan ValidResultCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan InvalidResultCacheDuration = TimeSpan.FromHours(1);

    public VatLookupService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<VatLookupService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
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

        // Italian VAT checksum validation (perform first for IT VAT numbers)
        if (countryCode == "IT" && !IsValidItalianVatChecksum(vatNumberOnly))
        {
            _logger.LogInformation("Italian VAT checksum validation failed for {VatNumber}", vatNumberOnly);
            var invalidResult = new VatLookupResult
            {
                IsValid = false,
                CountryCode = countryCode,
                VatNumber = vatNumberOnly,
                ErrorMessage = "Invalid Italian VAT number (checksum failed)"
            };

            // Cache invalid result with shorter TTL
            _cache.Set(cacheKey, invalidResult, InvalidResultCacheDuration);
            return invalidResult;
        }

        VatLookupResult? result = null;

        // Try REST provider first
        try
        {
            result = await TryRestProviderAsync(countryCode, vatNumberOnly, cancellationToken);
            if (result != null)
            {
                _logger.LogInformation("REST provider returned result for {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
                CacheResult(cacheKey, result);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST provider failed for {CountryCode}{VatNumber}, falling back to VIES SOAP", countryCode, vatNumberOnly);
        }

        // Fallback to VIES SOAP
        try
        {
            result = await TryViesSoapAsync(countryCode, vatNumberOnly, cancellationToken);
            if (result != null)
            {
                _logger.LogInformation("VIES SOAP returned result for {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
                CacheResult(cacheKey, result);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIES SOAP fallback failed for {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
        }

        // All providers failed
        _logger.LogError("All VAT lookup providers failed for {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
        return new VatLookupResult
        {
            IsValid = false,
            CountryCode = countryCode,
            VatNumber = vatNumberOnly,
            ErrorMessage = "All VAT lookup providers are temporarily unavailable"
        };
    }

    /// <summary>
    /// Validates Italian VAT number using the 11-digit checksum algorithm.
    /// </summary>
    private bool IsValidItalianVatChecksum(string vatNumber)
    {
        if (string.IsNullOrWhiteSpace(vatNumber) || vatNumber.Length != 11)
        {
            return false;
        }

        // Check if all characters are digits
        if (!vatNumber.All(char.IsDigit))
        {
            return false;
        }

        // Italian VAT checksum algorithm
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            int digit = vatNumber[i] - '0';
            if (i % 2 == 0)
            {
                // Even positions (0, 2, 4, 6, 8): add the digit
                sum += digit;
            }
            else
            {
                // Odd positions (1, 3, 5, 7, 9): double the digit and add
                int doubled = digit * 2;
                sum += doubled > 9 ? doubled - 9 : doubled;
            }
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        int providedCheckDigit = vatNumber[10] - '0';

        return checkDigit == providedCheckDigit;
    }

    /// <summary>
    /// Attempts to lookup VAT using REST provider (e.g., VATcomply).
    /// </summary>
    private async Task<VatLookupResult?> TryRestProviderAsync(string countryCode, string vatNumber, CancellationToken cancellationToken)
    {
        var urlTemplate = _configuration["VatLookup:RestProviderUrlTemplate"];
        if (string.IsNullOrWhiteSpace(urlTemplate))
        {
            _logger.LogDebug("REST provider URL template not configured");
            return null;
        }

        // Replace placeholders in template
        var url = urlTemplate
            .Replace("{country}", countryCode)
            .Replace("{vat}", vatNumber)
            .Replace("{countrycode}", countryCode)
            .Replace("{vatnumber}", vatNumber);

        // If template doesn't have placeholders, use it as-is (assume full replacement)
        if (!urlTemplate.Contains("{"))
        {
            url = urlTemplate;
        }

        _logger.LogInformation("Querying REST provider: {Url}", url);

        var timeout = _configuration.GetValue<int?>("VatLookup:RestTimeoutSeconds") ?? 10;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeout));

        var response = await _httpClient.GetAsync(url, cts.Token);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("REST provider returned status code {StatusCode}", response.StatusCode);
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
        _logger.LogDebug("REST provider raw response: {Response}", responseJson);

        // Parse VATcomply response format
        var vatComplyResponse = JsonSerializer.Deserialize<VatComplyResponse>(responseJson);
        if (vatComplyResponse == null)
        {
            _logger.LogWarning("Failed to deserialize REST provider response");
            return null;
        }

        var result = new VatLookupResult
        {
            IsValid = vatComplyResponse.Valid ?? false,
            CountryCode = vatComplyResponse.CountryCode ?? countryCode,
            VatNumber = vatComplyResponse.VatNumber ?? vatNumber,
            Name = vatComplyResponse.Name,
            Address = vatComplyResponse.Address
        };

        // Parse address if available
        if (!string.IsNullOrWhiteSpace(result.Address))
        {
            result.ParsedAddress = ParseItalianAddress(result.Address);
        }

        return result;
    }

    /// <summary>
    /// Attempts to lookup VAT using VIES SOAP service.
    /// </summary>
    private async Task<VatLookupResult?> TryViesSoapAsync(string countryCode, string vatNumber, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Querying VIES SOAP for {CountryCode}{VatNumber}", countryCode, vatNumber);

        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <checkVat xmlns=""urn:ec.europa.eu:taxud:vies:services:checkVat:types"">
      <countryCode>{countryCode}</countryCode>
      <vatNumber>{vatNumber}</vatNumber>
    </checkVat>
  </soap:Body>
</soap:Envelope>";

        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "");

        var response = await _httpClient.PostAsync(ViesSoapUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("VIES SOAP returned status code {StatusCode}", response.StatusCode);
            return null;
        }

        var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("VIES SOAP raw response: {Response}", responseXml);

        // Parse SOAP response
        try
        {
            var doc = XDocument.Parse(responseXml);
            var ns = XNamespace.Get("urn:ec.europa.eu:taxud:vies:services:checkVat:types");
            var checkVatResponse = doc.Descendants(ns + "checkVatResponse").FirstOrDefault();

            if (checkVatResponse == null)
            {
                _logger.LogWarning("Failed to parse VIES SOAP response");
                return null;
            }

            var valid = checkVatResponse.Element(ns + "valid")?.Value.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            var name = checkVatResponse.Element(ns + "name")?.Value;
            var address = checkVatResponse.Element(ns + "address")?.Value;

            // Handle missing name/address as '---'
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "---";
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                address = "---";
            }

            var result = new VatLookupResult
            {
                IsValid = valid,
                CountryCode = countryCode,
                VatNumber = vatNumber,
                Name = name,
                Address = address
            };

            // Parse address if available and not placeholder
            if (!string.IsNullOrWhiteSpace(result.Address) && result.Address != "---")
            {
                result.ParsedAddress = ParseItalianAddress(result.Address);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse VIES SOAP response");
            return null;
        }
    }

    /// <summary>
    /// Caches the result with appropriate TTL based on validity.
    /// </summary>
    private void CacheResult(string cacheKey, VatLookupResult result)
    {
        var cacheDuration = result.IsValid ? ValidResultCacheDuration : InvalidResultCacheDuration;
        _cache.Set(cacheKey, result, cacheDuration);
        _logger.LogDebug("Cached VAT lookup result for {CacheKey} with TTL {Duration}", cacheKey, cacheDuration);
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
                if (match.Groups[3].Success && !string.IsNullOrWhiteSpace(match.Groups[3].Value))
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
    /// Response model from VATcomply API.
    /// </summary>
    private class VatComplyResponse
    {
        [JsonPropertyName("valid")]
        public bool? Valid { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }

        [JsonPropertyName("vat_number")]
        public string? VatNumber { get; set; }

        [JsonPropertyName("company_name")]
        public string? Name { get; set; }

        [JsonPropertyName("company_address")]
        public string? Address { get; set; }
    }
}
