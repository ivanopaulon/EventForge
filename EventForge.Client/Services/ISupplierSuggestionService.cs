using EventForge.DTOs.Products.SupplierSuggestion;

namespace EventForge.Client.Services;

/// <summary>
/// Service for intelligent supplier recommendations.
/// </summary>
public interface ISupplierSuggestionService
{
    /// <summary>
    /// Gets supplier suggestions for a product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <returns>Supplier suggestions response.</returns>
    Task<SupplierSuggestionResponse?> GetSupplierSuggestionsAsync(Guid productId);

    /// <summary>
    /// Applies a suggested supplier as preferred.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="reason">Reason for applying the suggestion.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> ApplySuggestedSupplierAsync(Guid productId, Guid supplierId, string? reason);

    /// <summary>
    /// Gets reliability metrics for a supplier.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <returns>Reliability response.</returns>
    Task<SupplierReliabilityResponse?> GetSupplierReliabilityAsync(Guid supplierId);
}
