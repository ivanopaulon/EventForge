using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for Inventory detail page
/// </summary>
public class InventoryDetailViewModel : BaseEntityDetailViewModel<InventoryDocumentDto, CreateInventoryDocumentDto, UpdateInventoryDocumentDto>
{
    private readonly IInventoryService _inventoryService;
    private readonly IWarehouseService _warehouseService;

    public InventoryDetailViewModel(
        IInventoryService inventoryService,
        IWarehouseService warehouseService,
        ILogger<InventoryDetailViewModel> logger) 
        : base(logger)
    {
        _inventoryService = inventoryService;
        _warehouseService = warehouseService;
    }

    // Related entity collections
    public IEnumerable<InventoryDocumentRowDto>? InventoryRows { get; private set; }
    public IEnumerable<StorageFacilityDto>? Warehouses { get; private set; }

    protected override InventoryDocumentDto CreateNewEntity()
    {
        return new InventoryDocumentDto
        {
            Id = Guid.Empty,
            Number = string.Empty,
            Series = null,
            InventoryDate = DateTime.UtcNow,
            WarehouseId = null,
            WarehouseName = null,
            Status = "Draft",
            Notes = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            FinalizedAt = null,
            FinalizedBy = null,
            Rows = new List<InventoryDocumentRowDto>()
        };
    }

    protected override async Task<InventoryDocumentDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _inventoryService.GetInventoryDocumentAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        if (IsNewEntity)
        {
            InventoryRows = new List<InventoryDocumentRowDto>();
            Warehouses = new List<StorageFacilityDto>();
            return;
        }

        try
        {
            var warehousesTask = _warehouseService.GetStorageFacilitiesAsync(1, 100);
            
            await Task.WhenAll(warehousesTask);
            
            // Load warehouses for selection
            Warehouses = warehousesTask.Result?.Items ?? new List<StorageFacilityDto>();
            
            // Rows are already loaded with the entity
            InventoryRows = Entity?.Rows ?? new List<InventoryDocumentRowDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for inventory {InventoryId}", entityId);
            InventoryRows = new List<InventoryDocumentRowDto>();
            Warehouses = new List<StorageFacilityDto>();
        }
    }

    protected override CreateInventoryDocumentDto MapToCreateDto(InventoryDocumentDto entity)
    {
        return new CreateInventoryDocumentDto
        {
            WarehouseId = entity.WarehouseId,
            InventoryDate = entity.InventoryDate,
            Notes = entity.Notes,
            Series = entity.Series,
            Number = entity.Number
        };
    }

    protected override UpdateInventoryDocumentDto MapToUpdateDto(InventoryDocumentDto entity)
    {
        return new UpdateInventoryDocumentDto
        {
            InventoryDate = entity.InventoryDate,
            WarehouseId = entity.WarehouseId,
            Notes = entity.Notes
        };
    }

    protected override Task<InventoryDocumentDto?> CreateEntityAsync(CreateInventoryDocumentDto createDto)
    {
        return _inventoryService.StartInventoryDocumentAsync(createDto);
    }

    protected override Task<InventoryDocumentDto?> UpdateEntityAsync(Guid entityId, UpdateInventoryDocumentDto updateDto)
    {
        return _inventoryService.UpdateInventoryDocumentAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(InventoryDocumentDto entity)
    {
        return entity.Id;
    }

    /// <summary>
    /// Adds a new inventory row to the document
    /// </summary>
    public async Task<bool> AddInventoryRowAsync(AddInventoryDocumentRowDto rowDto)
    {
        if (Entity == null || IsNewEntity)
        {
            Logger.LogWarning("Cannot add row to unsaved inventory document");
            return false;
        }

        try
        {
            var entityId = GetEntityId(Entity);
            var updated = await _inventoryService.AddInventoryDocumentRowAsync(entityId, rowDto);
            
            if (updated != null)
            {
                Entity = updated;
                InventoryRows = updated.Rows;
                NotifyStateChanged();
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding inventory row");
            throw;
        }
    }

    /// <summary>
    /// Deletes an inventory row from the document
    /// </summary>
    public async Task<bool> DeleteInventoryRowAsync(Guid rowId)
    {
        if (Entity == null || IsNewEntity)
        {
            Logger.LogWarning("Cannot delete row from unsaved inventory document");
            return false;
        }

        try
        {
            var entityId = GetEntityId(Entity);
            var updated = await _inventoryService.DeleteInventoryDocumentRowAsync(entityId, rowId);
            
            if (updated != null)
            {
                Entity = updated;
                InventoryRows = updated.Rows;
                NotifyStateChanged();
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting inventory row");
            throw;
        }
    }
}
