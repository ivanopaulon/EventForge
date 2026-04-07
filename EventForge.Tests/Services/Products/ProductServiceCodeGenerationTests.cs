using EventForge.DTOs.Products;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.CodeGeneration;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for ProductService focusing on automatic code generation.
/// </summary>
[Trait("Category", "Unit")]
public class ProductServiceCodeGenerationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly Mock<IDailyCodeGenerator> _mockCodeGenerator;
    private readonly Mock<EventForge.Server.Services.PriceHistory.ISupplierProductPriceHistoryService> _mockPriceHistoryService;
    private readonly ProductService _productService;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ProductServiceCodeGenerationTests()
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
        _mockCodeGenerator = new Mock<IDailyCodeGenerator>();
        _mockPriceHistoryService = new Mock<EventForge.Server.Services.PriceHistory.ISupplierProductPriceHistoryService>();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create ProductService with mocked code generator
        _productService = new ProductService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object,
            _mockCodeGenerator.Object,
            _mockPriceHistoryService.Object);
    }

    [Fact]
    public async Task CreateProductAsync_WithEmptyCode_GeneratesCode()
    {
        // Arrange
        var expectedCode = "20251110000001";
        _ = _mockCodeGenerator
            .Setup(x => x.GenerateDailyCodeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCode);

        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Code = string.Empty, // Empty code should trigger generation
            Status = EventForge.DTOs.Common.ProductStatus.Active
        };

        // Act
        var result = await _productService.CreateProductAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Code);
        _mockCodeGenerator.Verify(x => x.GenerateDailyCodeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WithNullCode_GeneratesCode()
    {
        // Arrange
        var expectedCode = "20251110000002";
        _ = _mockCodeGenerator
            .Setup(x => x.GenerateDailyCodeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCode);

        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Code = null!, // Null code should trigger generation
            Status = EventForge.DTOs.Common.ProductStatus.Active
        };

        // Act
        var result = await _productService.CreateProductAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Code);
        _mockCodeGenerator.Verify(x => x.GenerateDailyCodeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WithProvidedCode_DoesNotGenerateCode()
    {
        // Arrange
        var providedCode = "CUSTOM001";
        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Code = providedCode,
            Status = EventForge.DTOs.Common.ProductStatus.Active
        };

        // Act
        var result = await _productService.CreateProductAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(providedCode, result.Code);
        _mockCodeGenerator.Verify(x => x.GenerateDailyCodeAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_WithWhitespaceCode_GeneratesCode()
    {
        // Arrange
        var expectedCode = "20251110000003";
        _ = _mockCodeGenerator
            .Setup(x => x.GenerateDailyCodeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCode);

        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Code = "   ", // Whitespace should be treated as empty
            Status = EventForge.DTOs.Common.ProductStatus.Active
        };

        // Act
        var result = await _productService.CreateProductAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCode, result.Code);
        _mockCodeGenerator.Verify(x => x.GenerateDailyCodeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_CodeFieldNotInDto_PreservesExistingCode()
    {
        // Arrange
        var originalCode = "ORIGINAL001";
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Original Product",
            Code = originalCode,
            Status = Server.Data.Entities.Products.ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "testuser"
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateProductDto
        {
            Name = "Updated Product",
            Status = EventForge.DTOs.Common.ProductStatus.Active
        };

        // Act
        var result = await _productService.UpdateProductAsync(product.Id, updateDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Product", result.Name);
        Assert.Equal(originalCode, result.Code); // Code should remain unchanged
    }

    [Fact]
    public async Task CreateProductAsync_GeneratedCodeStoredInDatabase()
    {
        // Arrange
        var expectedCode = "20251110000004";
        _ = _mockCodeGenerator
            .Setup(x => x.GenerateDailyCodeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCode);

        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Code = string.Empty,
            Status = EventForge.DTOs.Common.ProductStatus.Active
        };

        // Act
        var result = await _productService.CreateProductAsync(createDto, "testuser");

        // Assert
        var productInDb = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == result.Id);

        Assert.NotNull(productInDb);
        Assert.Equal(expectedCode, productInDb.Code);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
