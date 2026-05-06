using Prym.Web.Services;
using Prym.DTOs.Common;

namespace Prym.Web.Shared.Management.Adapters;

/// <summary>
/// Management adapter that loads the full classification node tree and flattens it
/// for display in EntityManagementPage, preserving visual hierarchy via indented names.
/// </summary>
public class ClassificationNodeManagementService : IEntityManagementService<ClassificationNodeDto>
{
    private readonly IEntityManagementService _entityManagementService;

    public ClassificationNodeManagementService(IEntityManagementService entityManagementService)
        => _entityManagementService = entityManagementService;

    public async Task<PagedResult<ClassificationNodeDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        // Load the full tree in one request so child nodes appear in the list
        var tree = (await _entityManagementService.GetClassificationNodesTreeAsync(ct)).ToList();

        // Flatten the tree depth-first, injecting indentation into the Name for display
        var flat = new List<ClassificationNodeDto>();
        FlattenTree(tree, flat, depth: 0);

        // Apply optional search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lower = searchTerm.ToLower();
            flat = flat.Where(n =>
                (n.Name?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (n.Code?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        return new PagedResult<ClassificationNodeDto>
        {
            Items = flat,
            Page = 1,
            PageSize = flat.Count,
            TotalCount = flat.Count
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _entityManagementService.DeleteClassificationNodeAsync(id, ct);

    private static void FlattenTree(IEnumerable<ClassificationNodeDto> nodes, List<ClassificationNodeDto> result, int depth)
    {
        foreach (var node in nodes)
        {
            // Create a display copy with indented name so the flat table shows the hierarchy
            var display = new ClassificationNodeDto
            {
                Id = node.Id,
                Code = node.Code,
                Name = (depth > 0 ? new string(' ', depth * 2) + "• " : string.Empty) + node.Name,
                Description = node.Description,
                Type = node.Type,
                Status = node.Status,
                Level = node.Level,
                Order = node.Order,
                ParentId = node.ParentId,
                ApplicableTo = node.ApplicableTo,
                IsActive = node.IsActive,
                CreatedAt = node.CreatedAt,
                CreatedBy = node.CreatedBy,
                ModifiedAt = node.ModifiedAt,
                ModifiedBy = node.ModifiedBy
            };
            result.Add(display);

            if (node.Children?.Count > 0)
                FlattenTree(node.Children, result, depth + 1);
        }
    }
}
