using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace EventForge.Server.Services.External;

/// <summary>
/// Simplified VAT lookup service using the official VIES REST API.
/// Replaces complex multi-provider system with direct calls to VIES.
/// </summary>
public class VatLookupService : IVatLookupService
{
    private readonly IViesValidationService _viesValidationService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VatLookupService> _logger;

    private static readonly TimeSpan ValidResultCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan InvalidResultCacheDuration = TimeSpan.FromHours(1);

    public VatLookupService(
        IViesValidationService viesValidationService,
        IMemoryCache cache,
        ILogger<VatLookupService> logger)
    {
        _viesValidationService = viesValidationService;
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

        // Call VIES REST API
        try
        {
            var viesResponse = await _viesValidationService.ValidateVatAsync(countryCode, vatNumberOnly, cancellationToken);

            if (viesResponse == null)
            {
                _logger.LogWarning("VIES service returned null for {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
                return new VatLookupResult
                {
                    IsValid = false,
                    CountryCode = countryCode,
                    VatNumber = vatNumberOnly,
                    ErrorMessage = "VIES service is temporarily unavailable"
                };
            }

            // Map VIES response to VatLookupResult
            var result = new VatLookupResult
            {
                IsValid = viesResponse.IsValid,
                CountryCode = countryCode,
                VatNumber = vatNumberOnly,
                Name = viesResponse.Name,
                Address = viesResponse.Address
            };

            // Parse address if available
            if (!string.IsNullOrWhiteSpace(result.Address))
            {
                result.ParsedAddress = ParseItalianAddress(result.Address);
            }

            // Cache the result
            CacheResult(cacheKey, result);

            _logger.LogInformation("VIES validation completed for {CountryCode}{VatNumber}: Valid={IsValid}",
                countryCode, vatNumberOnly, result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VAT {CountryCode}{VatNumber}", countryCode, vatNumberOnly);
            return new VatLookupResult
            {
                IsValid = false,
                CountryCode = countryCode,
                VatNumber = vatNumberOnly,
                ErrorMessage = "An error occurred during VAT validation"
            };
        }
    }

    /// <summary>
    /// Caches the result with appropriate TTL based on validity.
    /// </summary>
    private void CacheResult(string cacheKey, VatLookupResult result)
    {
        var cacheDuration = result.IsValid ? ValidResultCacheDuration : InvalidResultCacheDuration;
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheDuration,
            Size = 1
        };
        _cache.Set(cacheKey, result, cacheOptions);
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

}
