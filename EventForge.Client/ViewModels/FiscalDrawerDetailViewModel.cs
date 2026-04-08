using EventForge.Client.Services.Store;
using EventForge.DTOs.Store;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for FiscalDrawer detail dialog following the standard BaseEntityDetailViewModel pattern.
/// </summary>
public class FiscalDrawerDetailViewModel : BaseEntityDetailViewModel<FiscalDrawerDto, CreateFiscalDrawerDto, UpdateFiscalDrawerDto>
{
    private readonly IFiscalDrawerService _fiscalDrawerService;
    private readonly IStorePosService _storePosService;
    private readonly IStoreUserService _storeUserService;

    public FiscalDrawerDetailViewModel(
        IFiscalDrawerService fiscalDrawerService,
        IStorePosService storePosService,
        IStoreUserService storeUserService,
        ILogger<FiscalDrawerDetailViewModel> logger)
        : base(logger)
    {
        _fiscalDrawerService = fiscalDrawerService;
        _storePosService = storePosService;
        _storeUserService = storeUserService;
    }

    // Related entity collections loaded for the assignment dropdowns
    public List<StorePosDto> AvailablePos { get; private set; } = [];
    public List<StoreUserDto> AvailableOperators { get; private set; } = [];

    // Cash denominations for the current drawer (loaded on edit)
    public List<CashDenominationDto> CashDenominations { get; private set; } = [];

    protected override FiscalDrawerDto CreateNewEntity() => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        AssignmentType = FiscalDrawerAssignmentType.Fixed,
        CurrencyCode = "EUR",
        Status = FiscalDrawerStatus.Active,
        OpeningBalance = 0,
        CurrentBalance = 0
    };

    protected override Task<FiscalDrawerDto?> LoadEntityFromServiceAsync(Guid entityId)
        => _fiscalDrawerService.GetByIdAsync(entityId);

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        try
        {
            var posTask = _storePosService.GetActiveAsync();
            var opTask = _storeUserService.GetAllAsync();
            await Task.WhenAll(posTask, opTask);
            AvailablePos = posTask.Result;
            AvailableOperators = opTask.Result;

            // Load denominations only for existing drawers (entityId is a real GUID, not empty)
            if (entityId != Guid.Empty)
                CashDenominations = await _fiscalDrawerService.GetDenominationsAsync(entityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for FiscalDrawer");
            AvailablePos = [];
            AvailableOperators = [];
        }
    }

    // Override to ensure related entities (POS, operators) are also loaded for NEW entities,
    // since the base class only calls LoadRelatedEntitiesAsync for existing entities.
    public new async Task LoadEntityAsync(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            // New entity: base won't call LoadRelatedEntitiesAsync, so load here first
            await LoadRelatedEntitiesAsync(Guid.Empty);
        }
        await base.LoadEntityAsync(entityId);
    }

    protected override CreateFiscalDrawerDto MapToCreateDto(FiscalDrawerDto entity) => new()
    {
        Name = entity.Name,
        Code = entity.Code,
        Description = entity.Description,
        AssignmentType = entity.AssignmentType,
        CurrencyCode = entity.CurrencyCode,
        Status = entity.Status,
        OpeningBalance = entity.OpeningBalance,
        PosId = entity.PosId,
        OperatorId = entity.OperatorId,
        Notes = entity.Notes
    };

    protected override UpdateFiscalDrawerDto MapToUpdateDto(FiscalDrawerDto entity) => new()
    {
        Name = entity.Name,
        Code = entity.Code,
        Description = entity.Description,
        AssignmentType = entity.AssignmentType,
        CurrencyCode = entity.CurrencyCode,
        Status = entity.Status,
        PosId = entity.PosId,
        OperatorId = entity.OperatorId,
        Notes = entity.Notes
    };

    protected override Task<FiscalDrawerDto?> CreateEntityAsync(CreateFiscalDrawerDto createDto)
        => _fiscalDrawerService.CreateAsync(createDto);

    protected override Task<FiscalDrawerDto?> UpdateEntityAsync(Guid entityId, UpdateFiscalDrawerDto updateDto)
        => _fiscalDrawerService.UpdateAsync(entityId, updateDto);

    protected override Guid GetEntityId(FiscalDrawerDto entity) => entity.Id;

    /// <summary>Initializes standard denominations for the current currency.</summary>
    public async Task<bool> InitializeDenominationsAsync()
    {
        if (Entity is null) return false;
        try
        {
            CashDenominations = await _fiscalDrawerService.InitializeDenominationsAsync(Entity.Id, Entity.CurrencyCode);
            NotifyStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing denominations");
            return false;
        }
    }

    /// <summary>Updates the quantity of a cash denomination.</summary>
    public async Task<bool> UpdateDenominationQuantityAsync(Guid denominationId, int quantity)
    {
        try
        {
            var dto = new UpdateCashDenominationDto { Quantity = quantity };
            var result = await _fiscalDrawerService.UpdateDenominationAsync(denominationId, dto);
            if (result is not null)
            {
                var idx = CashDenominations.FindIndex(d => d.Id == denominationId);
                if (idx >= 0) CashDenominations[idx] = result;
                NotifyStateChanged();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating denomination quantity");
            return false;
        }
    }
}
