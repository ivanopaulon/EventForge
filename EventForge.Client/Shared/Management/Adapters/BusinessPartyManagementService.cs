using EventForge.Client.Services;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;

namespace EventForge.Client.Shared.Management.Adapters;

public class BusinessPartyManagementService : IEntityManagementService<BusinessPartyDto>
{
    private readonly IBusinessPartyService _businessPartyService;

    /// <summary>
    /// Cached list of all loaded business parties, available to the parent page
    /// (e.g. for BulkVatUpdateDialog which needs AllParties).
    /// Populated after each GetPagedAsync call.
    /// </summary>
    public List<BusinessPartyDto> CachedItems { get; private set; } = new();

    public BusinessPartyManagementService(IBusinessPartyService businessPartyService)
        => _businessPartyService = businessPartyService;

    public async Task<PagedResult<BusinessPartyDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        // Load all business party types and de-duplicate by Id
        var customers = await _businessPartyService.GetBusinessPartiesByTypeAsync(BusinessPartyType.Cliente);
        var suppliers = await _businessPartyService.GetBusinessPartiesByTypeAsync(BusinessPartyType.Supplier);
        var both = await _businessPartyService.GetBusinessPartiesByTypeAsync(BusinessPartyType.Both);

        var allParties = customers.Concat(suppliers).Concat(both)
            .GroupBy(bp => bp.Id)
            .Select(g => g.First())
            .OrderBy(bp => bp.Name)
            .ToList();

        CachedItems = allParties;

        return new PagedResult<BusinessPartyDto>
        {
            Items = allParties,
            TotalCount = allParties.Count,
            Page = 1,
            PageSize = allParties.Count
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _businessPartyService.DeleteBusinessPartyAsync(id);
}
