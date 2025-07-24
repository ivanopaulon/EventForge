using EventForge.Models.Promotions;

namespace EventForge.Services.Promotions;

/// <summary>
/// Service interface for managing promotions.
/// </summary>
public interface IPromotionService
{
    /// <summary>
    /// Gets all promotions with optional pagination.
    /// </summary>
    Task<PagedResult<PromotionDto>> GetPromotionsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

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
}