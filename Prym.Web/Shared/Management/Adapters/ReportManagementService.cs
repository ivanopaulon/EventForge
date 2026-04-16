using Prym.Web.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Reports;

namespace Prym.Web.Shared.Management.Adapters;

public class ReportManagementService(IReportDefinitionService reportService) : IEntityManagementService<ReportListItemDto>
{
    public async Task<PagedResult<ReportListItemDto>> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null,
        Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var result = await reportService.GetReportsAsync(
            searchTerm: searchTerm,
            page: page,
            pageSize: pageSize,
            ct: ct);
        return result ?? new PagedResult<ReportListItemDto>();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await reportService.DeleteReportAsync(id, ct);
}
