using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Client.Services.Domain.Products;

/// <summary>
/// Service implementation for managing models.
/// </summary>
public class ModelService : IModelService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ModelService> _logger;
    private const string BaseUrl = "api/v1/product-management/models";

    public ModelService(IHttpClientService httpClientService, ILogger<ModelService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ModelDto>> GetModelsAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<ModelDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<ModelDto> { Items = new List<ModelDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models");
            throw;
        }
    }

    public async Task<PagedResult<ModelDto>> GetModelsByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 100)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<ModelDto>>($"{BaseUrl}?brandId={brandId}&page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<ModelDto> { Items = new List<ModelDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models for brand {BrandId}", brandId);
            throw;
        }
    }

    public async Task<ModelDto?> GetModelByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<ModelDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model with ID {Id}", id);
            throw;
        }
    }

    public async Task<ModelDto> CreateModelAsync(CreateModelDto createModelDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateModelDto, ModelDto>(BaseUrl, createModelDto);
            return result ?? throw new InvalidOperationException("Failed to create model");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating model");
            throw;
        }
    }

    public async Task<ModelDto?> UpdateModelAsync(Guid id, UpdateModelDto updateModelDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateModelDto, ModelDto>($"{BaseUrl}/{id}", updateModelDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating model with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteModelAsync(Guid id)
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
            _logger.LogError(ex, "Error deleting model with ID {Id}", id);
            throw;
        }
    }
}
