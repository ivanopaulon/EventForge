using EventForge.DTOs.Documents;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Warehouse;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.Warehouse;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using DtoApprovalStatus = EventForge.DTOs.Common.ApprovalStatus;
using EntityApprovalStatus = EventForge.Server.Data.Entities.Documents.ApprovalStatus;
using EntityBusinessPartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentHeaderService focusing on stock movement creation.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentHeaderStockMovementTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<DocumentHeaderService>> _mockLogger;
    private readonly Mock<IDocumentCounterService> _mockDocumentCounterService;
    private readonly StockMovementService _stockMovementService;
    private readonly Mock<ILogger<StockMovementService>> _mockStockMovementLogger;
    private readonly Mock<IUnitConversionService> _mockUnitConversionService;
    private readonly DocumentHeaderService _documentHeaderService;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _storageLocationId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _businessPartyId = Guid.NewGuid();

    public DocumentHeaderStockMovementTests()
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
        _mockStockMovementLogger = new Mock<ILogger<StockMovementService>>();
        _mockDocumentCounterService = new Mock<IDocumentCounterService>();
        _mockUnitConversionService = new Mock<IUnitConversionService>();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create real StockMovementService
        _stockMovementService = new StockMovementService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockStockMovementLogger.Object);

        // Create DocumentHeaderService with real StockMovementService
        _documentHeaderService = new DocumentHeaderService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockDocumentCounterService.Object,
            _stockMovementService,
            _mockUnitConversionService.Object,
            _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create warehouse
        var warehouse = new StorageFacility
        {
            Id = _warehouseId,
            TenantId = _tenantId,
            Name = "Main Warehouse",
            Code = "WH001",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StorageFacilities.Add(warehouse);

        // Create storage location
        var storageLocation = new StorageLocation
        {
            Id = _storageLocationId,
            TenantId = _tenantId,
            WarehouseId = _warehouseId,
            Code = "LOC-001",
            Description = "Test Location",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StorageLocations.Add(storageLocation);

        // Create product
        var product = new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Name = "Test Product",
            Code = "PROD-001",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Products.Add(product);

        // Create business party
        var businessParty = new BusinessParty
        {
            Id = _businessPartyId,
            TenantId = _tenantId,
            Name = "Test Supplier",
            PartyType = EntityBusinessPartyType.Fornitore,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.BusinessParties.Add(businessParty);

        // Create initial stock
        var stock = new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = _productId,
            StorageLocationId = _storageLocationId,
            Quantity = 100,
            ReservedQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Stocks.Add(stock);

        _context.SaveChanges();
    }

    [Fact]
    public async Task ApproveDocumentAsync_WithStockIncreaseDocument_CreatesInboundMovement()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Purchase Order",
            Code = "PO",
            IsStockIncrease = true,
            DefaultWarehouseId = _warehouseId,
            RequiredPartyType = EntityBusinessPartyType.Fornitore,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(documentType);

        var documentHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = documentType.Id,
            BusinessPartyId = _businessPartyId,
            Number = "PO-001",
            Date = DateTime.UtcNow,
            ApprovalStatus = EntityApprovalStatus.None,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(documentHeader);

        var documentRow = new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = documentHeader.Id,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 10,
            UnitPrice = 50,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentRows.Add(documentRow);

        await _context.SaveChangesAsync();

        var initialStockQuantity = await _context.Stocks
            .Where(s => s.ProductId == _productId && s.StorageLocationId == _storageLocationId)
            .SumAsync(s => s.Quantity);

        // Act
        var result = await _documentHeaderService.ApproveDocumentAsync(documentHeader.Id, "test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DtoApprovalStatus.Approved, result.ApprovalStatus);

        // Check that stock movement was created
        var movements = await _context.StockMovements
            .Where(sm => sm.DocumentHeaderId == documentHeader.Id)
            .ToListAsync();

        Assert.Single(movements);
        var movement = movements.First();
        Assert.Equal(StockMovementType.Inbound, movement.MovementType);
        Assert.Equal(_productId, movement.ProductId);
        Assert.Equal(10, movement.Quantity);
        Assert.Equal(_storageLocationId, movement.ToLocationId);

        // Check that stock was increased
        var finalStockQuantity = await _context.Stocks
            .Where(s => s.ProductId == _productId && s.StorageLocationId == _storageLocationId)
            .SumAsync(s => s.Quantity);

        Assert.Equal(initialStockQuantity + 10, finalStockQuantity);
    }

    [Fact]
    public async Task ApproveDocumentAsync_WithStockDecreaseDocument_CreatesOutboundMovement()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Sales Order",
            Code = "SO",
            IsStockIncrease = false,
            DefaultWarehouseId = _warehouseId,
            RequiredPartyType = EntityBusinessPartyType.Cliente,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(documentType);

        var businessPartyCustomer = new BusinessParty
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Customer",
            PartyType = EntityBusinessPartyType.Cliente,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.BusinessParties.Add(businessPartyCustomer);

        var documentHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = documentType.Id,
            BusinessPartyId = businessPartyCustomer.Id,
            Number = "SO-001",
            Date = DateTime.UtcNow,
            ApprovalStatus = EntityApprovalStatus.None,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(documentHeader);

        var documentRow = new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = documentHeader.Id,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 5,
            UnitPrice = 100,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentRows.Add(documentRow);

        await _context.SaveChangesAsync();

        var initialStockQuantity = await _context.Stocks
            .Where(s => s.ProductId == _productId && s.StorageLocationId == _storageLocationId)
            .SumAsync(s => s.Quantity);

        // Act
        var result = await _documentHeaderService.ApproveDocumentAsync(documentHeader.Id, "test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DtoApprovalStatus.Approved, result.ApprovalStatus);

        // Check that stock movement was created
        var movements = await _context.StockMovements
            .Where(sm => sm.DocumentHeaderId == documentHeader.Id)
            .ToListAsync();

        Assert.Single(movements);
        var movement = movements.First();
        Assert.Equal(StockMovementType.Outbound, movement.MovementType);
        Assert.Equal(_productId, movement.ProductId);
        Assert.Equal(5, movement.Quantity);
        Assert.Equal(_storageLocationId, movement.FromLocationId);

        // Check that stock was decreased
        var finalStockQuantity = await _context.Stocks
            .Where(s => s.ProductId == _productId && s.StorageLocationId == _storageLocationId)
            .SumAsync(s => s.Quantity);

        Assert.Equal(initialStockQuantity - 5, finalStockQuantity);
    }

    [Fact]
    public async Task ApproveDocumentAsync_WithoutProducts_DoesNotCreateMovements()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Service Invoice",
            Code = "SI",
            IsStockIncrease = false,
            RequiredPartyType = EntityBusinessPartyType.Cliente,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(documentType);

        var businessPartyCustomer = new BusinessParty
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Customer",
            PartyType = EntityBusinessPartyType.Cliente,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.BusinessParties.Add(businessPartyCustomer);

        var documentHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = documentType.Id,
            BusinessPartyId = businessPartyCustomer.Id,
            Number = "SI-001",
            Date = DateTime.UtcNow,
            ApprovalStatus = EntityApprovalStatus.None,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(documentHeader);

        var documentRow = new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = documentHeader.Id,
            ProductId = null, // No product (service row)
            Description = "Consulting Service",
            Quantity = 1,
            UnitPrice = 500,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentRows.Add(documentRow);

        await _context.SaveChangesAsync();

        // Act
        var result = await _documentHeaderService.ApproveDocumentAsync(documentHeader.Id, "test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DtoApprovalStatus.Approved, result.ApprovalStatus);

        // Check that no stock movement was created
        var movements = await _context.StockMovements
            .Where(sm => sm.DocumentHeaderId == documentHeader.Id)
            .ToListAsync();

        Assert.Empty(movements);
    }

    [Fact]
    public async Task ApproveDocumentAsync_AlreadyApproved_DoesNotDuplicateMovements()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Purchase Order",
            Code = "PO",
            IsStockIncrease = true,
            DefaultWarehouseId = _warehouseId,
            RequiredPartyType = EntityBusinessPartyType.Fornitore,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(documentType);

        var documentHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = documentType.Id,
            BusinessPartyId = _businessPartyId,
            Number = "PO-002",
            Date = DateTime.UtcNow,
            ApprovalStatus = EntityApprovalStatus.None,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(documentHeader);

        var documentRow = new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = documentHeader.Id,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 10,
            UnitPrice = 50,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentRows.Add(documentRow);

        await _context.SaveChangesAsync();

        // First approval
        await _documentHeaderService.ApproveDocumentAsync(documentHeader.Id, "test");

        var movementsAfterFirstApproval = await _context.StockMovements
            .Where(sm => sm.DocumentHeaderId == documentHeader.Id)
            .CountAsync();

        // Act - Try to approve again (simulating re-approval)
        // Reset approval status
        var docToReapprove = await _context.DocumentHeaders.FindAsync(documentHeader.Id);
        Assert.NotNull(docToReapprove);
        docToReapprove.ApprovalStatus = EntityApprovalStatus.None;
        await _context.SaveChangesAsync();

        await _documentHeaderService.ApproveDocumentAsync(documentHeader.Id, "test");

        // Assert
        var movementsAfterSecondApproval = await _context.StockMovements
            .Where(sm => sm.DocumentHeaderId == documentHeader.Id)
            .CountAsync();

        Assert.Equal(1, movementsAfterFirstApproval);
        Assert.Equal(1, movementsAfterSecondApproval); // Should not duplicate
    }

    [Fact]
    public async Task ApproveDocument_UsesDocumentDateForMovement()
    {
        // Arrange
        var documentDate = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        
        var documentType = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Purchase Order",
            Code = "PO",
            IsStockIncrease = true,
            DefaultWarehouseId = _warehouseId,
            RequiredPartyType = EntityBusinessPartyType.Fornitore,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(documentType);

        var documentHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = documentType.Id,
            BusinessPartyId = _businessPartyId,
            Number = "PO-003",
            Date = documentDate,
            ApprovalStatus = EntityApprovalStatus.None,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(documentHeader);

        var documentRow = new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = documentHeader.Id,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 10,
            UnitPrice = 50,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentRows.Add(documentRow);

        await _context.SaveChangesAsync();

        // Act
        var result = await _documentHeaderService.ApproveDocumentAsync(documentHeader.Id, "test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DtoApprovalStatus.Approved, result.ApprovalStatus);

        // Check that stock movement was created with the document date
        var movements = await _context.StockMovements
            .Where(sm => sm.DocumentHeaderId == documentHeader.Id)
            .ToListAsync();

        Assert.Single(movements);
        var movement = movements.First();
        Assert.Equal(documentDate, movement.MovementDate);
    }

    [Fact]
    public async Task UpdateDocumentHeader_ChangingDate_SyncsMovementDates()
    {
        // Arrange
        var originalDate = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var newDate = new DateTime(2024, 6, 20, 14, 45, 0, DateTimeKind.Utc);
        
        var documentType = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Purchase Order",
            Code = "PO",
            IsStockIncrease = true,
            DefaultWarehouseId = _warehouseId,
            RequiredPartyType = EntityBusinessPartyType.Fornitore,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(documentType);

        var documentHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = documentType.Id,
            BusinessPartyId = _businessPartyId,
            Number = "PO-004",
            Date = originalDate,
            ApprovalStatus = EntityApprovalStatus.None,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(documentHeader);

        var documentRow = new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = documentHeader.Id,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 10,
            UnitPrice = 50,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentRows.Add(documentRow);

        await _context.SaveChangesAsync();

        // Approve document to create stock movements
        await _documentHeaderService.ApproveDocumentAsync(documentHeader.Id, "test");

        // Verify movement was created with original date
        var movementsBeforeUpdate = await _context.StockMovements
            .Where(sm => sm.DocumentHeaderId == documentHeader.Id)
            .ToListAsync();
        Assert.Single(movementsBeforeUpdate);
        Assert.Equal(originalDate, movementsBeforeUpdate.First().MovementDate);

        // Act - Update document date
        var updateDto = new UpdateDocumentHeaderDto
        {
            DocumentTypeId = documentType.Id,
            Number = "PO-004",
            Date = newDate,
            BusinessPartyId = _businessPartyId
        };
        
        var updatedDocument = await _documentHeaderService.UpdateDocumentHeaderAsync(documentHeader.Id, updateDto, "testUpdater");

        // Assert
        Assert.NotNull(updatedDocument);
        Assert.Equal(newDate, updatedDocument.Date);

        // Check that stock movement date was synced
        var movementsAfterUpdate = await _context.StockMovements
            .Where(sm => sm.DocumentHeaderId == documentHeader.Id)
            .ToListAsync();

        Assert.Single(movementsAfterUpdate);
        var updatedMovement = movementsAfterUpdate.First();
        Assert.Equal(newDate, updatedMovement.MovementDate);
        Assert.NotNull(updatedMovement.ModifiedAt);
        // Note: In tests, ModifiedBy is set by DbContext.SaveChangesAsync which uses "system" as default
        Assert.Equal("system", updatedMovement.ModifiedBy);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
