using EventForge.DTOs.Products.SupplierSuggestion;
using EventForge.Server.Data.Entities.Products;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service for intelligent supplier recommendations.
/// </summary>
public interface ISupplierSuggestionService
{
    /// <summary>
    /// Gets ranked supplier suggestions for a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Supplier suggestions with detailed scoring.</returns>
    Task<SupplierSuggestionResponse> GetSupplierSuggestionsAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates suggestions for all suppliers of a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of scored supplier suggestions.</returns>
    Task<List<SupplierSuggestion>> CalculateSuggestionsAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a suggested supplier as preferred for a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="reason">Reason for applying the suggestion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> ApplySuggestedSupplierAsync(Guid productId, Guid supplierId, string? reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed reliability metrics for a supplier.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Supplier reliability response.</returns>
    Task<SupplierReliabilityResponse> GetSupplierReliabilityAsync(Guid supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an AI-powered natural language explanation for a recommendation.
    /// </summary>
    /// <param name="suggestion">The supplier suggestion.</param>
    /// <param name="product">The product.</param>
    /// <returns>Natural language explanation.</returns>
    Task<string> GenerateRecommendationExplanationAsync(SupplierSuggestion suggestion, Product product);
}
