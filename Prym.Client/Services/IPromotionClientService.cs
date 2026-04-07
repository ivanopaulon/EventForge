using Prym.DTOs.Common;
using Prym.DTOs.Promotions;

namespace Prym.Client.Services;

/// <summary>
/// Client-side service interface for promotion management HTTP calls.
/// </summary>
public interface IPromotionClientService
{
    Task<PagedResult<PromotionDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PromotionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<PromotionDto>> GetActiveAsync(CancellationToken ct = default);
    Task<PromotionDto> CreateAsync(CreatePromotionDto dto, CancellationToken ct = default);
    Task<PromotionDto?> UpdateAsync(Guid id, UpdatePromotionDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<PromotionRuleDto>> GetRulesAsync(Guid promotionId, CancellationToken ct = default);
    Task<PromotionRuleDto> AddRuleAsync(Guid promotionId, CreatePromotionRuleDto dto, CancellationToken ct = default);
    Task<PromotionRuleDto?> UpdateRuleAsync(Guid promotionId, Guid ruleId, UpdatePromotionRuleDto dto, CancellationToken ct = default);
    Task<bool> DeleteRuleAsync(Guid promotionId, Guid ruleId, CancellationToken ct = default);

    Task<IEnumerable<PromotionRuleProductDto>> GetRuleProductsAsync(Guid promotionId, Guid ruleId, CancellationToken ct = default);
    Task<PromotionRuleProductDto> AddRuleProductAsync(Guid promotionId, Guid ruleId, CreatePromotionRuleProductDto dto, CancellationToken ct = default);
    Task<bool> RemoveRuleProductAsync(Guid promotionId, Guid ruleId, Guid productId, CancellationToken ct = default);
    Task<PromotionDto?> ValidateCouponCodeAsync(string couponCode, CancellationToken ct = default);
    Task<PromotionApplicationResultDto> ApplyPromotionsAsync(ApplyPromotionRulesDto dto, CancellationToken ct = default);
}
