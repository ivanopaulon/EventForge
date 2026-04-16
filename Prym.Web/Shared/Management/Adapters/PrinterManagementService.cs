using Prym.Web.Services.Station;
using Prym.DTOs.Common;
using Prym.DTOs.Station;

namespace Prym.Web.Shared.Management.Adapters;

/// <summary>
/// Management service adapter for Printer entities, bridging IStationService
/// to the generic IEntityManagementService interface used by EntityManagementPage.
/// </summary>
public class PrinterManagementService : IEntityManagementService<PrinterDto>
{
    private readonly IStationService _stationService;

    public PrinterManagementService(IStationService stationService)
        => _stationService = stationService;

    public async Task<PagedResult<PrinterDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
        => await _stationService.GetPagedPrintersAsync(page, pageSize, searchTerm, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _stationService.DeletePrinterAsync(id, ct);
        if (!success)
            throw new InvalidOperationException($"Impossibile eliminare la stampante {id}.");
    }
}
