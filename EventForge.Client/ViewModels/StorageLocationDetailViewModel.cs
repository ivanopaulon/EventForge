using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for StorageLocation detail page
/// </summary>
public class StorageLocationDetailViewModel : BaseEntityDetailViewModel<StorageLocationDto, CreateStorageLocationDto, UpdateStorageLocationDto>
{
    private readonly IStorageLocationService _storageLocationService;
    private readonly IWarehouseService _warehouseService;

    public StorageLocationDetailViewModel(
        IStorageLocationService storageLocationService,
        IWarehouseService warehouseService,
        ILogger<StorageLocationDetailViewModel> logger) 
        : base(logger)
    {
        _storageLocationService = storageLocationService;
        _warehouseService = warehouseService;
    }

    // Related entity collections
    public IEnumerable<StorageFacilityDto>? Warehouses { get; private set; }

    protected override StorageLocationDto CreateNewEntity()
    {
        return new StorageLocationDto
        {
            Id = Guid.Empty,
            Code = string.Empty,
            Description = null,
            WarehouseId = Guid.Empty,
            WarehouseName = null,
            Capacity = null,
            Occupancy = null,
            LastInventoryDate = null,
            IsRefrigerated = false,
            Notes = null,
            Zone = null,
            Floor = null,
            Row = null,
            Column = null,
            Level = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null
        };
    }

    protected override async Task<StorageLocationDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _storageLocationService.GetStorageLocationAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        if (IsNewEntity)
        {
            Warehouses = new List<StorageFacilityDto>();
            return;
        }

        try
        {
            // Load warehouses for dropdown selection
            var warehousesResult = await _warehouseService.GetStorageFacilitiesAsync(1, 100);
            Warehouses = warehousesResult?.Items ?? new List<StorageFacilityDto>();
            
            Logger.LogInformation("Loaded {Count} warehouses for location {Id}", 
                Warehouses.Count(), entityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for location {Id}", entityId);
            Warehouses = new List<StorageFacilityDto>();
        }
    }

    protected override CreateStorageLocationDto MapToCreateDto(StorageLocationDto entity)
    {
        return new CreateStorageLocationDto
        {
            Code = entity.Code,
            Description = entity.Description,
            WarehouseId = entity.WarehouseId,
            Capacity = entity.Capacity,
            Occupancy = entity.Occupancy,
            IsRefrigerated = entity.IsRefrigerated,
            Notes = entity.Notes,
            Zone = entity.Zone,
            Floor = entity.Floor,
            Row = entity.Row,
            Column = entity.Column,
            Level = entity.Level
        };
    }

    protected override UpdateStorageLocationDto MapToUpdateDto(StorageLocationDto entity)
    {
        return new UpdateStorageLocationDto
        {
            Code = entity.Code,
            Description = entity.Description,
            WarehouseId = entity.WarehouseId,
            Capacity = entity.Capacity,
            Occupancy = entity.Occupancy,
            IsRefrigerated = entity.IsRefrigerated,
            Notes = entity.Notes,
            Zone = entity.Zone,
            Floor = entity.Floor,
            Row = entity.Row,
            Column = entity.Column,
            Level = entity.Level,
            IsActive = entity.IsActive
        };
    }

    protected override Task<StorageLocationDto?> CreateEntityAsync(CreateStorageLocationDto createDto)
    {
        return _storageLocationService.CreateStorageLocationAsync(createDto);
    }

    protected override Task<StorageLocationDto?> UpdateEntityAsync(Guid entityId, UpdateStorageLocationDto updateDto)
    {
        return _storageLocationService.UpdateStorageLocationAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(StorageLocationDto entity)
    {
        return entity.Id;
    }
}
