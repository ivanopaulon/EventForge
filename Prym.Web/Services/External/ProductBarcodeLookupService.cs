using Prym.DTOs.External;
using System.Text.Json;

namespace Prym.Web.Services.External;

/// <summary>
/// Implementation of product barcode lookup using public/free providers.
/// </summary>
public class ProductBarcodeLookupService(
    IHttpClientFactory httpClientFactory,
    ILogger<ProductBarcodeLookupService> logger) : IProductBarcodeLookupService
{
    private readonly record struct LookupProvider(string Name, string UrlTemplate);

    private static readonly LookupProvider[] Providers =
    [
        new("Open Products Facts", "https://world.openproductsfacts.org/api/v2/product/{0}.json"),
        new("Open Beauty Facts", "https://world.openbeautyfacts.org/api/v2/product/{0}.json"),
        new("Open Pet Food Facts", "https://world.openpetfoodfacts.org/api/v2/product/{0}.json"),
        new("Open Food Facts", "https://world.openfoodfacts.org/api/v2/product/{0}.json")
    ];

    public async Task<ProductBarcodeLookupResultDto?> LookupAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new ProductBarcodeLookupResultDto
            {
                IsFound = false,
                ErrorMessage = "Product code is required"
            };
        }

        var normalizedCode = NormalizeCode(code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new ProductBarcodeLookupResultDto
            {
                IsFound = false,
                ErrorMessage = "Product code is not valid"
            };
        }

        var httpClient = httpClientFactory.CreateClient("ProductBarcodeLookupClient");
        var escapedCode = Uri.EscapeDataString(normalizedCode);

        foreach (var provider in Providers)
        {
            try
            {
                var result = await TryLookupProviderAsync(httpClient, provider, normalizedCode, escapedCode, ct);
                if (result is { IsFound: true })
                {
                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Product lookup failed on provider {Provider} for {Code}", provider.Name, normalizedCode);
            }
        }

        return new ProductBarcodeLookupResultDto
        {
            IsFound = false,
            Code = normalizedCode,
            ErrorMessage = "No product data found from public providers"
        };
    }

    private async Task<ProductBarcodeLookupResultDto?> TryLookupProviderAsync(
        HttpClient httpClient,
        LookupProvider provider,
        string code,
        string escapedCode,
        CancellationToken ct)
    {
        var endpoint = string.Format(provider.UrlTemplate, escapedCode);

        using var response = await httpClient.GetAsync(endpoint, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogDebug("Provider {Provider} returned {StatusCode} for {Code}", provider.Name, response.StatusCode, code);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!json.RootElement.TryGetProperty("status", out var statusElement) || statusElement.GetInt32() != 1)
        {
            return null;
        }

        if (!json.RootElement.TryGetProperty("product", out var productElement) || productElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var name = GetFirstString(productElement, "product_name_it", "product_name", "product_name_en");
        var brand = GetFirstString(productElement, "brands", "brand_owner");
        var shortDescription = GetFirstString(productElement, "generic_name_it", "generic_name", "quantity");
        var description = GetFirstString(productElement, "generic_name_it", "generic_name", "ingredients_text_it", "ingredients_text", "categories");
        var imageUrl = GetFirstString(productElement, "image_front_url", "image_url", "image_front_small_url");

        if (string.IsNullOrWhiteSpace(name) &&
            string.IsNullOrWhiteSpace(brand) &&
            string.IsNullOrWhiteSpace(shortDescription) &&
            string.IsNullOrWhiteSpace(description) &&
            string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        return new ProductBarcodeLookupResultDto
        {
            IsFound = true,
            Code = code,
            Name = name,
            Brand = brand,
            ShortDescription = shortDescription,
            Description = description,
            ImageUrl = imageUrl,
            Source = provider.Name
        };
    }

    private static string NormalizeCode(string code)
    {
        return new string(code.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string? GetFirstString(JsonElement productElement, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!productElement.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = value.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        return null;
    }
}
