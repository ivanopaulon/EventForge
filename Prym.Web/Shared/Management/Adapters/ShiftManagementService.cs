using Prym.DTOs.Common;
using Prym.DTOs.Store;
using Prym.Web.Services.Store;

namespace Prym.Web.Shared.Management.Adapters;

public class ShiftManagementService(IShiftService shiftService)
    : IEntityManagementService<CashierShiftDto>
{
    public async Task<PagedResult<CashierShiftDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var from = DateOnly.FromDateTime(DateTime.Today);
        var to = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        Guid? operatorId = null;

        if (filters != null)
        {
            if (filters.TryGetValue("From", out var rawFrom) && rawFrom is DateOnly dateFrom)
                from = dateFrom;
            if (filters.TryGetValue("To", out var rawTo) && rawTo is DateOnly dateTo)
                to = dateTo;
            if (filters.TryGetValue("OperatorId", out var rawOp) && rawOp is Guid opId)
                operatorId = opId;
        }

        List<CashierShiftDto> all;
        if (operatorId.HasValue)
            all = await shiftService.GetShiftsByOperatorAsync(operatorId.Value, from, to, ct);
        else
            all = await shiftService.GetShiftsAsync(from, to, ct);

        // Client-side text search
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            all = all.Where(s =>
                s.StoreUserName.ToUpperInvariant().Contains(term) ||
                (s.PosName != null && s.PosName.ToUpperInvariant().Contains(term)) ||
                (s.Notes != null && s.Notes.ToUpperInvariant().Contains(term))).ToList();
        }

        var totalCount = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<CashierShiftDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await shiftService.DeleteAsync(id, ct);
}
