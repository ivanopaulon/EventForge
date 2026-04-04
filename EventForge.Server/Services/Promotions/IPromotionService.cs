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

    /// <summary>
    /// Validates a coupon code and returns the matching promotion if valid and within usage limits.
    /// Returns null if the coupon is invalid, expired, or has reached MaxUses.
    /// </summary>
    /// <param name="couponCode">The coupon code to validate.</param>
    /// <param name="customerId">Optional customer ID for customer-specific promotions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching <see cref="PromotionDto"/> if valid; otherwise null.</returns>
    Task<PromotionDto?> ValidateCouponAsync(string couponCode, Guid? customerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments the usage counter for a promotion, respecting MaxUses limits.
    /// Uses optimistic concurrency with retry to handle concurrent requests.
    /// Returns true if the increment succeeded, false if MaxUses was already reached.
    /// </summary>
    /// <param name="promotionId">The ID of the promotion to increment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the increment succeeded; false if MaxUses was already reached.</returns>
    Task<bool> IncrementUsageAsync(Guid promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a collection of applied promotions into a JSON string suitable for
    /// storing in <c>DocumentRow.AppliedPromotionsJSON</c>.
    /// Returns <c>null</c> when <paramref name="appliedPromotions"/> is empty.
    /// </summary>
    /// <param name="appliedPromotions">The promotions applied to a single document row.</param>
    /// <returns>A JSON string, or <c>null</c> if no promotions were applied.</returns>
    string? SerializeAppliedPromotionsJson(IEnumerable<AppliedPromotionDto> appliedPromotions);

    /// <summary>
    /// Gets all rules for a promotion.
    /// </summary>
    Task<IEnumerable<PromotionRuleDto>> GetPromotionRulesAsync(Guid promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new rule to a promotion.
    /// </summary>
    Task<PromotionRuleDto> AddPromotionRuleAsync(Guid promotionId, CreatePromotionRuleDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing promotion rule.
    /// </summary>
    Task<PromotionRuleDto?> UpdatePromotionRuleAsync(Guid promotionId, Guid ruleId, UpdatePromotionRuleDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a promotion rule (soft delete).
    /// </summary>
    Task<bool> DeletePromotionRuleAsync(Guid promotionId, Guid ruleId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products associated with a promotion rule.
    /// </summary>
    Task<IEnumerable<PromotionRuleProductDto>> GetRuleProductsAsync(Guid promotionId, Guid ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a product to a promotion rule.
    /// </summary>
    Task<PromotionRuleProductDto> AddRuleProductAsync(Guid promotionId, Guid ruleId, CreatePromotionRuleProductDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a product from a promotion rule.
    /// </summary>
    Task<bool> RemoveRuleProductAsync(Guid promotionId, Guid ruleId, Guid productId, string currentUser, CancellationToken cancellationToken = default);
}