using EventForge.DTOs.Documents;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.Warehouse;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentHeaderService focusing on merge duplicate rows functionality.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentRowMergeTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<DocumentHeaderService>> _mockLogger;
    private readonly Mock<IDocumentCounterService> _mockCounterService;
    private readonly Mock<IStockMovementService> _mockStockMovementService;
    private readonly Mock<IUnitConversionService> _mockUnitConversionService;
    private readonly DocumentHeaderService _documentHeaderService;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _documentHeaderId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public DocumentRowMergeTests()
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
        _mockUnitConversionService = new Mock<IUnitConversionService>();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create service
        _documentHeaderService = new DocumentHeaderService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockCounterService.Object,
            _mockStockMovementService.Object,
            _mockUnitConversionService.Object,
            _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var documentHeader = new DocumentHeader
        {
            Id = _documentHeaderId,
            TenantId = _tenantId,
            Number = "DOC-001",
            Date = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        _context.DocumentHeaders.Add(documentHeader);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithoutMerge_CreatesNewRow()
    {
        // Arrange
        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 5,
            UnitPrice = 10.00m,
            MergeDuplicateProducts = false
        };

        // Act
        var result = await _documentHeaderService.AddDocumentRowAsync(createDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Quantity);
        Assert.Equal(_productId, result.ProductId);

        // Verify a new row was created in database
        var rowsInDb = await _context.DocumentRows
            .Where(r => r.DocumentHeaderId == _documentHeaderId && !r.IsDeleted)
            .ToListAsync();
        Assert.Single(rowsInDb);
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithMerge_WhenNoDuplicate_CreatesNewRow()
    {
        // Arrange
        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 5,
            UnitPrice = 10.00m,
            MergeDuplicateProducts = true
        };

        // Act
        var result = await _documentHeaderService.AddDocumentRowAsync(createDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Quantity);

        var rowsInDb = await _context.DocumentRows
            .Where(r => r.DocumentHeaderId == _documentHeaderId && !r.IsDeleted)
            .ToListAsync();
        Assert.Single(rowsInDb);
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithMerge_WhenDuplicateExists_UpdatesQuantity()
    {
        // Arrange - Create first row
        var firstDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 5,
            UnitPrice = 10.00m,
            MergeDuplicateProducts = false
        };
        var firstRow = await _documentHeaderService.AddDocumentRowAsync(firstDto, "test-user");

        // Act - Add same product with merge enabled
        var secondDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 3,
            UnitPrice = 10.00m,
            MergeDuplicateProducts = true
        };
        var result = await _documentHeaderService.AddDocumentRowAsync(secondDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.Quantity); // 5 + 3
        Assert.Equal(firstRow.Id, result.Id); // Same row updated

        // Verify only one row exists in database
        var rowsInDb = await _context.DocumentRows
            .Where(r => r.DocumentHeaderId == _documentHeaderId && !r.IsDeleted)
            .ToListAsync();
        Assert.Single(rowsInDb);
        Assert.Equal(8, rowsInDb[0].Quantity);
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithoutMerge_WhenDuplicateExists_CreatesSeparateRow()
    {
        // Arrange - Create first row
        var firstDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 5,
            UnitPrice = 10.00m,
            MergeDuplicateProducts = false
        };
        await _documentHeaderService.AddDocumentRowAsync(firstDto, "test-user");

        // Act - Add same product without merge
        var secondDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 3,
            UnitPrice = 10.00m,
            MergeDuplicateProducts = false
        };
        var result = await _documentHeaderService.AddDocumentRowAsync(secondDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Quantity); // New row with 3

        // Verify two separate rows exist
        var rowsInDb = await _context.DocumentRows
            .Where(r => r.DocumentHeaderId == _documentHeaderId && !r.IsDeleted)
            .ToListAsync();
        Assert.Equal(2, rowsInDb.Count);
    }

    [Fact]
    public async Task AddDocumentRowAsync_WithMerge_DifferentProducts_CreatesNewRows()
    {
        // Arrange - Create first row with product 1
        var firstDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            Description = "Test Product 1",
            Quantity = 5,
            UnitPrice = 10.00m,
            MergeDuplicateProducts = true
        };
        await _documentHeaderService.AddDocumentRowAsync(firstDto, "test-user");

        // Act - Add different product with merge enabled
        var secondDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = _documentHeaderId,
            ProductId = Guid.NewGuid(), // Different product
            Description = "Test Product 2",
            Quantity = 3,
            UnitPrice = 15.00m,
            MergeDuplicateProducts = true
        };
        var result = await _documentHeaderService.AddDocumentRowAsync(secondDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Quantity);

        // Verify two separate rows exist
        var rowsInDb = await _context.DocumentRows
            .Where(r => r.DocumentHeaderId == _documentHeaderId && !r.IsDeleted)
            .ToListAsync();
        Assert.Equal(2, rowsInDb.Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
