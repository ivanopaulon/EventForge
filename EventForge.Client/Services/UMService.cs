using EventForge.DTOs.Common;
using EventForge.DTOs.UnitOfMeasures;

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

    public async Task<PagedResult<UMDto>> GetUMsAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<UMDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<UMDto> { Items = new List<UMDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving units of measure");
            throw;
        }
    }

    public async Task<UMDto?> GetUMByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<UMDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unit of measure with ID {Id}", id);
            throw;
        }
    }

    public async Task<UMDto> CreateUMAsync(CreateUMDto createUMDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateUMDto, UMDto>(BaseUrl, createUMDto);
            return result ?? throw new InvalidOperationException("Failed to create unit of measure");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating unit of measure");
            throw;
        }
    }

    public async Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateUMDto, UMDto>($"{BaseUrl}/{id}", updateUMDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unit of measure with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteUMAsync(Guid id)
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
            _logger.LogError(ex, "Error deleting unit of measure with ID {Id}", id);
            throw;
        }
    }
}
