using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for StorageLocationDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class StorageLocationDetailViewModelTests : IDisposable
{
    private readonly Mock<IStorageLocationService> _mockStorageLocationService;
    private readonly Mock<IWarehouseService> _mockWarehouseService;
    private readonly Mock<ILogger<StorageLocationDetailViewModel>> _mockLogger;
    private readonly StorageLocationDetailViewModel _viewModel;

    public StorageLocationDetailViewModelTests()
    {
        _mockStorageLocationService = new Mock<IStorageLocationService>();
        _mockWarehouseService = new Mock<IWarehouseService>();
        _mockLogger = new Mock<ILogger<StorageLocationDetailViewModel>>();
        _viewModel = new StorageLocationDetailViewModel(
            _mockStorageLocationService.Object,
            _mockWarehouseService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var expectedLocation = new StorageLocationDto
        {
            Id = locationId,
            Code = "A-01",
            Description = "First location",
            WarehouseId = warehouseId,
            WarehouseName = "Main Warehouse",
            Capacity = 100,
            Occupancy = 50,
            IsRefrigerated = false,
            IsActive = true,
            Zone = "A",
            Floor = "1",
            Row = "01",
            Column = null,
            Level = null
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>
            {
                new StorageFacilityDto { Id = warehouseId, Name = "Main Warehouse", Code = "WH-001" },
                new StorageFacilityDto { Id = Guid.NewGuid(), Name = "Secondary Warehouse", Code = "WH-002" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };

        _mockStorageLocationService.Setup(s => s.GetStorageLocationAsync(locationId))
            .ReturnsAsync(expectedLocation);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        // Act
        await _viewModel.LoadEntityAsync(locationId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(locationId, _viewModel.Entity.Id);
        Assert.Equal("A-01", _viewModel.Entity.Code);
        Assert.Equal("First location", _viewModel.Entity.Description);
        Assert.Equal(warehouseId, _viewModel.Entity.WarehouseId);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.Warehouses);
        Assert.Equal(2, _viewModel.Warehouses.Count());
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultStorageLocation()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Code);
        Assert.Equal(Guid.Empty, _viewModel.Entity.WarehouseId);
        Assert.True(_viewModel.Entity.IsActive);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);
        
        var warehouseId = Guid.NewGuid();
        _viewModel.Entity!.Code = "A-01";
        _viewModel.Entity.Description = "New Location";
        _viewModel.Entity.WarehouseId = warehouseId;
        _viewModel.Entity.Capacity = 100;
        
        var createdLocation = new StorageLocationDto
        {
            Id = Guid.NewGuid(),
            Code = "A-01",
            Description = "New Location",
            WarehouseId = warehouseId,
            WarehouseName = "Main Warehouse",
            Capacity = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockStorageLocationService.Setup(s => s.CreateStorageLocationAsync(
            It.IsAny<CreateStorageLocationDto>()))
            .ReturnsAsync(createdLocation);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdLocation.Id, _viewModel.Entity.Id);
        Assert.Equal("A-01", _viewModel.Entity.Code);
        Assert.Equal("New Location", _viewModel.Entity.Description);
        Assert.False(_viewModel.IsNewEntity);
        _mockStorageLocationService.Verify(s => s.CreateStorageLocationAsync(
            It.IsAny<CreateStorageLocationDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var existingLocation = new StorageLocationDto
        {
            Id = locationId,
            Code = "A-01",
            Description = "Original Location",
            WarehouseId = warehouseId,
            Capacity = 100,
            IsActive = true
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockStorageLocationService.Setup(s => s.GetStorageLocationAsync(locationId))
            .ReturnsAsync(existingLocation);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        await _viewModel.LoadEntityAsync(locationId);

        // Modify entity
        _viewModel.Entity!.Description = "Updated Location";
        _viewModel.Entity.Capacity = 200;

        var updatedLocation = new StorageLocationDto
        {
            Id = locationId,
            Code = "A-01",
            Description = "Updated Location",
            WarehouseId = warehouseId,
            Capacity = 200,
            IsActive = true
        };

        _mockStorageLocationService.Setup(s => s.UpdateStorageLocationAsync(
            locationId,
            It.IsAny<UpdateStorageLocationDto>()))
            .ReturnsAsync(updatedLocation);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Updated Location", _viewModel.Entity.Description);
        Assert.Equal(200, _viewModel.Entity.Capacity);
        _mockStorageLocationService.Verify(s => s.UpdateStorageLocationAsync(
            locationId,
            It.IsAny<UpdateStorageLocationDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_LoadsWarehouses()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var existingLocation = new StorageLocationDto
        {
            Id = locationId,
            Code = "A-01",
            Description = "Test Location",
            WarehouseId = warehouseId,
            IsActive = true
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>
            {
                new StorageFacilityDto { Id = warehouseId, Name = "Warehouse 1", Code = "WH-001" },
                new StorageFacilityDto { Id = Guid.NewGuid(), Name = "Warehouse 2", Code = "WH-002" },
                new StorageFacilityDto { Id = Guid.NewGuid(), Name = "Warehouse 3", Code = "WH-003" }
            },
            TotalCount = 3,
            Page = 1,
            PageSize = 100
        };

        _mockStorageLocationService.Setup(s => s.GetStorageLocationAsync(locationId))
            .ReturnsAsync(existingLocation);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        // Act
        await _viewModel.LoadEntityAsync(locationId);

        // Assert
        Assert.NotNull(_viewModel.Warehouses);
        Assert.Equal(3, _viewModel.Warehouses.Count());
        Assert.Contains(_viewModel.Warehouses, w => w.Code == "WH-001");
        Assert.Contains(_viewModel.Warehouses, w => w.Code == "WH-002");
        Assert.Contains(_viewModel.Warehouses, w => w.Code == "WH-003");
        _mockWarehouseService.Verify(s => s.GetStorageFacilitiesAsync(1, 100), Times.Once);
    }

    [Fact]
    public async Task IsNewEntity_WithEmptyId_ReturnsTrue()
    {
        // Arrange & Act
        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Assert
        Assert.True(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
    }

    [Fact]
    public async Task GetEntityId_ReturnsCorrectId()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var expectedLocation = new StorageLocationDto
        {
            Id = locationId,
            Code = "A-01",
            Description = "Test Location",
            WarehouseId = warehouseId,
            IsActive = true
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockStorageLocationService.Setup(s => s.GetStorageLocationAsync(locationId))
            .ReturnsAsync(expectedLocation);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        // Act
        await _viewModel.LoadEntityAsync(locationId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(locationId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
