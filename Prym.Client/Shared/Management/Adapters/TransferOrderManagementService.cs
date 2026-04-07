using Prym.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace Prym.Client.Shared.Management.Adapters;

/// <summary>
/// Adapter that wraps <see cref="ITransferOrderService"/> for use with
/// <see cref="EntityManagementPage{TEntity}"/>.
/// All transfer orders are loaded client-side so that filtering, search and
/// quick-filters operate without additional server round-trips.
/// "Delete" is mapped to <c>CancelTransferOrderAsync</c> because transfer orders
/// are never physically deleted — they are cancelled.
/// </summary>
public class TransferOrderManagementService : IEntityManagementService<TransferOrderDto>
{
    private readonly ITransferOrderService _service;

    public TransferOrderManagementService(ITransferOrderService service)
        => _service = service;

    public async Task<PagedResult<TransferOrderDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var result = await _service.GetTransferOrdersAsync(1, int.MaxValue, cancellationToken: ct);
        var list = result?.Items?.ToList() ?? new List<TransferOrderDto>();
        return new PagedResult<TransferOrderDto> { Items = list, TotalCount = list.Count, Page = 1, PageSize = list.Count };
    }

    /// <summary>
    /// "Deletes" a transfer order by cancelling it.
    /// Only <c>Pending</c> orders should be passed here (enforced by <c>CanDelete</c> in the config).
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _service.CancelTransferOrderAsync(id, ct);
        if (!success)
            throw new InvalidOperationException("Impossibile annullare l'ordine di trasferimento.");
    }
}
