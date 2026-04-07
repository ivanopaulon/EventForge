using EventForge.Client.Services.Store;
using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for Operator Group detail page following the ProductDetail pattern
/// </summary>
public class OperatorGroupDetailViewModel : BaseEntityDetailViewModel<StoreUserGroupDto, CreateStoreUserGroupDto, UpdateStoreUserGroupDto>
{
    private readonly IStoreUserGroupService _groupService;

    public OperatorGroupDetailViewModel(
        IStoreUserGroupService groupService,
        ILogger<OperatorGroupDetailViewModel> logger)
        : base(logger)
    {
        _groupService = groupService;
    }

    protected override StoreUserGroupDto CreateNewEntity()
    {
        return new StoreUserGroupDto
        {
            Id = Guid.Empty,
            Code = string.Empty,
            Name = string.Empty,
            Status = CashierGroupStatus.Active,
            IsSystemGroup = false,
            IsDefault = false
        };
    }

    protected override Task<StoreUserGroupDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return _groupService.GetByIdAsync(entityId);
    }

    protected override CreateStoreUserGroupDto MapToCreateDto(StoreUserGroupDto entity)
    {
        return new CreateStoreUserGroupDto
        {
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            ColorHex = entity.ColorHex,
            IsSystemGroup = entity.IsSystemGroup,
            IsDefault = entity.IsDefault
        };
    }

    protected override UpdateStoreUserGroupDto MapToUpdateDto(StoreUserGroupDto entity)
    {
        return new UpdateStoreUserGroupDto
        {
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            ColorHex = entity.ColorHex
        };
    }

    protected override Task<StoreUserGroupDto?> CreateEntityAsync(CreateStoreUserGroupDto createDto)
    {
        return _groupService.CreateAsync(createDto);
    }

    protected override Task<StoreUserGroupDto?> UpdateEntityAsync(Guid entityId, UpdateStoreUserGroupDto updateDto)
    {
        return _groupService.UpdateAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(StoreUserGroupDto entity)
    {
        return entity.Id;
    }
}
