using EventForge.Client.Services.Store;
using EventForge.DTOs.Common;
using EventForge.DTOs.PaymentTerminal;

namespace EventForge.Client.Shared.Management.Adapters;

/// <summary>
/// Management service adapter for PaymentTerminal entities.
/// Bridges <see cref="IPaymentTerminalService"/> to the generic
/// <see cref="IEntityManagementService{T}"/> interface used by <c>EntityManagementPage</c>.
/// </summary>
public class PaymentTerminalManagementService(IPaymentTerminalService paymentTerminalService)
    : IEntityManagementService<PaymentTerminalDto>
{
    public async Task<PagedResult<PaymentTerminalDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        var all = await paymentTerminalService.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            all = all.Where(t =>
                t.Name.ToLowerInvariant().Contains(term) ||
                (t.Description?.ToLowerInvariant().Contains(term) ?? false) ||
                (t.IpAddress?.Contains(term) ?? false) ||
                (t.TerminalId?.Contains(term) ?? false)).ToList();
        }

        var totalCount = all.Count;
        var items = all
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<PaymentTerminalDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await paymentTerminalService.DeleteAsync(id);
        if (!success)
            throw new InvalidOperationException($"Impossibile eliminare il terminale POS {id}.");
    }
}
