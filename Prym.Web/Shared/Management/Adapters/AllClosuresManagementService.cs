using Prym.DTOs.Common;
using Prym.DTOs.FiscalPrinting;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class AllClosuresManagementService(IFiscalPrintingService fiscalPrintingService)
    : IEntityManagementService<DailyClosureHistoryDto>
{
    public async Task<PagedResult<DailyClosureHistoryDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = (await fiscalPrintingService.GetAllClosureHistoryAsync(1, 500) ?? [])
            .OrderByDescending(c => c.ClosedAt)
            .ToList();

        // External filters
        if (filters != null)
        {
            if (filters.TryGetValue("From", out var rawFrom) && rawFrom is DateTime from)
                all = all.Where(c => c.ClosedAt >= from).ToList();

            if (filters.TryGetValue("To", out var rawTo) && rawTo is DateTime to)
                all = all.Where(c => c.ClosedAt <= to.AddDays(1)).ToList();

            if (filters.TryGetValue("ClosureType", out var rawType) && rawType is ClosureType closureType)
                all = all.Where(c => c.ClosureType == closureType).ToList();

            if (filters.TryGetValue("Pending", out var rawPending) && rawPending is bool pending)
                all = all.Where(c => c.FiscalClosurePending == pending).ToList();
        }

        // Text search: printer name, operator, Z-report number
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(c =>
                (c.PrinterName ?? string.Empty).ToUpperInvariant().Contains(term) ||
                (c.Operator ?? string.Empty).ToUpperInvariant().Contains(term) ||
                c.ZReportNumber.ToString().Contains(term)).ToList();
        }

        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<DailyClosureHistoryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotSupportedException("Closure history records cannot be deleted.");
}
