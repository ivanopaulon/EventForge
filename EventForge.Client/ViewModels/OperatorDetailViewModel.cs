using EventForge.Client.Services.Store;
using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for Operator detail page following the ProductDetail pattern
/// </summary>
public class OperatorDetailViewModel : BaseEntityDetailViewModel<StoreUserDto, CreateStoreUserDto, UpdateStoreUserDto>
{
    private readonly IStoreUserService _storeUserService;
    private readonly IStoreUserGroupService _groupService;

    public OperatorDetailViewModel(
        IStoreUserService storeUserService,
        IStoreUserGroupService groupService,
        ILogger<OperatorDetailViewModel> logger)
        : base(logger)
    {
        _storeUserService = storeUserService;
        _groupService = groupService;
    }

    // Related entity collections
    public List<StoreUserGroupDto> AvailableGroups { get; private set; } = new();

    // Additional properties for create mode
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool PhotoConsent { get; set; }

    protected override StoreUserDto CreateNewEntity()
    {
        Username = string.Empty;
        Password = null;
        PhotoConsent = false;
        
        return new StoreUserDto
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Username = string.Empty,
            Status = CashierStatus.Active
        };
    }

    protected override async Task<StoreUserDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        var entity = await _storeUserService.GetByIdAsync(entityId);
        if (entity != null)
        {
            Username = entity.Username;
        }
        return entity;
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        try
        {
            AvailableGroups = await _groupService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading available groups for operator {OperatorId}", entityId);
            AvailableGroups = new List<StoreUserGroupDto>();
        }
    }

    protected override CreateStoreUserDto MapToCreateDto(StoreUserDto entity)
    {
        return new CreateStoreUserDto
        {
            Name = entity.Name,
            Username = Username,
            Email = entity.Email,
            PasswordHash = Password,
            Role = entity.Role,
            Status = entity.Status,
            Notes = entity.Notes,
            CashierGroupId = entity.CashierGroupId,
            PhoneNumber = entity.PhoneNumber,
            PhotoConsent = PhotoConsent
        };
    }

    protected override UpdateStoreUserDto MapToUpdateDto(StoreUserDto entity)
    {
        return new UpdateStoreUserDto
        {
            Name = entity.Name,
            Email = entity.Email,
            Role = entity.Role,
            Status = entity.Status,
            Notes = entity.Notes,
            CashierGroupId = entity.CashierGroupId,
            PhoneNumber = entity.PhoneNumber
        };
    }

    protected override Task<StoreUserDto?> CreateEntityAsync(CreateStoreUserDto createDto)
    {
        return _storeUserService.CreateAsync(createDto);
    }

    protected override Task<StoreUserDto?> UpdateEntityAsync(Guid entityId, UpdateStoreUserDto updateDto)
    {
        return _storeUserService.UpdateAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(StoreUserDto entity)
    {
        return entity.Id;
    }

    public new async Task LoadEntityAsync(Guid entityId)
    {
        // Load groups first before loading entity
        try
        {
            AvailableGroups = await _groupService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading available groups");
            AvailableGroups = new List<StoreUserGroupDto>();
        }

        await base.LoadEntityAsync(entityId);
    }
}
