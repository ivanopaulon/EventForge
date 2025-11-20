using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for InventoryDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class InventoryDetailViewModelTests : IDisposable
{
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<IWarehouseService> _mockWarehouseService;
    private readonly Mock<ILogger<InventoryDetailViewModel>> _mockLogger;
    private readonly InventoryDetailViewModel _viewModel;

    public InventoryDetailViewModelTests()
    {
        _mockInventoryService = new Mock<IInventoryService>();
        _mockWarehouseService = new Mock<IWarehouseService>();
        _mockLogger = new Mock<ILogger<InventoryDetailViewModel>>();
        _viewModel = new InventoryDetailViewModel(
            _mockInventoryService.Object,
            _mockWarehouseService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var expectedInventory = new InventoryDocumentDto
        {
            Id = inventoryId,
            Number = "INV-001",
            Status = "Draft",
            InventoryDate = DateTime.UtcNow,
            Rows = new List<InventoryDocumentRowDto>()
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>
            {
                new StorageFacilityDto { Id = Guid.NewGuid(), Name = "Warehouse 1" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };

        _mockInventoryService.Setup(s => s.GetInventoryDocumentAsync(inventoryId))
            .ReturnsAsync(expectedInventory);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        // Act
        await _viewModel.LoadEntityAsync(inventoryId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(inventoryId, _viewModel.Entity.Id);
        Assert.Equal("INV-001", _viewModel.Entity.Number);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.InventoryRows);
        Assert.NotNull(_viewModel.Warehouses);
        Assert.Single(_viewModel.Warehouses);
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultInventory()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal("Draft", _viewModel.Entity.Status);
        Assert.True(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.Entity.Rows);
        Assert.Empty(_viewModel.Entity.Rows);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);
        
        var createdInventory = new InventoryDocumentDto
        {
            Id = Guid.NewGuid(),
            Number = "INV-NEW",
            Status = "Draft",
            InventoryDate = DateTime.UtcNow,
            Rows = new List<InventoryDocumentRowDto>()
        };

        _mockInventoryService.Setup(s => s.StartInventoryDocumentAsync(
            It.IsAny<CreateInventoryDocumentDto>()))
            .ReturnsAsync(createdInventory);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdInventory.Id, _viewModel.Entity.Id);
        Assert.False(_viewModel.IsNewEntity);
        _mockInventoryService.Verify(s => s.StartInventoryDocumentAsync(
            It.IsAny<CreateInventoryDocumentDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var existingInventory = new InventoryDocumentDto
        {
            Id = inventoryId,
            Number = "INV-001",
            Status = "Draft",
            InventoryDate = DateTime.UtcNow,
            Notes = "Original",
            Rows = new List<InventoryDocumentRowDto>()
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockInventoryService.Setup(s => s.GetInventoryDocumentAsync(inventoryId))
            .ReturnsAsync(existingInventory);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        await _viewModel.LoadEntityAsync(inventoryId);

        // Modify entity
        _viewModel.Entity!.Notes = "Updated";

        var updatedInventory = new InventoryDocumentDto
        {
            Id = inventoryId,
            Number = "INV-001",
            Status = "Draft",
            InventoryDate = DateTime.UtcNow,
            Notes = "Updated",
            Rows = new List<InventoryDocumentRowDto>()
        };

        _mockInventoryService.Setup(s => s.UpdateInventoryDocumentAsync(
            inventoryId,
            It.IsAny<UpdateInventoryDocumentDto>()))
            .ReturnsAsync(updatedInventory);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Updated", _viewModel.Entity.Notes);
        _mockInventoryService.Verify(s => s.UpdateInventoryDocumentAsync(
            inventoryId,
            It.IsAny<UpdateInventoryDocumentDto>()), Times.Once);
    }

    [Fact]
    public async Task AddInventoryRowAsync_WithValidRow_AddsToCollection()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var existingInventory = new InventoryDocumentDto
        {
            Id = inventoryId,
            Number = "INV-001",
            Status = "Draft",
            InventoryDate = DateTime.UtcNow,
            Rows = new List<InventoryDocumentRowDto>()
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockInventoryService.Setup(s => s.GetInventoryDocumentAsync(inventoryId))
            .ReturnsAsync(existingInventory);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        await _viewModel.LoadEntityAsync(inventoryId);

        var rowDto = new AddInventoryDocumentRowDto
        {
            ProductId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Quantity = 10
        };

        var updatedInventory = new InventoryDocumentDto
        {
            Id = inventoryId,
            Number = "INV-001",
            Status = "Draft",
            InventoryDate = DateTime.UtcNow,
            Rows = new List<InventoryDocumentRowDto>
            {
                new InventoryDocumentRowDto
                {
                    Id = Guid.NewGuid(),
                    ProductId = rowDto.ProductId,
                    LocationId = rowDto.LocationId,
                    Quantity = rowDto.Quantity,
                    ProductName = "Test Product",
                    ProductCode = "TEST-001",
                    LocationName = "Test Location"
                }
            }
        };

        _mockInventoryService.Setup(s => s.AddInventoryDocumentRowAsync(
            inventoryId,
            rowDto))
            .ReturnsAsync(updatedInventory);

        // Act
        var result = await _viewModel.AddInventoryRowAsync(rowDto);

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.InventoryRows);
        Assert.Single(_viewModel.InventoryRows);
        _mockInventoryService.Verify(s => s.AddInventoryDocumentRowAsync(
            inventoryId,
            rowDto), Times.Once);
    }

    [Fact]
    public async Task DeleteInventoryRowAsync_WithValidId_RemovesFromCollection()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        var existingInventory = new InventoryDocumentDto
        {
            Id = inventoryId,
            Number = "INV-001",
            Status = "Draft",
            InventoryDate = DateTime.UtcNow,
            Rows = new List<InventoryDocumentRowDto>
            {
                new InventoryDocumentRowDto
                {
                    Id = rowId,
                    ProductId = Guid.NewGuid(),
                    LocationId = Guid.NewGuid(),
                    Quantity = 10,
                    ProductName = "Test Product",
                    ProductCode = "TEST-001",
                    LocationName = "Test Location"
                }
            }
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockInventoryService.Setup(s => s.GetInventoryDocumentAsync(inventoryId))
            .ReturnsAsync(existingInventory);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        await _viewModel.LoadEntityAsync(inventoryId);

        var updatedInventory = new InventoryDocumentDto
        {
            Id = inventoryId,
            Number = "INV-001",
            Status = "Draft",
            InventoryDate = DateTime.UtcNow,
            Rows = new List<InventoryDocumentRowDto>()
        };

        _mockInventoryService.Setup(s => s.DeleteInventoryDocumentRowAsync(
            inventoryId,
            rowId))
            .ReturnsAsync(updatedInventory);

        // Act
        var result = await _viewModel.DeleteInventoryRowAsync(rowId);

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.InventoryRows);
        Assert.Empty(_viewModel.InventoryRows);
        _mockInventoryService.Verify(s => s.DeleteInventoryDocumentRowAsync(
            inventoryId,
            rowId), Times.Once);
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
