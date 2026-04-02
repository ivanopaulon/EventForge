using EventForge.DTOs.Common;
using EventForge.DTOs.Promotions;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side service implementation for managing promotions via HTTP calls.
/// </summary>
public class PromotionClientService : IPromotionClientService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<PromotionClientService> _logger;
    private const string BaseUrl = "api/v1/product-management/promotions";

    public PromotionClientService(IHttpClientService httpClientService, ILogger<PromotionClientService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<PromotionDto>> GetPagedAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<PromotionDto>>(
                $"{BaseUrl}?page={page}&pageSize={pageSize}", ct);

            return result ?? new PagedResult<PromotionDto>
            {
                Items = new List<PromotionDto>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotions (page={Page}, pageSize={PageSize})", page, pageSize);
            throw;
        }
    }

    public async Task<PromotionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<PromotionDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving promotion with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PromotionDto>> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.GetAsync<IEnumerable<PromotionDto>>($"{BaseUrl}/active", ct);
            return result ?? Enumerable.Empty<PromotionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active promotions");
            throw;
        }
    }

    public async Task<PromotionDto> CreateAsync(CreatePromotionDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreatePromotionDto, PromotionDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create promotion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promotion");
            throw;
        }
    }

    public async Task<PromotionDto?> UpdateAsync(Guid id, UpdatePromotionDto dto, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdatePromotionDto, PromotionDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating promotion with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting promotion with ID {Id}", id);
            throw;
        }
    }
}
