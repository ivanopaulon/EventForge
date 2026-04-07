using Prym.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace Prym.Client.Shared.Management.Adapters;

/// <summary>
/// Adapter that wraps <see cref="IDocumentCounterService"/> for use with
/// <c>EntityManagementPage{TEntity}</c>.
/// </summary>
public class DocumentCounterManagementService : IEntityManagementService<DocumentCounterDto>
{
    private readonly IDocumentCounterService _service;

    public DocumentCounterManagementService(IDocumentCounterService service)
        => _service = service;

    public async Task<PagedResult<DocumentCounterDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var items = await _service.GetAllDocumentCountersAsync();
        var list = items?.ToList() ?? new List<DocumentCounterDto>();
        return new PagedResult<DocumentCounterDto> { Items = list, TotalCount = list.Count, Page = 1, PageSize = list.Count };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _service.DeleteDocumentCounterAsync(id);
        if (!success)
            throw new InvalidOperationException("Impossibile eliminare il contatore documento.");
    }
}
