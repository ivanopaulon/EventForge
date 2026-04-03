using EventForge.DTOs.Common;
using EventForge.DTOs.Promotions;

namespace EventForge.Client.Services;

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
}
