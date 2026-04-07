using EventForge.DTOs.Common;
using EventForge.DTOs.Station;

namespace EventForge.Client.Services.Station;

/// <summary>
/// Client service implementation for managing stations.
/// </summary>
public class StationService(
    IHttpClientService httpClientService,
    ILogger<StationService> logger) : IStationService
{
    private const string ApiBase = "api/v1/stations";

    public async Task<List<StationDto>> GetAllAsync()
    {
        try
        {
            var result = await GetPagedAsync(1, 200);
            return result.Items?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting all stations, returning empty list");
            return [];
        }
    }

    public async Task<StationDto?> GetByIdAsync(Guid id)
    {
        return await httpClientService.GetAsync<StationDto>($"{ApiBase}/{id}");
    }

    public async Task<StationDto?> CreateAsync(CreateStationDto createDto)
    {
        return await httpClientService.PostAsync<CreateStationDto, StationDto>(ApiBase, createDto);
    }

    public async Task<StationDto?> UpdateAsync(Guid id, UpdateStationDto updateDto)
    {
        return await httpClientService.PutAsync<UpdateStationDto, StationDto>($"{ApiBase}/{id}", updateDto);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await httpClientService.DeleteAsync($"{ApiBase}/{id}");
        return true;
    }

    public async Task<PagedResult<StationDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        return await httpClientService.GetAsync<PagedResult<StationDto>>($"{ApiBase}?page={page}&pageSize={pageSize}")
            ?? new PagedResult<StationDto>();
    }
}
