using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace Prym.Web.Services;

/// <summary>
/// Implementation of storage location management service using HTTP client.
/// </summary>
public class StorageLocationService(
    IHttpClientService httpClientService,
    ILogger<StorageLocationService> logger) : IStorageLocationService
{
    private const string BaseUrl = "api/v1/warehouse/locations";

    public async Task<PagedResult<StorageLocationDto>?> GetStorageLocationsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<PagedResult<StorageLocationDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage locations");
            return null;
        }
    }

    public async Task<PagedResult<StorageLocationDto>?> GetStorageLocationsByWarehouseAsync(Guid warehouseId, int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<PagedResult<StorageLocationDto>>($"{BaseUrl}?facilityId={warehouseId}&page={page}&pageSize={pageSize}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage locations for warehouse {WarehouseId}", warehouseId);
            return null;
        }
    }

    public async Task<StorageLocationDto?> GetStorageLocationAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<StorageLocationDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage location {LocationId}", id);
            return null;
        }
    }

    public async Task<StorageLocationDto?> CreateStorageLocationAsync(CreateStorageLocationDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateStorageLocationDto, StorageLocationDto>(BaseUrl, dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating storage location");
            return null;
        }
    }

    public async Task<StorageLocationDto?> UpdateStorageLocationAsync(Guid id, UpdateStorageLocationDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateStorageLocationDto, StorageLocationDto>($"{BaseUrl}/{id}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating storage location {LocationId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteStorageLocationAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting storage location {LocationId}", id);
            return false;
        }
    }
}
