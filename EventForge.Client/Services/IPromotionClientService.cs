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
}
