using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for ProductService focusing on barcode/code lookup functionality.
/// </summary>
[Trait("Category", "Unit")]
public class ProductServiceBarcodeTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly Mock<EventForge.Server.Services.CodeGeneration.IDailyCodeGenerator> _mockCodeGenerator;
    private readonly ProductService _productService;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _productUnitId = Guid.NewGuid();
    private readonly Guid _unitOfMeasureId = Guid.NewGuid();

    public ProductServiceBarcodeTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        // Create mocks
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<ProductService>>();
        _mockCodeGenerator = new Mock<EventForge.Server.Services.CodeGeneration.IDailyCodeGenerator>();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create ProductService
        _productService = new ProductService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object,
            _mockCodeGenerator.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create product
        var product = new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Name = "Test Product",
            Code = "PROD001",
            Status = Server.Data.Entities.Products.ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Products.Add(product);

        // Create product unit
        var productUnit = new ProductUnit
        {
            Id = _productUnitId,
            TenantId = _tenantId,
            ProductId = _productId,
            UnitOfMeasureId = _unitOfMeasureId,
            ConversionFactor = 1,
            UnitType = "Base",
            Status = Server.Data.Entities.Products.ProductUnitStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.ProductUnits.Add(productUnit);

        // Create product code with ProductUnitId (barcode assigned to specific unit)
        var productCode = new ProductCode
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = _productId,
            ProductUnitId = _productUnitId,
            Code = "123456789",
            CodeType = "EAN",
            Status = Server.Data.Entities.Products.ProductCodeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.ProductCodes.Add(productCode);

        // Create product code without ProductUnitId (barcode not assigned to specific unit)
        var productCodeNoUnit = new ProductCode
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = _productId,
            ProductUnitId = null,
            Code = "987654321",
            CodeType = "SKU",
            Status = Server.Data.Entities.Products.ProductCodeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.ProductCodes.Add(productCodeNoUnit);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProductByCodeAsync_WithValidCode_ReturnsProduct()
    {
        // Arrange
        var code = "123456789";

        // Act
        var result = await _productService.GetProductByCodeAsync(code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_productId, result.Id);
        Assert.Equal("Test Product", result.Name);
    }

    [Fact]
    public async Task GetProductByCodeAsync_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        var code = "INVALID";

        // Act
        var result = await _productService.GetProductByCodeAsync(code);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductWithCodeByCodeAsync_WithCodeAssignedToUnit_ReturnsProductWithUnitContext()
    {
        // Arrange
        var code = "123456789"; // This barcode is assigned to a specific ProductUnitId

        // Act
        var result = await _productService.GetProductWithCodeByCodeAsync(code);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Product);
        Assert.NotNull(result.Code);

        // Verify product information
        Assert.Equal(_productId, result.Product.Id);
        Assert.Equal("Test Product", result.Product.Name);

        // Verify code context with ProductUnitId
        Assert.Equal(code, result.Code.Code);
        Assert.Equal("EAN", result.Code.CodeType);
        Assert.Equal(_productUnitId, result.Code.ProductUnitId);
    }

    [Fact]
    public async Task GetProductWithCodeByCodeAsync_WithCodeNotAssignedToUnit_ReturnsProductWithCodeButNoUnit()
    {
        // Arrange
        var code = "987654321"; // This barcode is NOT assigned to a specific ProductUnitId

        // Act
        var result = await _productService.GetProductWithCodeByCodeAsync(code);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Product);
        Assert.NotNull(result.Code);

        // Verify product information
        Assert.Equal(_productId, result.Product.Id);
        Assert.Equal("Test Product", result.Product.Name);

        // Verify code context without ProductUnitId
        Assert.Equal(code, result.Code.Code);
        Assert.Equal("SKU", result.Code.CodeType);
        Assert.Null(result.Code.ProductUnitId);
    }

    [Fact]
    public async Task GetProductWithCodeByCodeAsync_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        var code = "INVALID";

        // Act
        var result = await _productService.GetProductWithCodeByCodeAsync(code);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductWithCodeByCodeAsync_WithDeletedProduct_ReturnsNull()
    {
        // Arrange
        var code = "999999999";
        var deletedProduct = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Deleted Product",
            Code = "DELPROD",
            Status = Server.Data.Entities.Products.ProductStatus.Active,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Products.Add(deletedProduct);

        var deletedProductCode = new ProductCode
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = deletedProduct.Id,
            Code = code,
            CodeType = "EAN",
            Status = Server.Data.Entities.Products.ProductCodeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.ProductCodes.Add(deletedProductCode);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetProductWithCodeByCodeAsync(code);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductsAsync_SearchByAliasCode_ReturnsMatchingProduct()
    {
        // Arrange - search by a partial alias code
        var pagination = new PaginationParameters { Page = 1, PageSize = 10 };
        var searchTerm = "12345"; // partial match of alias code "123456789"

        // Act
        var result = await _productService.GetProductsAsync(pagination, searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, p => p.Id == _productId);
    }

    [Fact]
    public async Task GetProductsAsync_SearchByAliasCode_ExcludesDeletedCodes()
    {
        // Arrange - add a deleted alias code with a unique search term
        var deletedCode = new ProductCode
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = _productId,
            Code = "DELETED-ALIAS-XYZ",
            CodeType = "SKU",
            Status = Server.Data.Entities.Products.ProductCodeStatus.Active,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.ProductCodes.Add(deletedCode);
        await _context.SaveChangesAsync();

        var pagination = new PaginationParameters { Page = 1, PageSize = 10 };
        var searchTerm = "DELETED-ALIAS-XYZ";

        // Act
        var result = await _productService.GetProductsAsync(pagination, searchTerm);

        // Assert - deleted alias codes should not surface the product
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetProductsForPosCatalogAsync_SearchByAliasCode_ReturnsMatchingProduct()
    {
        // Arrange - search by a partial alias code
        var pagination = new PaginationParameters { Page = 1, PageSize = 10 };
        var searchTerm = "987654"; // partial match of alias code "987654321"

        // Act
        var result = await _productService.GetProductsForPosCatalogAsync(pagination, searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, p => p.Id == _productId);
    }

    [Fact]
    public async Task GetProductsAsync_SearchByDescription_ReturnsMatchingProduct()
    {
        // Arrange - add a product whose search term appears only in Description
        var descriptionProduct = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Product Without Desc In Name",
            Code = "DESCONLY001",
            Description = "Unique description text for C4 regression",
            Status = Server.Data.Entities.Products.ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Products.Add(descriptionProduct);
        await _context.SaveChangesAsync();

        var pagination = new PaginationParameters { Page = 1, PageSize = 10 };
        var searchTerm = "C4 regression"; // only in Description

        // Act
        var result = await _productService.GetProductsAsync(pagination, searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, p => p.Id == descriptionProduct.Id);
    }

    [Fact]
    public async Task GetProductsForPosCatalogAsync_SearchByDescription_ReturnsMatchingProduct()
    {
        // Arrange - add a product whose search term appears only in Description
        var descriptionProduct = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "POS Product Without Desc In Name",
            Code = "POSDESCONLY001",
            Description = "Unique POS description text for C4 regression",
            Status = Server.Data.Entities.Products.ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Products.Add(descriptionProduct);
        await _context.SaveChangesAsync();

        var pagination = new PaginationParameters { Page = 1, PageSize = 10 };
        var searchTerm = "POS description text"; // only in Description

        // Act
        var result = await _productService.GetProductsForPosCatalogAsync(pagination, searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, p => p.Id == descriptionProduct.Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
