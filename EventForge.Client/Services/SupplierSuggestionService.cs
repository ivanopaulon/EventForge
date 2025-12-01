using EventForge.DTOs.Products.SupplierSuggestion;
using System.Net.Http.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of supplier suggestion service.
/// </summary>
public class SupplierSuggestionService : ISupplierSuggestionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SupplierSuggestionService> _logger;

    public SupplierSuggestionService(
        HttpClient httpClient,
        ILogger<SupplierSuggestionService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SupplierSuggestionResponse?> GetSupplierSuggestionsAsync(Guid productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/supplier-suggestions/products/{productId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SupplierSuggestionResponse>();
            }

            _logger.LogWarning("Failed to get supplier suggestions for product {ProductId}. Status: {StatusCode}",
                productId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier suggestions for product {ProductId}", productId);
            return null;
        }
    }

    public async Task<bool> ApplySuggestedSupplierAsync(Guid productId, Guid supplierId, string? reason)
    {
        try
        {
            var request = new ApplySuggestionRequest
            {
                ProductId = productId,
                SupplierId = supplierId,
                Reason = reason
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/supplier-suggestions/products/{productId}/apply", request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Failed to apply suggested supplier {SupplierId} for product {ProductId}. Status: {StatusCode}",
                supplierId, productId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying suggested supplier {SupplierId} for product {ProductId}",
                supplierId, productId);
            return false;
        }
    }

    public async Task<SupplierReliabilityResponse?> GetSupplierReliabilityAsync(Guid supplierId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/supplier-suggestions/suppliers/{supplierId}/reliability");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SupplierReliabilityResponse>();
            }

            _logger.LogWarning("Failed to get reliability for supplier {SupplierId}. Status: {StatusCode}",
                supplierId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reliability for supplier {SupplierId}", supplierId);
            return null;
        }
    }
}
