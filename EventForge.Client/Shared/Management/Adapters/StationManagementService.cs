using EventForge.Client.Services.Station;
using Prym.DTOs.Common;
using Prym.DTOs.Station;

namespace EventForge.Client.Shared.Management.Adapters;

public class StationManagementService : IEntityManagementService<StationDto>
{
    private readonly IStationService _stationService;

    public StationManagementService(IStationService stationService)
        => _stationService = stationService;

    public async Task<PagedResult<StationDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _stationService.GetPagedAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _stationService.DeleteAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete station {id}");
    }
}
