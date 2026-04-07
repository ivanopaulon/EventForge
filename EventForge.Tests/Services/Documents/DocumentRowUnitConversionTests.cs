using EventForge.DTOs.Documents;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Common;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentHeaderService focusing on unit conversion and base quantity calculations.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentRowUnitConversionTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<DocumentHeaderService>> _mockLogger;
    private readonly Mock<IDocumentCounterService> _mockCounterService;
    private readonly Mock<IStockMovementService> _mockStockMovementService;
    private readonly IUnitConversionService _unitConversionService;
    private readonly DocumentHeaderService _documentHeaderService;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _documentHeaderId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _baseUnitId = Guid.NewGuid();
    private readonly Guid _packUnitId = Guid.NewGuid();

    public DocumentRowUnitConversionTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        // Create mocks
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<DocumentHeaderService>>();
        _mockCounterService = new Mock<IDocumentCounterService>();
        _mockStockMovementService = new Mock<IStockMovementService>();

        // Use real UnitConversionService instead of mock
        _unitConversionService = new UnitConversionService();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create service with real UnitConversionService
        _documentHeaderService = new DocumentHeaderService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockCounterService.Object,
            _mockStockMovementService.Object,
            _unitConversionService,
            _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create document header
        var documentHeader = new DocumentHeader
        {
            Id = _documentHeaderId,
            TenantId = _tenantId,
            Number = "DOC-001",
            Date = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        // Create product
        var product = new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Name = "Test Product",
            Code = "TEST-001",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        // Create units of measure
        var baseUnit = new UM
        {
            Id = _baseUnitId,
            TenantId = _tenantId,
            Name = "Piece",
            Symbol = "pc",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        var packUnit = new UM
        {
            Id = _packUnitId,
            TenantId = _tenantId,
            Name = "Pack",
            Symbol = "pk",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        // Create product units with conversion factors
        var baseProductUnit = new ProductUnit
        {
            Id = Guid.NewGuid(),
            ProductId = _productId,
            UnitOfMeasureId = _baseUnitId,
            ConversionFactor = 1m, // Base unit
            UnitType = "Base",
            TenantId = _tenantId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        var packProductUnit = new ProductUnit
        {
            Id = Guid.NewGuid(),
            ProductId = _productId,
            UnitOfMeasureId = _packUnitId,
            ConversionFactor = 6m, // Pack of 6
            UnitType = "Pack",
            TenantId = _tenantId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        _context.DocumentHeaders.Add(documentHeader);
        _context.Products.Add(product);
        _context.UMs.Add(baseUnit);
        _context.UMs.Add(packUnit);
        _context.ProductUnits.Add(baseProductUnit);
        _context.ProductUnits.Add(packProductUnit);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithPackUnit_ComputesBaseQuantityCorrectly()
    {
        // Arrange - Add 2 packs (with conversion factor 6, should give 12 base units)
        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 2m, // 2 packs
            UnitOfMeasureId = _packUnitId,
            UnitPrice = 60.00m, // 60 per pack
            MergeDuplicateProducts = false
        };

        // Act
        var result = await _documentHeaderService.AddDocumentRowAsync(createDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2m, result.Quantity); // Display quantity in packs
        Assert.Equal(12m, result.BaseQuantity); // Base quantity = 2 * 6 = 12 pieces
        Assert.Equal(_baseUnitId, result.BaseUnitOfMeasureId);

        // Base unit price should be 10 (60 / 6 conversion factor)
        Assert.Equal(10m, result.BaseUnitPrice);
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithBaseUnit_BaseQuantityEqualsQuantity()
    {
        // Arrange - Add directly in base units
        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 12m, // 12 pieces
            UnitOfMeasureId = _baseUnitId,
            UnitPrice = 10.00m, // 10 per piece
            MergeDuplicateProducts = false
        };

        // Act
        var result = await _documentHeaderService.AddDocumentRowAsync(createDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12m, result.Quantity); // Display quantity
        Assert.Equal(12m, result.BaseQuantity); // Base quantity same as quantity for base unit
        Assert.Equal(_baseUnitId, result.BaseUnitOfMeasureId);
        Assert.Equal(10m, result.BaseUnitPrice); // Price same for base unit
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithDecimalQuantity_HandlesCorrectly()
    {
        // Arrange - Add fractional packs
        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 2.5m, // 2.5 packs
            UnitOfMeasureId = _packUnitId,
            UnitPrice = 60.00m,
            MergeDuplicateProducts = false
        };

        // Act
        var result = await _documentHeaderService.AddDocumentRowAsync(createDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2.5m, result.Quantity); // Display quantity
        Assert.Equal(15m, result.BaseQuantity); // Base quantity = 2.5 * 6 = 15 pieces
        Assert.Equal(_baseUnitId, result.BaseUnitOfMeasureId);
        Assert.Equal(10m, result.BaseUnitPrice);
    }

    [Fact]
    public async Task AddDocumentRowAsync_MergeDifferentUnits_SumsBaseQuantityCorrectly()
    {
        // Arrange - First add 2 packs
        var firstDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 2m, // 2 packs = 12 base units
            UnitOfMeasureId = _packUnitId,
            UnitPrice = 60.00m,
            MergeDuplicateProducts = true
        };

        await _documentHeaderService.AddDocumentRowAsync(firstDto, "test-user");

        // Act - Now add 6 more pieces (base units)
        var secondDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 6m, // 6 pieces
            UnitOfMeasureId = _baseUnitId,
            UnitPrice = 10.00m,
            MergeDuplicateProducts = true
        };

        var result = await _documentHeaderService.AddDocumentRowAsync(secondDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(18m, result.BaseQuantity); // Total: 12 + 6 = 18 base units

        // Display quantity should be recalculated based on the first row's unit (packs)
        // 18 base units / 6 (pack factor) = 3 packs
        Assert.Equal(3m, result.Quantity);

        // Verify only one row exists
        var rowsInDb = await _context.DocumentRows
            .Where(r => r.DocumentHeaderId == _documentHeaderId && !r.IsDeleted)
            .ToListAsync();
        Assert.Single(rowsInDb);
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithoutProductUnit_DoesNotComputeBaseQuantity()
    {
        // Arrange - Create a new product without product units
        var newProductId = Guid.NewGuid();
        var newProduct = new Product
        {
            Id = newProductId,
            TenantId = _tenantId,
            Name = "Product Without Units",
            Code = "TEST-002",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
        _context.Products.Add(newProduct);
        _context.SaveChanges();

        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = newProductId,
            Description = "Product Without Units",
            Quantity = 5m,
            UnitOfMeasureId = _baseUnitId, // Unit exists but no ProductUnit mapping
            UnitPrice = 10.00m,
            MergeDuplicateProducts = false
        };

        // Act
        var result = await _documentHeaderService.AddDocumentRowAsync(createDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5m, result.Quantity);
        Assert.Null(result.BaseQuantity); // Should be null when no ProductUnit mapping exists
        Assert.Null(result.BaseUnitPrice);
        Assert.Null(result.BaseUnitOfMeasureId);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
