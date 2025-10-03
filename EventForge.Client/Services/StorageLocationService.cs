using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of storage location management service using HTTP client.
/// </summary>
public class StorageLocationService : IStorageLocationService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<StorageLocationService> _logger;
    private const string BaseUrl = "api/v1/warehouse/locations";

    public StorageLocationService(IHttpClientService httpClientService, ILogger<StorageLocationService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<StorageLocationDto>?> GetStorageLocationsAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            return await _httpClientService.GetAsync<PagedResult<StorageLocationDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage locations");
            return null;
        }
    }

    public async Task<PagedResult<StorageLocationDto>?> GetStorageLocationsByWarehouseAsync(Guid warehouseId, int page = 1, int pageSize = 100)
    {
        try
        {
            return await _httpClientService.GetAsync<PagedResult<StorageLocationDto>>($"{BaseUrl}?facilityId={warehouseId}&page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage locations for warehouse {WarehouseId}", warehouseId);
            return null;
        }
    }

    public async Task<StorageLocationDto?> GetStorageLocationAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<StorageLocationDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage location {LocationId}", id);
            return null;
        }
    }

    public async Task<StorageLocationDto?> CreateStorageLocationAsync(CreateStorageLocationDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateStorageLocationDto, StorageLocationDto>(BaseUrl, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storage location");
            return null;
        }
    }

    public async Task<StorageLocationDto?> UpdateStorageLocationAsync(Guid id, UpdateStorageLocationDto dto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateStorageLocationDto, StorageLocationDto>($"{BaseUrl}/{id}", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating storage location {LocationId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteStorageLocationAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting storage location {LocationId}", id);
            return false;
        }
    }
}
