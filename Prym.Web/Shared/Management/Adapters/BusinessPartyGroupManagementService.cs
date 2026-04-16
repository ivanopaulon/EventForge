using Prym.Web.Services;
using Prym.DTOs.Business;
using Prym.DTOs.Common;

namespace Prym.Web.Shared.Management.Adapters;

public class BusinessPartyGroupManagementService : IEntityManagementService<BusinessPartyGroupDto>
{
    private readonly IBusinessPartyGroupService _groupService;

    public BusinessPartyGroupManagementService(IBusinessPartyGroupService groupService)
        => _groupService = groupService;

    public async Task<PagedResult<BusinessPartyGroupDto>> GetPagedAsync(
        int page, int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default)
        => await _groupService.GetGroupsAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _groupService.DeleteGroupAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete business party group {id}");
    }
}
