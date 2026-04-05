using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of warehouse management service using HTTP client.
/// </summary>
public class WarehouseService(
    IHttpClientService httpClientService,
    ILogger<WarehouseService> logger) : IWarehouseService
{
    private const string BaseUrl = "api/v1/warehouse/facilities";

    public async Task<PagedResult<StorageFacilityDto>?> GetStorageFacilitiesAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            return await httpClientService.GetAsync<PagedResult<StorageFacilityDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage facilities");
            return null;
        }
    }

    public async Task<StorageFacilityDto?> GetStorageFacilityAsync(Guid id)
    {
        try
        {
            return await httpClientService.GetAsync<StorageFacilityDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage facility {FacilityId}", id);
            return null;
        }
    }

    public async Task<StorageFacilityDto?> CreateStorageFacilityAsync(CreateStorageFacilityDto dto)
    {
        try
        {
            return await httpClientService.PostAsync<CreateStorageFacilityDto, StorageFacilityDto>(BaseUrl, dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating storage facility");
            return null;
        }
    }

    public async Task<StorageFacilityDto?> UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto dto)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateStorageFacilityDto, StorageFacilityDto>($"{BaseUrl}/{id}", dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating storage facility {FacilityId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteStorageFacilityAsync(Guid id)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting storage facility {FacilityId}", id);
            return false;
        }
    }

    public async Task<EventForge.DTOs.Bulk.BulkTransferResultDto?> BulkTransferAsync(EventForge.DTOs.Bulk.BulkTransferDto bulkTransferDto, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Starting bulk transfer of {Count} items from facility {SourceId} to {DestinationId}",
                bulkTransferDto.Items.Count, bulkTransferDto.SourceFacilityId, bulkTransferDto.DestinationFacilityId);
            var result = await httpClientService.PostAsync<EventForge.DTOs.Bulk.BulkTransferDto, EventForge.DTOs.Bulk.BulkTransferResultDto>(
                "api/v1/warehouse/bulk-transfer",
                bulkTransferDto,
                ct);

            if (result is not null)
            {
                logger.LogInformation("Bulk transfer completed. Success: {SuccessCount}, Failed: {FailedCount}",
                    result.SuccessCount, result.FailedCount);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing bulk transfer");
            return null;
        }
    }
}
