using Prym.Web.Services.Store;
using Prym.DTOs.Common;
using Prym.DTOs.Store;

namespace Prym.Web.ViewModels;

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

    // Additional property for both create and edit
    public DateTime? DateOfBirth { get; set; }

    protected override StoreUserDto CreateNewEntity()
    {
        Username = string.Empty;
        Password = null;
        PhotoConsent = false;
        DateOfBirth = null;

        return new StoreUserDto
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Username = string.Empty,
            Status = CashierStatus.Active
        };
    }

    protected override async Task<StoreUserDto?> LoadEntityFromServiceAsync(Guid entityId, CancellationToken ct = default)
    {
        var entity = await _storeUserService.GetByIdAsync(entityId, ct);
        if (entity != null)
        {
            Username = entity.Username;
            DateOfBirth = entity.DateOfBirth;
        }
        return entity;
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId, CancellationToken ct = default)
    {
        // Load available groups for both new and existing entities
        try
        {
            AvailableGroups = await _groupService.GetAllAsync(ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading available groups for operator");
            AvailableGroups = new List<StoreUserGroupDto>();
        }
    }

    // Override to ensure groups are loaded even for new entities
    public new async Task LoadEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        // Always load groups first, regardless of whether it's a new or existing entity
        await LoadRelatedEntitiesAsync(entityId, ct);

        // Call base implementation
        await base.LoadEntityAsync(entityId, ct);
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
            PhotoConsent = PhotoConsent,
            DateOfBirth = DateOfBirth
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
            PhoneNumber = entity.PhoneNumber,
            DateOfBirth = DateOfBirth
        };
    }

    protected override Task<StoreUserDto?> CreateEntityAsync(CreateStoreUserDto createDto, CancellationToken ct = default)
    {
        return _storeUserService.CreateAsync(createDto, ct);
    }

    protected override Task<StoreUserDto?> UpdateEntityAsync(Guid entityId, UpdateStoreUserDto updateDto, CancellationToken ct = default)
    {
        return _storeUserService.UpdateAsync(entityId, updateDto, ct);
    }

    protected override Guid GetEntityId(StoreUserDto entity)
    {
        return entity.Id;
    }
}
