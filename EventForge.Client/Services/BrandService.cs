using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using System.Net;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing brands.
/// </summary>
public class BrandService : IBrandService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<BrandService> _logger;
    private const string BaseUrl = "api/v1/product-management/brands";

    public BrandService(IHttpClientService httpClientService, ILogger<BrandService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<BrandDto>> GetBrandsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var result = await _httpClientService.GetAsync<PagedResult<BrandDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
        return result ?? new PagedResult<BrandDto> { Items = new List<BrandDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _httpClientService.GetAsync<BrandDto>($"{BaseUrl}/{id}", ct);
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, CancellationToken ct = default)
    {
        var result = await _httpClientService.PostAsync<CreateBrandDto, BrandDto>(BaseUrl, createBrandDto, ct);
        return result ?? throw new InvalidOperationException("Failed to create brand");
    }

    public async Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, CancellationToken ct = default)
    {
        return await _httpClientService.PutAsync<UpdateBrandDto, BrandDto>($"{BaseUrl}/{id}", updateBrandDto, ct);
    }

    public async Task<bool> DeleteBrandAsync(Guid id, CancellationToken ct = default)
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
