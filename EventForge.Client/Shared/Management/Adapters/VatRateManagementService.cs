using EventForge.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.VatRates;

namespace EventForge.Client.Shared.Management.Adapters;

public class VatRateManagementService : IEntityManagementService<VatRateDto>
{
    private readonly IFinancialService _financialService;

    public VatRateManagementService(IFinancialService financialService)
        => _financialService = financialService;

    public async Task<PagedResult<VatRateDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _financialService.GetVatRatesAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _financialService.DeleteVatRateAsync(id);
}
