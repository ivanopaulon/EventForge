using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of warehouse management service using HTTP client.
/// </summary>
public class WarehouseService : IWarehouseService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<WarehouseService> _logger;
    private const string BaseUrl = "api/v1/warehouse/facilities";

    public WarehouseService(IHttpClientService httpClientService, ILogger<WarehouseService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<StorageFacilityDto>?> GetStorageFacilitiesAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            return await _httpClientService.GetAsync<PagedResult<StorageFacilityDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage facilities");
            return null;
        }
    }

    public async Task<StorageFacilityDto?> GetStorageFacilityAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<StorageFacilityDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage facility {FacilityId}", id);
            return null;
        }
    }

    public async Task<StorageFacilityDto?> CreateStorageFacilityAsync(CreateStorageFacilityDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateStorageFacilityDto, StorageFacilityDto>(BaseUrl, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storage facility");
            return null;
        }
    }

    public async Task<StorageFacilityDto?> UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto dto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateStorageFacilityDto, StorageFacilityDto>($"{BaseUrl}/{id}", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating storage facility {FacilityId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteStorageFacilityAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting storage facility {FacilityId}", id);
            return false;
        }
    }
}
