using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for LotDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class LotDetailViewModelTests : IDisposable
{
    private readonly Mock<ILotService> _mockLotService;
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ILogger<LotDetailViewModel>> _mockLogger;
    private readonly LotDetailViewModel _viewModel;

    public LotDetailViewModelTests()
    {
        _mockLotService = new Mock<ILotService>();
        _mockProductService = new Mock<IProductService>();
        _mockLogger = new Mock<ILogger<LotDetailViewModel>>();
        _viewModel = new LotDetailViewModel(
            _mockLotService.Object,
            _mockProductService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var expectedLot = new LotDto
        {
            Id = lotId,
            TenantId = Guid.NewGuid(),
            Code = "LOT-001",
            ProductId = productId,
            ProductName = "Product A",
            ProductCode = "PROD-A",
            ProductionDate = DateTime.UtcNow.AddDays(-30),
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            SupplierId = Guid.NewGuid(),
            SupplierName = "Supplier A",
            OriginalQuantity = 100,
            AvailableQuantity = 75,
            Status = "Active",
            QualityStatus = "Approved",
            Notes = "Test lot",
            Barcode = "123456789",
            CountryOfOrigin = "Italy",
            IsActive = true
        };

        var products = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>
            {
                new ProductDto { Id = productId, Name = "Product A", Code = "PROD-A" },
                new ProductDto { Id = Guid.NewGuid(), Name = "Product B", Code = "PROD-B" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };

        _mockLotService.Setup(s => s.GetLotByIdAsync(lotId))
            .ReturnsAsync(expectedLot);
        _mockProductService.Setup(s => s.GetProductsAsync(1, 100))
            .ReturnsAsync(products);

        // Act
        await _viewModel.LoadEntityAsync(lotId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(lotId, _viewModel.Entity.Id);
        Assert.Equal("LOT-001", _viewModel.Entity.Code);
        Assert.Equal(productId, _viewModel.Entity.ProductId);
        Assert.Equal("Product A", _viewModel.Entity.ProductName);
        Assert.Equal(100, _viewModel.Entity.OriginalQuantity);
        Assert.Equal(75, _viewModel.Entity.AvailableQuantity);
        Assert.Equal("Active", _viewModel.Entity.Status);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.Products);
        Assert.Equal(2, _viewModel.Products.Count());
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultLot()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        await _viewModel.LoadEntityAsync(emptyId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.Code);
        Assert.Equal(Guid.Empty, _viewModel.Entity.ProductId);
        Assert.Equal("Active", _viewModel.Entity.Status);
        Assert.Equal("Approved", _viewModel.Entity.QualityStatus);
        Assert.True(_viewModel.Entity.IsActive);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);
        
        var productId = Guid.NewGuid();
        _viewModel.Entity!.Code = "LOT-NEW";
        _viewModel.Entity.ProductId = productId;
        _viewModel.Entity.OriginalQuantity = 100;
        _viewModel.Entity.ProductionDate = DateTime.UtcNow;
        _viewModel.Entity.ExpiryDate = DateTime.UtcNow.AddYears(1);
        
        var createdLot = new LotDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Code = "LOT-NEW",
            ProductId = productId,
            ProductName = "Product A",
            ProductCode = "PROD-A",
            OriginalQuantity = 100,
            AvailableQuantity = 100,
            ProductionDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = "Active",
            QualityStatus = "Approved",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockLotService.Setup(s => s.CreateLotAsync(
            It.IsAny<CreateLotDto>()))
            .ReturnsAsync(createdLot);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdLot.Id, _viewModel.Entity.Id);
        Assert.Equal("LOT-NEW", _viewModel.Entity.Code);
        Assert.Equal(productId, _viewModel.Entity.ProductId);
        Assert.False(_viewModel.IsNewEntity);
        _mockLotService.Verify(s => s.CreateLotAsync(
            It.IsAny<CreateLotDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var existingLot = new LotDto
        {
            Id = lotId,
            TenantId = Guid.NewGuid(),
            Code = "LOT-001",
            ProductId = productId,
            ProductName = "Product A",
            OriginalQuantity = 100,
            AvailableQuantity = 75,
            ProductionDate = DateTime.UtcNow.AddDays(-30),
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = "Active",
            QualityStatus = "Approved",
            IsActive = true
        };

        var products = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockLotService.Setup(s => s.GetLotByIdAsync(lotId))
            .ReturnsAsync(existingLot);
        _mockProductService.Setup(s => s.GetProductsAsync(1, 100))
            .ReturnsAsync(products);

        await _viewModel.LoadEntityAsync(lotId);

        // Modify entity
        _viewModel.Entity!.AvailableQuantity = 50;
        _viewModel.Entity.Notes = "Updated notes";

        var updatedLot = new LotDto
        {
            Id = lotId,
            TenantId = existingLot.TenantId,
            Code = "LOT-001",
            ProductId = productId,
            ProductName = "Product A",
            OriginalQuantity = 100,
            AvailableQuantity = 50,
            ProductionDate = existingLot.ProductionDate,
            ExpiryDate = existingLot.ExpiryDate,
            Status = "Active",
            QualityStatus = "Approved",
            Notes = "Updated notes",
            IsActive = true
        };

        _mockLotService.Setup(s => s.UpdateLotAsync(
            lotId,
            It.IsAny<UpdateLotDto>()))
            .ReturnsAsync(updatedLot);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal(50, _viewModel.Entity.AvailableQuantity);
        Assert.Equal("Updated notes", _viewModel.Entity.Notes);
        _mockLotService.Verify(s => s.UpdateLotAsync(
            lotId,
            It.IsAny<UpdateLotDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_LoadsProducts()
    {
        // Arrange
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var existingLot = new LotDto
        {
            Id = lotId,
            TenantId = Guid.NewGuid(),
            Code = "LOT-001",
            ProductId = productId,
            ProductName = "Product A",
            OriginalQuantity = 100,
            AvailableQuantity = 75,
            Status = "Active",
            IsActive = true
        };

        var products = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>
            {
                new ProductDto { Id = productId, Name = "Product A", Code = "PROD-A" },
                new ProductDto { Id = Guid.NewGuid(), Name = "Product B", Code = "PROD-B" },
                new ProductDto { Id = Guid.NewGuid(), Name = "Product C", Code = "PROD-C" }
            },
            TotalCount = 3,
            Page = 1,
            PageSize = 100
        };

        _mockLotService.Setup(s => s.GetLotByIdAsync(lotId))
            .ReturnsAsync(existingLot);
        _mockProductService.Setup(s => s.GetProductsAsync(1, 100))
            .ReturnsAsync(products);

        // Act
        await _viewModel.LoadEntityAsync(lotId);

        // Assert
        Assert.NotNull(_viewModel.Products);
        Assert.Equal(3, _viewModel.Products.Count());
        Assert.Contains(_viewModel.Products, p => p.Code == "PROD-A");
        Assert.Contains(_viewModel.Products, p => p.Code == "PROD-B");
        Assert.Contains(_viewModel.Products, p => p.Code == "PROD-C");
        _mockProductService.Verify(s => s.GetProductsAsync(1, 100), Times.Once);
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
        var lotId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var expectedLot = new LotDto
        {
            Id = lotId,
            TenantId = Guid.NewGuid(),
            Code = "LOT-001",
            ProductId = productId,
            ProductName = "Product A",
            OriginalQuantity = 100,
            AvailableQuantity = 75,
            Status = "Active",
            IsActive = true
        };

        var products = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        };

        _mockLotService.Setup(s => s.GetLotByIdAsync(lotId))
            .ReturnsAsync(expectedLot);
        _mockProductService.Setup(s => s.GetProductsAsync(1, 100))
            .ReturnsAsync(products);

        // Act
        await _viewModel.LoadEntityAsync(lotId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(lotId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
