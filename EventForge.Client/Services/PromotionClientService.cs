using EventForge.DTOs.Common;
using EventForge.DTOs.Promotions;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side service implementation for managing promotions via HTTP calls.
/// </summary>
public class PromotionClientService(
    IHttpClientService httpClientService,
    ILogger<PromotionClientService> logger) : IPromotionClientService
{
    private const string BaseUrl = "api/v1/product-management/promotions";

    public async Task<PagedResult<PromotionDto>> GetPagedAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<PagedResult<PromotionDto>>(
                $"{BaseUrl}?page={page}&pageSize={pageSize}", ct);

            return result ?? new PagedResult<PromotionDto>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving promotions (page={Page}, pageSize={PageSize})", page, pageSize);
            throw;
        }
    }

    public async Task<PromotionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<PromotionDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving promotion with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PromotionDto>> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<IEnumerable<PromotionDto>>($"{BaseUrl}/active", ct);
            return result ?? Enumerable.Empty<PromotionDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active promotions");
            throw;
        }
    }

    public async Task<PromotionDto> CreateAsync(CreatePromotionDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreatePromotionDto, PromotionDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create promotion");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating promotion");
            throw;
        }
    }

    public async Task<PromotionDto?> UpdateAsync(Guid id, UpdatePromotionDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdatePromotionDto, PromotionDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating promotion with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting promotion with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PromotionRuleDto>> GetRulesAsync(Guid promotionId, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<IEnumerable<PromotionRuleDto>>($"{BaseUrl}/{promotionId}/rules", ct);
            return result ?? Enumerable.Empty<PromotionRuleDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving rules for promotion {PromotionId}", promotionId);
            throw;
        }
    }

    public async Task<PromotionRuleDto> AddRuleAsync(Guid promotionId, CreatePromotionRuleDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreatePromotionRuleDto, PromotionRuleDto>($"{BaseUrl}/{promotionId}/rules", dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create promotion rule");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding rule to promotion {PromotionId}", promotionId);
            throw;
        }
    }

    public async Task<PromotionRuleDto?> UpdateRuleAsync(Guid promotionId, Guid ruleId, UpdatePromotionRuleDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdatePromotionRuleDto, PromotionRuleDto>($"{BaseUrl}/{promotionId}/rules/{ruleId}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating rule {RuleId} for promotion {PromotionId}", ruleId, promotionId);
            throw;
        }
    }

    public async Task<bool> DeleteRuleAsync(Guid promotionId, Guid ruleId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{promotionId}/rules/{ruleId}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting rule {RuleId} for promotion {PromotionId}", ruleId, promotionId);
            throw;
        }
    }

    public async Task<IEnumerable<PromotionRuleProductDto>> GetRuleProductsAsync(Guid promotionId, Guid ruleId, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<IEnumerable<PromotionRuleProductDto>>($"{BaseUrl}/{promotionId}/rules/{ruleId}/products", ct);
            return result ?? Enumerable.Empty<PromotionRuleProductDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products for rule {RuleId}", ruleId);
            throw;
        }
    }

    public async Task<PromotionRuleProductDto> AddRuleProductAsync(Guid promotionId, Guid ruleId, CreatePromotionRuleProductDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreatePromotionRuleProductDto, PromotionRuleProductDto>($"{BaseUrl}/{promotionId}/rules/{ruleId}/products", dto, ct);
            return result ?? throw new InvalidOperationException("Failed to add product to rule");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding product to rule {RuleId}", ruleId);
            throw;
        }
    }

    public async Task<bool> RemoveRuleProductAsync(Guid promotionId, Guid ruleId, Guid productId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{promotionId}/rules/{ruleId}/products/{productId}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing product from rule {RuleId}", ruleId);
            throw;
        }
    }

    public async Task<PromotionDto?> ValidateCouponCodeAsync(string couponCode, CancellationToken ct = default)
    {
        try
        {
            var dto = new ValidateCouponRequestDto { CouponCode = couponCode };
            return await httpClientService.PostAsync<ValidateCouponRequestDto, PromotionDto>($"{BaseUrl}/validate-coupon", dto, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating coupon code");
            throw;
        }
    }

    public async Task<PromotionApplicationResultDto> ApplyPromotionsAsync(ApplyPromotionRulesDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<ApplyPromotionRulesDto, PromotionApplicationResultDto>($"{BaseUrl}/apply", dto, ct);
            return result ?? new PromotionApplicationResultDto { Success = false };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying promotions");
            throw;
        }
    }
}
