using EventForge.DTOs.Products.SupplierSuggestion;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of supplier suggestion service.
/// </summary>
public class SupplierSuggestionService : ISupplierSuggestionService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<SupplierSuggestionService> _logger;

    public SupplierSuggestionService(
        IHttpClientService httpClientService,
        ILogger<SupplierSuggestionService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SupplierSuggestionResponse?> GetSupplierSuggestionsAsync(Guid productId)
    {
        try
        {
            return await _httpClientService.GetAsync<SupplierSuggestionResponse>($"api/v1/supplier-suggestions/products/{productId}");
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

            await _httpClientService.PostAsync<ApplySuggestionRequest, object>(
                $"api/v1/supplier-suggestions/products/{productId}/apply", request);
            return true;
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
            return await _httpClientService.GetAsync<SupplierReliabilityResponse>($"api/v1/supplier-suggestions/suppliers/{supplierId}/reliability");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reliability for supplier {SupplierId}", supplierId);
            return null;
        }
    }
}
