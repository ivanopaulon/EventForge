using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.VatRates;

namespace EventForge.Client.Shared.Management.Adapters;

public class VatNatureManagementService : IEntityManagementService<VatNatureDto>
{
    private readonly IFinancialService _financialService;

    public VatNatureManagementService(IFinancialService financialService)
        => _financialService = financialService;

    public async Task<PagedResult<VatNatureDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _financialService.GetVatNaturesAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _financialService.DeleteVatNatureAsync(id);
}
