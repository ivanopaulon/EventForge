using Prym.Web.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace Prym.Web.Shared.Management.Adapters;

public class DocumentTypeManagementService : IEntityManagementService<DocumentTypeDto>
{
    private readonly IDocumentTypeService _documentTypeService;

    public DocumentTypeManagementService(IDocumentTypeService documentTypeService)
        => _documentTypeService = documentTypeService;

    public async Task<PagedResult<DocumentTypeDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var items = await _documentTypeService.GetAllDocumentTypesAsync();
        var list = items?.ToList() ?? new List<DocumentTypeDto>();
        return new PagedResult<DocumentTypeDto>
        {
            Items = list,
            Page = 1,
            PageSize = list.Count,
            TotalCount = list.Count
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _documentTypeService.DeleteDocumentTypeAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete document type {id}");
    }
}
