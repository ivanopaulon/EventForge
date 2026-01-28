using EventForge.DTOs.Common;
using EventForge.DTOs.Promotions;

namespace EventForge.Server.Services.Promotions;

/// <summary>
/// Service interface for managing promotions.
/// </summary>
public interface IPromotionService
{
    /// <summary>
    /// Gets all promotions with pagination.
    /// </summary>
    Task<PagedResult<PromotionDto>> GetPromotionsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a promotion by ID.
    /// </summary>
    Task<PromotionDto?> GetPromotionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active promotions.
    /// </summary>
    Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new promotion.
    /// </summary>
    Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing promotion.
    /// </summary>
    Task<PromotionDto?> UpdatePromotionAsync(Guid id, UpdatePromotionDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a promotion (soft delete).
    /// </summary>
    Task<bool> DeletePromotionAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a promotion exists.
    /// </summary>
    Task<bool> PromotionExistsAsync(Guid promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies promotion rules to a cart/order and returns the discount calculations.
    /// </summary>
    /// <param name="applyDto">Cart/order data for promotion application</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with applied discounts and affected items</returns>
    Task<PromotionApplicationResultDto> ApplyPromotionRulesAsync(ApplyPromotionRulesDto applyDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available promotion rules for a given context.
    /// </summary>
    /// <param name="customerId">Customer ID (optional)</param>
    /// <param name="salesChannel">Sales channel (optional)</param>
    /// <param name="orderDateTime">Order date/time for date-based rules</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of applicable promotion rules</returns>
    Task<IEnumerable<PromotionRuleDto>> GetApplicablePromotionRulesAsync(
        Guid? customerId = null,
        string? salesChannel = null,
        DateTime? orderDateTime = null,
        CancellationToken cancellationToken = default);
}