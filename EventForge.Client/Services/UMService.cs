using EventForge.DTOs.Common;
using EventForge.DTOs.UnitOfMeasures;
using System.Net;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing units of measure.
/// </summary>
public class UMService : IUMService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<UMService> _logger;
    private const string BaseUrl = "api/v1/product-management/units";

    public UMService(IHttpClientService httpClientService, ILogger<UMService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<UMDto>> GetUMsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var result = await _httpClientService.GetAsync<PagedResult<UMDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
        return result ?? new PagedResult<UMDto> { Items = new List<UMDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync(CancellationToken ct = default)
    {
        try
        {
            // Get all active units (use max allowed page size of 100)
            var result = await GetUMsAsync(1, 100, ct);
            return result?.Items?.Where(um => um.IsActive) ?? Enumerable.Empty<UMDto>();
        }
        catch (HttpRequestException)
        {
            // Fallback on HTTP error (already logged by HttpClientService)
            return Enumerable.Empty<UMDto>();
        }
    }

    public async Task<UMDto?> GetUMByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _httpClientService.GetAsync<UMDto>($"{BaseUrl}/{id}", ct);
    }

    public async Task<UMDto> CreateUMAsync(CreateUMDto createUMDto, CancellationToken ct = default)
    {
        var result = await _httpClientService.PostAsync<CreateUMDto, UMDto>(BaseUrl, createUMDto, ct);
        return result ?? throw new InvalidOperationException("Failed to create unit of measure");
    }

    public async Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto, CancellationToken ct = default)
    {
        return await _httpClientService.PutAsync<UpdateUMDto, UMDto>($"{BaseUrl}/{id}", updateUMDto, ct);
    }

    public async Task<bool> DeleteUMAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Business logic: return false if not found (no logging)
            return false;
        }
    }
}
