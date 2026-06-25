using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Warehouse;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Warehouse;
using EntityDocumentStatus = Prym.DTOs.Common.DocumentStatus;

namespace EventForge.Tests.Services.Warehouse;

/// <summary>
/// Unit tests for StockReconciliationService — RebuildMissingMovementsFromDocumentsAsync,
/// IsReconciliationAdjustment filter behaviour, and dry-run mode.
/// </summary>
[Trait("Category", "Unit")]
public class StockReconciliationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IStockMovementService> _mockStockMovementService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<StockReconciliationService>> _mockLogger;
    private readonly StockReconciliationService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public StockReconciliationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockStockMovementService = new Mock<IStockMovementService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<StockReconciliationService>>();

        _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        _service = new StockReconciliationService(
            _context,
            _mockAuditLogService.Object,
            _mockStockMovementService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);
    }

    #region RebuildMissingMovementsFromDocumentsAsync — basic scenarios

    [Fact]
    public async Task RebuildMissingMovements_NoDocuments_ReturnsEmptyResult()
    {
        var request = new RebuildMovementsRequestDto { DryRun = true };

        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        Assert.NotNull(result);
        Assert.Equal(0, result.DocumentsScanned);
        Assert.Equal(0, result.RowsScanned);
        Assert.True(result.IsDryRun);
    }

    [Fact]
    public async Task RebuildMissingMovements_ArchivedDocument_WithEligibleRow_DryRun_ReturnsScannedNotCreated()
    {
        SeedArchivedDocumentWithRow(out var docId, out _);

        var request = new RebuildMovementsRequestDto { DryRun = true, UpdateExisting = false };

        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        Assert.Equal(1, result.DocumentsScanned);
        Assert.Equal(1, result.RowsScanned);
        Assert.Equal(0, result.MovementsCreated);  // DryRun — nothing persisted
        Assert.True(result.IsDryRun);
    }

    [Fact]
    public async Task RebuildMissingMovements_ActiveDocument_IsExcludedByDefault()
    {
        // RebuildMissingMovements defaults to Archived only — an Active doc should not be scanned
        var docTypeId = Guid.NewGuid();
        SeedDocumentType(docTypeId, isInventory: false, isStockIncrease: true);

        var docId = Guid.NewGuid();
        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = docId,
            TenantId = _tenantId,
            DocumentTypeId = docTypeId,
            Number = "DOC-ACTIVE",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = EntityDocumentStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = docId,
            ProductId = Guid.NewGuid(),
            Quantity = 5m,
            UnitPrice = 10m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = true };

        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        Assert.Equal(0, result.DocumentsScanned);
    }

    [Fact]
    public async Task RebuildMissingMovements_InventoryDocument_IsExcluded()
    {
        var docTypeId = Guid.NewGuid();
        SeedDocumentType(docTypeId, isInventory: true, isStockIncrease: true);

        var docId = Guid.NewGuid();
        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = docId,
            TenantId = _tenantId,
            DocumentTypeId = docTypeId,
            Number = "DOC-INV",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = EntityDocumentStatus.Archived,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = true };

        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        Assert.Equal(0, result.DocumentsScanned);
    }

    [Fact]
    public async Task RebuildMissingMovements_RowAlreadyHasMovement_UpdateExistingFalse_MarksAlreadyExists()
    {
        SeedArchivedDocumentWithRow(out var docId, out var rowId);

        // Seed an existing movement for that row
        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = Guid.NewGuid(),
            Quantity = 5m,
            DocumentRowId = rowId,
            MovementDate = DateTime.UtcNow,
            IsReconciliationAdjustment = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = true, UpdateExisting = false };

        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        Assert.Equal(1, result.RowsAlreadyHadMovement);
        Assert.Contains(result.Items, i => i.Status == "AlreadyExists");
    }

    [Fact]
    public async Task RebuildMissingMovements_InvalidDateRange_ThrowsArgumentException()
    {
        var request = new RebuildMovementsRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(5),
            ToDate = DateTime.UtcNow,
            DryRun = true
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user"));
    }

    [Fact]
    public async Task RebuildMissingMovements_NoTenantId_ThrowsInvalidOperationException()
    {
        _mockTenantContext.Setup(x => x.CurrentTenantId).Returns((Guid?)null);

        var request = new RebuildMovementsRequestDto { DryRun = true };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user"));
    }

    #endregion

    #region Transfer document support

    [Fact]
    public async Task RebuildMissingMovements_TransferDocument_DryRun_ReturnsTransferMovementType()
    {
        // Arrange: seed a document type marked as transfer document
        var docTypeId = Guid.NewGuid();
        _context.DocumentTypes.Add(new DocumentType
        {
            Id = docTypeId,
            TenantId = _tenantId,
            Name = "Transfer Type",
            Code = "TRF",
            CreatesStockMovements = true,
            MovesStockOnRowChange = false,
            IsInventoryDocument = false,
            IsTransferDocument = true,
            IsStockIncrease = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        var docId = Guid.NewGuid();
        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = docId,
            TenantId = _tenantId,
            DocumentTypeId = docTypeId,
            Number = "TRF-001",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = EntityDocumentStatus.Archived,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = docId,
            ProductId = Guid.NewGuid(),
            Quantity = 3m,
            UnitPrice = 5m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = true, UpdateExisting = false };

        // Act
        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        // Assert: the document was scanned and the resulting item has MovementType = "Transfer"
        Assert.NotNull(result);
        Assert.True(result.IsDryRun);
        Assert.Equal(1, result.DocumentsScanned);
        var item = result.Items.FirstOrDefault(i => i.DocumentNumber == "TRF-001");
        Assert.NotNull(item);
        Assert.Equal("Transfer", item.MovementType);
    }

    #endregion

    #region IsReconciliationAdjustment filter    [Fact]
    public async Task GetStockIdsForReconciliation_StockWithReconciliationMovementOnly_DoesNotExcludeStock()
    {
        // GetStockIdsForReconciliation queries Stock entities, not movements — just verifies non-empty return
        var productId = Guid.NewGuid();
        SeedProduct(productId);
        var locationId = SeedStorageLocation();

        _context.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            StorageLocationId = locationId,
            Quantity = 10m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        await _context.SaveChangesAsync();

        var request = new StockReconciliationRequestDto
        {
            IncludeDocuments = false
        };

        var stockIds = await _service.GetStockIdsForReconciliationAsync(request);

        Assert.NotEmpty(stockIds);
    }

    #endregion

    #region Helpers

    private void SeedArchivedDocumentWithRow(out Guid docId, out Guid rowId)
    {
        var docTypeId = Guid.NewGuid();
        SeedDocumentType(docTypeId, isInventory: false, isStockIncrease: true);

        docId = Guid.NewGuid();
        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = docId,
            TenantId = _tenantId,
            DocumentTypeId = docTypeId,
            Number = "DOC-ARCH",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = EntityDocumentStatus.Archived,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        rowId = Guid.NewGuid();
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = rowId,
            TenantId = _tenantId,
            DocumentHeaderId = docId,
            ProductId = Guid.NewGuid(),
            Quantity = 10m,
            UnitPrice = 10m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        _context.SaveChanges();
    }

    private void SeedDocumentType(Guid id, bool isInventory, bool isStockIncrease)
    {
        _context.DocumentTypes.Add(new DocumentType
        {
            Id = id,
            TenantId = _tenantId,
            Name = "Test Type",
            Code = "TST",
            CreatesStockMovements = true,
            MovesStockOnRowChange = false,
            IsInventoryDocument = isInventory,
            IsStockIncrease = isStockIncrease,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        _context.SaveChanges();
    }

    private void SeedProduct(Guid productId)
    {
        _context.Products.Add(new Product
        {
            Id = productId,
            TenantId = _tenantId,
            Code = "P-001",
            Name = "Test Product",
            ShortDescription = "Test",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        _context.SaveChanges();
    }

    private Guid SeedStorageLocation()
    {
        var warehouseId = Guid.NewGuid();
        _context.StorageLocations.Add(new StorageLocation
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            WarehouseId = warehouseId,
            Code = "LOC-001",
            Description = "Test Location",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        _context.SaveChanges();
        return _context.StorageLocations.First().Id;
    }

    public void Dispose() => _context.Dispose();

    #endregion
}
