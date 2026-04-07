using Prym.Client.Services;
using Prym.DTOs.Business;
using Prym.DTOs.Common;

namespace Prym.Client.Shared.Management.Adapters;

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

    /// <summary>
    /// Loads all business parties by fetching all three party types concurrently and de-duplicating.
    /// Pagination parameters are intentionally ignored: this adapter uses client-side filtering
    /// (UseServerSidePaging = false), so EntityManagementPage always requests page=1, pageSize=MaxValue
    /// to load the full dataset in one shot.
    /// </summary>
    public async Task<PagedResult<BusinessPartyDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        // Load all business party types concurrently and de-duplicate by Id
        var customersTask = _businessPartyService.GetBusinessPartiesByTypeAsync(BusinessPartyType.Cliente);
        var suppliersTask = _businessPartyService.GetBusinessPartiesByTypeAsync(BusinessPartyType.Supplier);
        var bothTask = _businessPartyService.GetBusinessPartiesByTypeAsync(BusinessPartyType.Both);

        await Task.WhenAll(customersTask, suppliersTask, bothTask);

        var allParties = customersTask.Result.Concat(suppliersTask.Result).Concat(bothTask.Result)
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
