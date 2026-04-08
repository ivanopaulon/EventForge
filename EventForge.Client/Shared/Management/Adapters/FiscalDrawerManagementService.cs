using EventForge.Client.Services.Store;
using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Client.Shared.Management.Adapters;

/// <summary>
/// Management service adapter for FiscalDrawer entities, bridging IFiscalDrawerService
/// to the generic IEntityManagementService interface used by EntityManagementPage.
/// </summary>
public class FiscalDrawerManagementService : IEntityManagementService<FiscalDrawerDto>
{
    private readonly IFiscalDrawerService _fiscalDrawerService;

    public FiscalDrawerManagementService(IFiscalDrawerService fiscalDrawerService)
        => _fiscalDrawerService = fiscalDrawerService;

    public async Task<PagedResult<FiscalDrawerDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
        => await _fiscalDrawerService.GetPagedAsync(page, pageSize, searchTerm) ?? new PagedResult<FiscalDrawerDto>();

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _fiscalDrawerService.DeleteAsync(id);
        if (!success)
            throw new InvalidOperationException($"Impossibile eliminare il cassetto fiscale {id}.");
    }
}
