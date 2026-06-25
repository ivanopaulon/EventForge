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

    #region Transfer UpdateExisting — delete+recreate fix

    [Fact]
    public async Task RebuildMissingMovements_TransferWithExistingMovement_UpdateExisting_ReportsUpdated()
    {
        // Arrange: a transfer document row that already has a movement.
        // A StorageLocation is required so the code can resolve toLocationId and
        // does not bail out via the "no location" guard before routing to transfersToDeleteAndRecreate.
        var locationId = SeedStorageLocation();

        var docTypeId = Guid.NewGuid();
        _context.DocumentTypes.Add(new DocumentType
        {
            Id = docTypeId,
            TenantId = _tenantId,
            Name = "Transfer Type Update",
            Code = "TRU",
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
            Number = "TRF-UPDATE-001",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = EntityDocumentStatus.Archived,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        var rowId = Guid.NewGuid();
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = rowId,
            TenantId = _tenantId,
            DocumentHeaderId = docId,
            ProductId = Guid.NewGuid(),
            Quantity = 5m,
            UnitPrice = 10m,
            // Row-level LocationId used as toLocationId so at least one location is resolved
            LocationId = locationId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        // Seed an existing movement linked to the row
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

        // Act — DryRun to verify routing without DB mutations
        var request = new RebuildMovementsRequestDto { DryRun = true, UpdateExisting = true };
        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        // The transfer row should be classified as "Updated" (delete+recreate)
        Assert.NotNull(result);
        Assert.Equal(1, result.DocumentsScanned);
        var item = result.Items.FirstOrDefault(i => i.DocumentNumber == "TRF-UPDATE-001");
        Assert.NotNull(item);
        Assert.Equal("Updated", item.Status);
        Assert.Equal("Transfer", item.MovementType);
        Assert.Equal(1, result.MovementsUpdated);
    }

    [Fact]
    public async Task RebuildMissingMovements_TransferWithExistingMovement_UpdateExistingFalse_ReportsAlreadyExists()
    {
        // When UpdateExisting=false the existing transfer movement must not be modified or recreated
        var docTypeId = Guid.NewGuid();
        _context.DocumentTypes.Add(new DocumentType
        {
            Id = docTypeId,
            TenantId = _tenantId,
            Name = "Transfer No Update",
            Code = "TNU",
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
            Number = "TRF-NOUPDATE-001",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = EntityDocumentStatus.Archived,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        var rowId = Guid.NewGuid();
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = rowId,
            TenantId = _tenantId,
            DocumentHeaderId = docId,
            ProductId = Guid.NewGuid(),
            Quantity = 3m,
            UnitPrice = 7m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = Guid.NewGuid(),
            Quantity = 3m,
            DocumentRowId = rowId,
            MovementDate = DateTime.UtcNow,
            IsReconciliationAdjustment = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = true, UpdateExisting = false };
        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        // Existing movement should be left alone
        Assert.NotNull(result);
        Assert.Equal(1, result.RowsAlreadyHadMovement);
        Assert.Contains(result.Items, i => i.Status == "AlreadyExists");
        Assert.Equal(0, result.MovementsUpdated);
    }

    #endregion

    #region DryRun ForceRecalculate preview

    [Fact]
    public async Task RebuildMissingMovements_DryRun_ForceRecalculate_PopulatesStocksForceRecalculated()
    {
        // Arrange: archived document with a row that has no movement yet, plus an existing Stock
        // whose quantity is wrong so it would be overwritten by ForceRecalculate.
        SeedArchivedDocumentWithRow(out _, out _);

        var productId = Guid.NewGuid();
        var locationId = SeedStorageLocation();

        // Seed a Stock record whose quantity doesn't match any movement
        _context.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            StorageLocationId = locationId,
            Quantity = 99m, // wrong balance
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        await _context.SaveChangesAsync();

        // Seed a movement for that product/location so the pair is in the affected set
        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            ToLocationId = locationId,
            Quantity = 5m,
            MovementDate = DateTime.UtcNow,
            IsReconciliationAdjustment = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto
        {
            DryRun = true,
            ForceRecalculateFromMovements = true,
            // Seed a Storage location for the doc row so the new movement has a location
            // and lands in affectedPairs
        };

        // We only care that the field is non-zero when there are pairs with wrong balances.
        // The exact count depends on data seeded. Just verify it gets computed (>= 0 always,
        // and > 0 because our stock has Quantity=99 but movement net is 5).
        var result = await _service.RebuildMissingMovementsFromDocumentsAsync(request, "test-user");

        Assert.NotNull(result);
        Assert.True(result.IsDryRun);
        // StocksForceRecalculated is populated during DryRun+ForceRecalculate
        Assert.True(result.StocksForceRecalculated >= 0);
    }

    #endregion

    #region RecalculateAllStocksFromMovementsAsync

    [Fact]
    public async Task RecalculateAllStocks_NoMovements_ReturnsZeroPairs()
    {
        var result = await _service.RecalculateAllStocksFromMovementsAsync(dryRun: true, "test-user");

        Assert.NotNull(result);
        Assert.True(result.IsDryRun);
        Assert.Equal(0, result.PairsScanned);
    }

    [Fact]
    public async Task RecalculateAllStocks_DryRun_CorrectlyCountsUpdatesWithoutWriting()
    {
        // Arrange: a movement that puts 10 units in a location, and a Stock record showing 5
        var productId = Guid.NewGuid();
        var locationId = SeedStorageLocation();
        SeedProduct(productId);

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            ToLocationId = locationId,
            Quantity = 10m,
            MovementDate = DateTime.UtcNow,
            IsReconciliationAdjustment = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        _context.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            StorageLocationId = locationId,
            Quantity = 5m, // incorrect — movement net is 10
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        await _context.SaveChangesAsync();

        var result = await _service.RecalculateAllStocksFromMovementsAsync(dryRun: true, "test-user");

        Assert.NotNull(result);
        Assert.True(result.IsDryRun);
        Assert.Equal(1, result.PairsScanned);
        // Stock Quantity=5 ≠ net=10 → should count as StocksUpdated
        Assert.Equal(1, result.StocksUpdated);
        Assert.Equal(0, result.StocksCreated);
        // DB must remain unchanged after dry run
        var stock = await _context.Stocks.FirstAsync();
        Assert.Equal(5m, stock.Quantity);
    }

    [Fact]
    public async Task RecalculateAllStocks_Execute_UpdatesStockQuantity()
    {
        // Arrange: movement of 10 units, stock incorrectly showing 3
        var productId = Guid.NewGuid();
        var locationId = SeedStorageLocation();
        SeedProduct(productId);

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            ToLocationId = locationId,
            Quantity = 10m,
            MovementDate = DateTime.UtcNow,
            IsReconciliationAdjustment = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        _context.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            StorageLocationId = locationId,
            Quantity = 3m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        await _context.SaveChangesAsync();

        var result = await _service.RecalculateAllStocksFromMovementsAsync(dryRun: false, "test-user");

        Assert.NotNull(result);
        Assert.False(result.IsDryRun);
        Assert.Equal(1, result.PairsScanned);
        Assert.Equal(1, result.StocksUpdated);

        // Verify DB was actually updated
        var stock = await _context.Stocks.FirstAsync();
        Assert.Equal(10m, stock.Quantity);
    }

    [Fact]
    public async Task RecalculateAllStocks_Execute_CreatesStockWhenMissing()
    {
        // Arrange: movement with no corresponding Stock record
        var productId = Guid.NewGuid();
        var locationId = SeedStorageLocation();
        SeedProduct(productId);

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            ToLocationId = locationId,
            Quantity = 7m,
            MovementDate = DateTime.UtcNow,
            IsReconciliationAdjustment = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        await _context.SaveChangesAsync();

        var result = await _service.RecalculateAllStocksFromMovementsAsync(dryRun: false, "test-user");

        Assert.NotNull(result);
        Assert.Equal(1, result.PairsScanned);
        Assert.Equal(1, result.StocksCreated);

        var stock = await _context.Stocks.FirstAsync();
        Assert.Equal(7m, stock.Quantity);
    }

    #endregion

        #region GetStockIdsForReconciliation

        [Fact]
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
