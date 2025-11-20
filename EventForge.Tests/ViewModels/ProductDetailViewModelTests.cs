using EventForge.Client.Services;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for ProductDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class ProductDetailViewModelTests : IDisposable
{
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ILogger<ProductDetailViewModel>> _mockLogger;
    private readonly ProductDetailViewModel _viewModel;

    public ProductDetailViewModelTests()
    {
        _mockProductService = new Mock<IProductService>();
        _mockLogger = new Mock<ILogger<ProductDetailViewModel>>();
        _viewModel = new ProductDetailViewModel(
            _mockProductService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedProduct = new ProductDto
        {
            Id = productId,
            Code = "PROD-001",
            Name = "Test Product",
            ShortDescription = "Short desc",
            Description = "Full description",
            Status = ProductStatus.Active,
            IsVatIncluded = false,
            IsBundle = false,
            DefaultPrice = 100.00m
        };

        _mockProductService.Setup(s => s.GetProductDetailAsync(productId))
            .ReturnsAsync(expectedProduct);

        _mockProductService.Setup(s => s.GetProductCodesAsync(productId))
            .ReturnsAsync(new List<ProductCodeDto>
            {
                new ProductCodeDto { Id = Guid.NewGuid(), ProductId = productId, Code = "EAN-123", CodeType = "EAN" }
            });

        _mockProductService.Setup(s => s.GetProductUnitsAsync(productId))
            .ReturnsAsync(new List<ProductUnitDto>
            {
                new ProductUnitDto { Id = Guid.NewGuid(), ProductId = productId, UnitType = "Box", ConversionFactor = 12 }
            });

        _mockProductService.Setup(s => s.GetProductSuppliersAsync(productId))
            .ReturnsAsync(new List<ProductSupplierDto>
            {
                new ProductSupplierDto { Id = Guid.NewGuid(), ProductId = productId, SupplierName = "Supplier A" }
            });

        // Act
        await _viewModel.LoadEntityAsync(productId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(productId, _viewModel.Entity.Id);
        Assert.Equal("PROD-001", _viewModel.Entity.Code);
        Assert.Equal("Test Product", _viewModel.Entity.Name);
        Assert.Equal(100.00m, _viewModel.Entity.DefaultPrice);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.ProductCodes);
        Assert.Single(_viewModel.ProductCodes);
        Assert.NotNull(_viewModel.ProductUnits);
        Assert.Single(_viewModel.ProductUnits);
        Assert.NotNull(_viewModel.ProductSuppliers);
        Assert.Single(_viewModel.ProductSuppliers);
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultProduct()
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
        Assert.Equal(ProductStatus.Active, _viewModel.Entity.Status);
        Assert.False(_viewModel.Entity.IsVatIncluded);
        Assert.False(_viewModel.Entity.IsBundle);
        Assert.Null(_viewModel.Entity.DefaultPrice);
        Assert.True(_viewModel.IsNewEntity);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        _viewModel.Entity!.Code = "PROD-NEW";
        _viewModel.Entity.Name = "New Product";
        _viewModel.Entity.ShortDescription = "Short description";
        _viewModel.Entity.Description = "Full description";
        _viewModel.Entity.DefaultPrice = 50.00m;

        var createdProduct = new ProductDto
        {
            Id = Guid.NewGuid(),
            Code = "PROD-NEW",
            Name = "New Product",
            ShortDescription = "Short description",
            Description = "Full description",
            Status = ProductStatus.Active,
            IsVatIncluded = false,
            IsBundle = false,
            DefaultPrice = 50.00m
        };

        _mockProductService.Setup(s => s.CreateProductAsync(
            It.IsAny<CreateProductDto>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdProduct.Id, _viewModel.Entity.Id);
        Assert.Equal("PROD-NEW", _viewModel.Entity.Code);
        Assert.Equal("New Product", _viewModel.Entity.Name);
        Assert.False(_viewModel.IsNewEntity);
        _mockProductService.Verify(s => s.CreateProductAsync(
            It.IsAny<CreateProductDto>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingEntity_CallsUpdate()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new ProductDto
        {
            Id = productId,
            Code = "PROD-001",
            Name = "Existing Product",
            ShortDescription = "Original description",
            Description = "Original full description",
            Status = ProductStatus.Active,
            IsVatIncluded = false,
            IsBundle = false,
            DefaultPrice = 100.00m
        };

        _mockProductService.Setup(s => s.GetProductDetailAsync(productId))
            .ReturnsAsync(existingProduct);

        _mockProductService.Setup(s => s.GetProductCodesAsync(productId))
            .ReturnsAsync(new List<ProductCodeDto>());

        _mockProductService.Setup(s => s.GetProductUnitsAsync(productId))
            .ReturnsAsync(new List<ProductUnitDto>());

        _mockProductService.Setup(s => s.GetProductSuppliersAsync(productId))
            .ReturnsAsync(new List<ProductSupplierDto>());

        await _viewModel.LoadEntityAsync(productId);

        // Modify entity
        _viewModel.Entity!.Name = "Updated Product";
        _viewModel.Entity.DefaultPrice = 150.00m;

        var updatedProduct = new ProductDto
        {
            Id = productId,
            Code = "PROD-001",
            Name = "Updated Product",
            ShortDescription = "Original description",
            Description = "Original full description",
            Status = ProductStatus.Active,
            IsVatIncluded = false,
            IsBundle = false,
            DefaultPrice = 150.00m
        };

        _mockProductService.Setup(s => s.UpdateProductAsync(
            productId,
            It.IsAny<UpdateProductDto>()))
            .ReturnsAsync(updatedProduct);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.Equal("Updated Product", _viewModel.Entity.Name);
        Assert.Equal(150.00m, _viewModel.Entity.DefaultPrice);
        _mockProductService.Verify(s => s.UpdateProductAsync(
            productId,
            It.IsAny<UpdateProductDto>()), Times.Once);
    }

    [Fact]
    public async Task LoadRelatedEntities_LoadsProductCodesUnitsSuppliers()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new ProductDto
        {
            Id = productId,
            Code = "PROD-001",
            Name = "Test Product",
            Status = ProductStatus.Active,
            IsVatIncluded = false,
            IsBundle = false
        };

        var productCodes = new List<ProductCodeDto>
        {
            new ProductCodeDto { Id = Guid.NewGuid(), ProductId = productId, Code = "EAN-123", CodeType = "EAN" },
            new ProductCodeDto { Id = Guid.NewGuid(), ProductId = productId, Code = "UPC-456", CodeType = "UPC" }
        };

        var productUnits = new List<ProductUnitDto>
        {
            new ProductUnitDto { Id = Guid.NewGuid(), ProductId = productId, UnitType = "Box", ConversionFactor = 12 },
            new ProductUnitDto { Id = Guid.NewGuid(), ProductId = productId, UnitType = "Pallet", ConversionFactor = 144 }
        };

        var productSuppliers = new List<ProductSupplierDto>
        {
            new ProductSupplierDto { Id = Guid.NewGuid(), ProductId = productId, SupplierName = "Supplier A" },
            new ProductSupplierDto { Id = Guid.NewGuid(), ProductId = productId, SupplierName = "Supplier B" }
        };

        _mockProductService.Setup(s => s.GetProductDetailAsync(productId))
            .ReturnsAsync(existingProduct);

        _mockProductService.Setup(s => s.GetProductCodesAsync(productId))
            .ReturnsAsync(productCodes);

        _mockProductService.Setup(s => s.GetProductUnitsAsync(productId))
            .ReturnsAsync(productUnits);

        _mockProductService.Setup(s => s.GetProductSuppliersAsync(productId))
            .ReturnsAsync(productSuppliers);

        // Act
        await _viewModel.LoadEntityAsync(productId);

        // Assert
        Assert.NotNull(_viewModel.ProductCodes);
        Assert.Equal(2, _viewModel.ProductCodes.Count());
        Assert.Contains(_viewModel.ProductCodes, c => c.Code == "EAN-123");
        Assert.Contains(_viewModel.ProductCodes, c => c.Code == "UPC-456");

        Assert.NotNull(_viewModel.ProductUnits);
        Assert.Equal(2, _viewModel.ProductUnits.Count());
        Assert.Contains(_viewModel.ProductUnits, u => u.UnitType == "Box");
        Assert.Contains(_viewModel.ProductUnits, u => u.UnitType == "Pallet");

        Assert.NotNull(_viewModel.ProductSuppliers);
        Assert.Equal(2, _viewModel.ProductSuppliers.Count());
        Assert.Contains(_viewModel.ProductSuppliers, s => s.SupplierName == "Supplier A");
        Assert.Contains(_viewModel.ProductSuppliers, s => s.SupplierName == "Supplier B");

        _mockProductService.Verify(s => s.GetProductCodesAsync(productId), Times.Once);
        _mockProductService.Verify(s => s.GetProductUnitsAsync(productId), Times.Once);
        _mockProductService.Verify(s => s.GetProductSuppliersAsync(productId), Times.Once);
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
        var productId = Guid.NewGuid();
        var expectedProduct = new ProductDto
        {
            Id = productId,
            Code = "PROD-001",
            Name = "Test Product",
            Status = ProductStatus.Active,
            IsVatIncluded = false,
            IsBundle = false
        };

        _mockProductService.Setup(s => s.GetProductDetailAsync(productId))
            .ReturnsAsync(expectedProduct);

        _mockProductService.Setup(s => s.GetProductCodesAsync(productId))
            .ReturnsAsync(new List<ProductCodeDto>());

        _mockProductService.Setup(s => s.GetProductUnitsAsync(productId))
            .ReturnsAsync(new List<ProductUnitDto>());

        _mockProductService.Setup(s => s.GetProductSuppliersAsync(productId))
            .ReturnsAsync(new List<ProductSupplierDto>());

        // Act
        await _viewModel.LoadEntityAsync(productId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(productId, _viewModel.Entity.Id);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
