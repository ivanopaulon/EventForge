using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for DocumentTypeDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentTypeDetailViewModelTests : IDisposable
{
    private readonly Mock<IDocumentTypeService> _mockDocumentTypeService;
    private readonly Mock<IWarehouseService> _mockWarehouseService;
    private readonly Mock<ILogger<DocumentTypeDetailViewModel>> _mockLogger;
    private readonly DocumentTypeDetailViewModel _viewModel;

    public DocumentTypeDetailViewModelTests()
    {
        _mockDocumentTypeService = new Mock<IDocumentTypeService>();
        _mockWarehouseService = new Mock<IWarehouseService>();
        _mockLogger = new Mock<ILogger<DocumentTypeDetailViewModel>>();
        _viewModel = new DocumentTypeDetailViewModel(
            _mockDocumentTypeService.Object,
            _mockWarehouseService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var documentTypeId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var expectedDocumentType = new DocumentTypeDto
        {
            Id = documentTypeId,
            Code = "INV",
            Name = "Invoice",
            IsStockIncrease = false,
            DefaultWarehouseId = warehouseId,
            DefaultWarehouseName = "Main Warehouse",
            IsFiscal = true,
            RequiredPartyType = BusinessPartyType.Customer,
            Notes = "Standard invoice document type"
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

        _mockDocumentTypeService.Setup(s => s.GetDocumentTypeByIdAsync(documentTypeId))
            .ReturnsAsync(expectedDocumentType);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        // Act
        await _viewModel.LoadEntityAsync(documentTypeId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(documentTypeId, _viewModel.Entity.Id);
        Assert.Equal("INV", _viewModel.Entity.Code);
        Assert.Equal("Invoice", _viewModel.Entity.Name);
        Assert.Equal(warehouseId, _viewModel.Entity.DefaultWarehouseId);
        Assert.True(_viewModel.Entity.IsFiscal);
        Assert.Equal(BusinessPartyType.Customer, _viewModel.Entity.RequiredPartyType);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.Warehouses);
        Assert.Equal(2, _viewModel.Warehouses.Count());
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultDocumentType()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Code);
        Assert.Equal(string.Empty, _viewModel.Entity.Name);
        Assert.False(_viewModel.Entity.IsStockIncrease);
        Assert.True(_viewModel.Entity.IsFiscal);
        Assert.Equal(BusinessPartyType.Both, _viewModel.Entity.RequiredPartyType);
        Assert.Null(_viewModel.Entity.DefaultWarehouseId);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);
        
        var warehouseId = Guid.NewGuid();
        _viewModel.Entity!.Code = "DDT";
        _viewModel.Entity.Name = "Delivery Note";
        _viewModel.Entity.IsStockIncrease = true;
        _viewModel.Entity.DefaultWarehouseId = warehouseId;
        _viewModel.Entity.IsFiscal = false;
        _viewModel.Entity.RequiredPartyType = BusinessPartyType.Supplier;
        _viewModel.Entity.Notes = "For deliveries";
        
        var createdDocumentType = new DocumentTypeDto
        {
            Id = Guid.NewGuid(),
            Code = "DDT",
            Name = "Delivery Note",
            IsStockIncrease = true,
            DefaultWarehouseId = warehouseId,
            DefaultWarehouseName = "Main Warehouse",
            IsFiscal = false,
            RequiredPartyType = BusinessPartyType.Supplier,
            Notes = "For deliveries",
            CreatedAt = DateTime.UtcNow
        };

        _mockDocumentTypeService.Setup(s => s.CreateDocumentTypeAsync(
            It.IsAny<CreateDocumentTypeDto>()))
            .ReturnsAsync(createdDocumentType);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdDocumentType.Id, _viewModel.Entity.Id);
        Assert.Equal("DDT", _viewModel.Entity.Code);
        Assert.Equal("Delivery Note", _viewModel.Entity.Name);
        Assert.True(_viewModel.Entity.IsStockIncrease);
        Assert.False(_viewModel.Entity.IsFiscal);
        Assert.Equal(BusinessPartyType.Supplier, _viewModel.Entity.RequiredPartyType);
        Assert.False(_viewModel.IsNewEntity);
        _mockDocumentTypeService.Verify(s => s.CreateDocumentTypeAsync(
            It.IsAny<CreateDocumentTypeDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var documentTypeId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var existingDocumentType = new DocumentTypeDto
        {
            Id = documentTypeId,
            Code = "INV",
            Name = "Invoice",
            IsStockIncrease = false,
            DefaultWarehouseId = warehouseId,
            IsFiscal = true,
            RequiredPartyType = BusinessPartyType.Customer,
            Notes = "Original notes"
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentTypeService.Setup(s => s.GetDocumentTypeByIdAsync(documentTypeId))
            .ReturnsAsync(existingDocumentType);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        await _viewModel.LoadEntityAsync(documentTypeId);

        // Modify entity
        _viewModel.Entity!.Name = "Updated Invoice";
        _viewModel.Entity.Notes = "Updated notes";
        _viewModel.Entity.IsFiscal = false;

        var updatedDocumentType = new DocumentTypeDto
        {
            Id = documentTypeId,
            Code = "INV",
            Name = "Updated Invoice",
            IsStockIncrease = false,
            DefaultWarehouseId = warehouseId,
            IsFiscal = false,
            RequiredPartyType = BusinessPartyType.Customer,
            Notes = "Updated notes"
        };

        _mockDocumentTypeService.Setup(s => s.UpdateDocumentTypeAsync(
            documentTypeId,
            It.IsAny<UpdateDocumentTypeDto>()))
            .ReturnsAsync(updatedDocumentType);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Updated Invoice", _viewModel.Entity.Name);
        Assert.Equal("Updated notes", _viewModel.Entity.Notes);
        Assert.False(_viewModel.Entity.IsFiscal);
        _mockDocumentTypeService.Verify(s => s.UpdateDocumentTypeAsync(
            documentTypeId,
            It.IsAny<UpdateDocumentTypeDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_LoadsWarehouses()
    {
        // Arrange
        var documentTypeId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var existingDocumentType = new DocumentTypeDto
        {
            Id = documentTypeId,
            Code = "INV",
            Name = "Invoice",
            IsStockIncrease = false,
            DefaultWarehouseId = warehouseId,
            IsFiscal = true,
            RequiredPartyType = BusinessPartyType.Customer
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

        _mockDocumentTypeService.Setup(s => s.GetDocumentTypeByIdAsync(documentTypeId))
            .ReturnsAsync(existingDocumentType);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        // Act
        await _viewModel.LoadEntityAsync(documentTypeId);

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
        var documentTypeId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var expectedDocumentType = new DocumentTypeDto
        {
            Id = documentTypeId,
            Code = "INV",
            Name = "Invoice",
            IsStockIncrease = false,
            DefaultWarehouseId = warehouseId,
            IsFiscal = true,
            RequiredPartyType = BusinessPartyType.Customer
        };

        var warehouses = new PagedResult<StorageFacilityDto>
        {
            Items = new List<StorageFacilityDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockDocumentTypeService.Setup(s => s.GetDocumentTypeByIdAsync(documentTypeId))
            .ReturnsAsync(expectedDocumentType);
        _mockWarehouseService.Setup(s => s.GetStorageFacilitiesAsync(1, 100))
            .ReturnsAsync(warehouses);

        // Act
        await _viewModel.LoadEntityAsync(documentTypeId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(documentTypeId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
