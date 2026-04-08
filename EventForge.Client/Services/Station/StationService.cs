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
    private const string PrintersBase = "api/v1/stations/printers";

    public async Task<List<StationDto>> GetAllAsync(CancellationToken ct = default)
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

    public async Task<StationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClientService.GetAsync<StationDto>($"{ApiBase}/{id}");
    }

    public async Task<StationDto?> CreateAsync(CreateStationDto createDto, CancellationToken ct = default)
    {
        return await httpClientService.PostAsync<CreateStationDto, StationDto>(ApiBase, createDto);
    }

    public async Task<StationDto?> UpdateAsync(Guid id, UpdateStationDto updateDto, CancellationToken ct = default)
    {
        return await httpClientService.PutAsync<UpdateStationDto, StationDto>($"{ApiBase}/{id}", updateDto);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await httpClientService.DeleteAsync($"{ApiBase}/{id}");
        return true;
    }

    public async Task<PagedResult<StationDto>> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return await httpClientService.GetAsync<PagedResult<StationDto>>($"{ApiBase}?page={page}&pageSize={pageSize}")
            ?? new PagedResult<StationDto>();
    }

    // ── Printer methods ───────────────────────────────────────

    public async Task<List<PrinterDto>> GetAllPrintersAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<PagedResult<PrinterDto>>($"{PrintersBase}?page=1&pageSize=200");
            return result?.Items?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting all printers, returning empty list");
            return [];
        }
    }

    public async Task<PrinterDto?> GetPrinterByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClientService.GetAsync<PrinterDto>($"{PrintersBase}/{id}");
    }

    public async Task<PrinterDto?> CreatePrinterAsync(CreatePrinterDto createDto, CancellationToken ct = default)
    {
        return await httpClientService.PostAsync<CreatePrinterDto, PrinterDto>(PrintersBase, createDto);
    }

    public async Task<PrinterDto?> UpdatePrinterAsync(Guid id, UpdatePrinterDto updateDto, CancellationToken ct = default)
    {
        return await httpClientService.PutAsync<UpdatePrinterDto, PrinterDto>($"{PrintersBase}/{id}", updateDto);
    }
}
