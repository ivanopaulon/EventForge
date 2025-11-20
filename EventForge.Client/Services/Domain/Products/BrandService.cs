using EventForge.DTOs.Common;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;
using EventForge.DTOs.Products;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Domain.Products;

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

    public async Task<PagedResult<BrandDto>> GetBrandsAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<BrandDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<BrandDto> { Items = new List<BrandDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brands");
            throw;
        }
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<BrandDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brand with ID {Id}", id);
            throw;
        }
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateBrandDto, BrandDto>(BaseUrl, createBrandDto);
            return result ?? throw new InvalidOperationException("Failed to create brand");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating brand");
            throw;
        }
    }

    public async Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateBrandDto, BrandDto>($"{BaseUrl}/{id}", updateBrandDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteBrandAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand with ID {Id}", id);
            throw;
        }
    }
}
