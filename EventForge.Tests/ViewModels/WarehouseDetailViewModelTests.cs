using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for WarehouseDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class WarehouseDetailViewModelTests : IDisposable
{
    private readonly Mock<IWarehouseService> _mockWarehouseService;
    private readonly Mock<IStorageLocationService> _mockStorageLocationService;
    private readonly Mock<ILogger<WarehouseDetailViewModel>> _mockLogger;
    private readonly WarehouseDetailViewModel _viewModel;

    public WarehouseDetailViewModelTests()
    {
        _mockWarehouseService = new Mock<IWarehouseService>();
        _mockStorageLocationService = new Mock<IStorageLocationService>();
        _mockLogger = new Mock<ILogger<WarehouseDetailViewModel>>();
        _viewModel = new WarehouseDetailViewModel(
            _mockWarehouseService.Object,
            _mockStorageLocationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var expectedWarehouse = new StorageFacilityDto
        {
            Id = warehouseId,
            Name = "Main Warehouse",
            Code = "WH-001",
            Address = "123 Storage St",
            IsFiscal = true,
            IsActive = true,
            TotalLocations = 10,
            ActiveLocations = 8
        };

        var locations = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>
            {
                new StorageLocationDto { Id = Guid.NewGuid(), Code = "A-01", WarehouseId = warehouseId },
                new StorageLocationDto { Id = Guid.NewGuid(), Code = "A-02", WarehouseId = warehouseId }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };

        _mockWarehouseService.Setup(s => s.GetStorageFacilityAsync(warehouseId))
            .ReturnsAsync(expectedWarehouse);
        _mockStorageLocationService.Setup(s => s.GetStorageLocationsByWarehouseAsync(warehouseId, 1, 100))
            .ReturnsAsync(locations);

        // Act
        await _viewModel.LoadEntityAsync(warehouseId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(warehouseId, _viewModel.Entity.Id);
        Assert.Equal("Main Warehouse", _viewModel.Entity.Name);
        Assert.Equal("WH-001", _viewModel.Entity.Code);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.StorageLocations);
        Assert.Equal(2, _viewModel.StorageLocations.Count());
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultWarehouse()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Name);
        Assert.Equal(string.Empty, _viewModel.Entity.Code);
        Assert.True(_viewModel.Entity.IsActive);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);
        
        _viewModel.Entity!.Name = "New Warehouse";
        _viewModel.Entity.Code = "WH-NEW";
        
        var createdWarehouse = new StorageFacilityDto
        {
            Id = Guid.NewGuid(),
            Name = "New Warehouse",
            Code = "WH-NEW",
            IsActive = true,
            TotalLocations = 0,
            ActiveLocations = 0,
            CreatedAt = DateTime.UtcNow
        };

        _mockWarehouseService.Setup(s => s.CreateStorageFacilityAsync(
            It.IsAny<CreateStorageFacilityDto>()))
            .ReturnsAsync(createdWarehouse);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdWarehouse.Id, _viewModel.Entity.Id);
        Assert.Equal("New Warehouse", _viewModel.Entity.Name);
        Assert.False(_viewModel.IsNewEntity);
        _mockWarehouseService.Verify(s => s.CreateStorageFacilityAsync(
            It.IsAny<CreateStorageFacilityDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var existingWarehouse = new StorageFacilityDto
        {
            Id = warehouseId,
            Name = "Original Warehouse",
            Code = "WH-001",
            Address = "Original Address",
            IsActive = true,
            TotalLocations = 5,
            ActiveLocations = 4
        };

        var locations = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockWarehouseService.Setup(s => s.GetStorageFacilityAsync(warehouseId))
            .ReturnsAsync(existingWarehouse);
        _mockStorageLocationService.Setup(s => s.GetStorageLocationsByWarehouseAsync(warehouseId, 1, 100))
            .ReturnsAsync(locations);

        await _viewModel.LoadEntityAsync(warehouseId);

        // Modify entity
        _viewModel.Entity!.Name = "Updated Warehouse";
        _viewModel.Entity.Address = "Updated Address";

        var updatedWarehouse = new StorageFacilityDto
        {
            Id = warehouseId,
            Name = "Updated Warehouse",
            Code = "WH-001",
            Address = "Updated Address",
            IsActive = true,
            TotalLocations = 5,
            ActiveLocations = 4
        };

        _mockWarehouseService.Setup(s => s.UpdateStorageFacilityAsync(
            warehouseId,
            It.IsAny<UpdateStorageFacilityDto>()))
            .ReturnsAsync(updatedWarehouse);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Updated Warehouse", _viewModel.Entity.Name);
        Assert.Equal("Updated Address", _viewModel.Entity.Address);
        _mockWarehouseService.Verify(s => s.UpdateStorageFacilityAsync(
            warehouseId,
            It.IsAny<UpdateStorageFacilityDto>()), Times.Once);
    }

    [Fact]
    public async Task AddStorageLocationAsync_WithValidLocation_AddsToCollection()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var existingWarehouse = new StorageFacilityDto
        {
            Id = warehouseId,
            Name = "Test Warehouse",
            Code = "WH-TEST",
            IsActive = true,
            TotalLocations = 0,
            ActiveLocations = 0
        };

        var locations = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockWarehouseService.Setup(s => s.GetStorageFacilityAsync(warehouseId))
            .ReturnsAsync(existingWarehouse);
        _mockStorageLocationService.Setup(s => s.GetStorageLocationsByWarehouseAsync(warehouseId, 1, 100))
            .ReturnsAsync(locations);

        await _viewModel.LoadEntityAsync(warehouseId);

        var locationDto = new CreateStorageLocationDto
        {
            Code = "A-01",
            Description = "First location",
            WarehouseId = warehouseId,
            Capacity = 100
        };

        var newLocation = new StorageLocationDto
        {
            Id = Guid.NewGuid(),
            Code = "A-01",
            Description = "First location",
            WarehouseId = warehouseId,
            Capacity = 100,
            IsActive = true
        };

        _mockStorageLocationService.Setup(s => s.CreateStorageLocationAsync(locationDto))
            .ReturnsAsync(newLocation);

        // Act
        var result = await _viewModel.AddStorageLocationAsync(locationDto);

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.StorageLocations);
        Assert.Single(_viewModel.StorageLocations);
        Assert.Equal("A-01", _viewModel.StorageLocations.First().Code);
        _mockStorageLocationService.Verify(s => s.CreateStorageLocationAsync(locationDto), Times.Once);
    }

    [Fact]
    public async Task DeleteStorageLocationAsync_WithValidId_RemovesFromCollection()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        
        var existingWarehouse = new StorageFacilityDto
        {
            Id = warehouseId,
            Name = "Test Warehouse",
            Code = "WH-TEST",
            IsActive = true,
            TotalLocations = 1,
            ActiveLocations = 1
        };

        var locations = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>
            {
                new StorageLocationDto
                {
                    Id = locationId,
                    Code = "A-01",
                    WarehouseId = warehouseId,
                    IsActive = true
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };

        _mockWarehouseService.Setup(s => s.GetStorageFacilityAsync(warehouseId))
            .ReturnsAsync(existingWarehouse);
        _mockStorageLocationService.Setup(s => s.GetStorageLocationsByWarehouseAsync(warehouseId, 1, 100))
            .ReturnsAsync(locations);

        await _viewModel.LoadEntityAsync(warehouseId);

        Assert.Single(_viewModel.StorageLocations!);

        _mockStorageLocationService.Setup(s => s.DeleteStorageLocationAsync(locationId))
            .ReturnsAsync(true);

        // Act
        var result = await _viewModel.DeleteStorageLocationAsync(locationId);

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.StorageLocations);
        Assert.Empty(_viewModel.StorageLocations);
        _mockStorageLocationService.Verify(s => s.DeleteStorageLocationAsync(locationId), Times.Once);
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

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
