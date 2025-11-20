using EventForge.Client.Services;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for Warehouse (StorageFacility) detail page
/// </summary>
public class WarehouseDetailViewModel : BaseEntityDetailViewModel<StorageFacilityDto, CreateStorageFacilityDto, UpdateStorageFacilityDto>
{
    private readonly IWarehouseService _warehouseService;
    private readonly IStorageLocationService _storageLocationService;

    public WarehouseDetailViewModel(
        IWarehouseService warehouseService,
        IStorageLocationService storageLocationService,
        ILogger<WarehouseDetailViewModel> logger)
        : base(logger)
    {
        _warehouseService = warehouseService;
        _storageLocationService = storageLocationService;
    }

    // Related entity collections
    public IEnumerable<StorageLocationDto>? StorageLocations { get; private set; }

    protected override StorageFacilityDto CreateNewEntity()
    {
        return new StorageFacilityDto
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Code = string.Empty,
            Address = null,
            Phone = null,
            Email = null,
            Manager = null,
            IsFiscal = false,
            Notes = null,
            AreaSquareMeters = null,
            Capacity = null,
            IsRefrigerated = false,
            TotalLocations = 0,
            ActiveLocations = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null
        };
    }

    protected override async Task<StorageFacilityDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _warehouseService.GetStorageFacilityAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        if (IsNewEntity)
        {
            StorageLocations = new List<StorageLocationDto>();
            return;
        }

        try
        {
            var locationsResult = await _storageLocationService.GetStorageLocationsByWarehouseAsync(entityId, 1, 100);
            StorageLocations = locationsResult?.Items ?? new List<StorageLocationDto>();
            Logger.LogInformation("Loaded {Count} storage locations for warehouse {WarehouseId}",
                StorageLocations.Count(), entityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading storage locations for warehouse {WarehouseId}", entityId);
            StorageLocations = new List<StorageLocationDto>();
        }
    }

    protected override CreateStorageFacilityDto MapToCreateDto(StorageFacilityDto entity)
    {
        return new CreateStorageFacilityDto
        {
            Name = entity.Name,
            Code = entity.Code,
            Address = entity.Address,
            Phone = entity.Phone,
            Email = entity.Email,
            Manager = entity.Manager,
            IsFiscal = entity.IsFiscal,
            Notes = entity.Notes,
            AreaSquareMeters = entity.AreaSquareMeters,
            Capacity = entity.Capacity,
            IsRefrigerated = entity.IsRefrigerated
        };
    }

    protected override UpdateStorageFacilityDto MapToUpdateDto(StorageFacilityDto entity)
    {
        return new UpdateStorageFacilityDto
        {
            Name = entity.Name,
            Address = entity.Address,
            Phone = entity.Phone,
            Email = entity.Email,
            Manager = entity.Manager,
            IsFiscal = entity.IsFiscal,
            Notes = entity.Notes,
            AreaSquareMeters = entity.AreaSquareMeters,
            Capacity = entity.Capacity,
            IsRefrigerated = entity.IsRefrigerated
        };
    }

    protected override Task<StorageFacilityDto?> CreateEntityAsync(CreateStorageFacilityDto createDto)
    {
        return _warehouseService.CreateStorageFacilityAsync(createDto);
    }

    protected override Task<StorageFacilityDto?> UpdateEntityAsync(Guid entityId, UpdateStorageFacilityDto updateDto)
    {
        return _warehouseService.UpdateStorageFacilityAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(StorageFacilityDto entity)
    {
        return entity.Id;
    }

    /// <summary>
    /// Adds a new storage location to the warehouse
    /// </summary>
    public async Task<bool> AddStorageLocationAsync(CreateStorageLocationDto locationDto)
    {
        if (Entity == null || IsNewEntity)
        {
            Logger.LogWarning("Cannot add storage location to unsaved warehouse");
            return false;
        }

        try
        {
            var newLocation = await _storageLocationService.CreateStorageLocationAsync(locationDto);

            if (newLocation != null)
            {
                StorageLocations = StorageLocations?.Append(newLocation) ?? new List<StorageLocationDto> { newLocation };
                NotifyStateChanged();
                Logger.LogInformation("Added storage location {LocationId} to warehouse {WarehouseId}",
                    newLocation.Id, Entity.Id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding storage location to warehouse {WarehouseId}", Entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a storage location from the warehouse
    /// </summary>
    public async Task<bool> DeleteStorageLocationAsync(Guid locationId)
    {
        if (Entity == null || IsNewEntity)
        {
            Logger.LogWarning("Cannot delete storage location from unsaved warehouse");
            return false;
        }

        try
        {
            var result = await _storageLocationService.DeleteStorageLocationAsync(locationId);

            if (result)
            {
                StorageLocations = StorageLocations?.Where(l => l.Id != locationId) ?? new List<StorageLocationDto>();
                NotifyStateChanged();
                Logger.LogInformation("Deleted storage location {LocationId} from warehouse {WarehouseId}",
                    locationId, Entity.Id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting storage location {LocationId} from warehouse {WarehouseId}",
                locationId, Entity.Id);
            throw;
        }
    }
}
