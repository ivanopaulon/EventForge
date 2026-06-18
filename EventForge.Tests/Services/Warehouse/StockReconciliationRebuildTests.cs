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

namespace EventForge.Tests.Services.Warehouse;

/// <summary>
/// Unit tests for StockReconciliationService.RebuildMissingMovementsFromDocumentsAsync
/// covering: OR→AND status filter, deterministic location resolution, UpdateExisting behavior.
/// </summary>
[Trait("Category", "Unit")]
public class StockReconciliationRebuildTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly StockReconciliationService _reconciliationService;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _locationAId = Guid.NewGuid();  // Code = "A"
    private readonly Guid _locationBId = Guid.NewGuid();  // Code = "B"
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _documentTypeInId = Guid.NewGuid(); // IsStockIncrease = true
    private readonly Guid _documentTypeOutId = Guid.NewGuid(); // IsStockIncrease = false
    private readonly Guid _documentTypeInvId = Guid.NewGuid(); // IsInventoryDocument = true

    public StockReconciliationRebuildTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        var mockAudit = new Mock<IAuditLogService>();
        var mockTenant = new Mock<ITenantContext>();
        var mockSmLogger = new Mock<ILogger<StockMovementService>>();
        var mockLogger = new Mock<ILogger<StockReconciliationService>>();

        mockTenant.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        var stockMovementService = new StockMovementService(
            _context,
            mockAudit.Object,
            mockTenant.Object,
            mockSmLogger.Object);

        _reconciliationService = new StockReconciliationService(
            _context,
            mockAudit.Object,
            stockMovementService,
            mockTenant.Object,
            mockLogger.Object);

        SeedBaseData();
    }

    private void SeedBaseData()
    {
        _context.StorageFacilities.Add(new StorageFacility
        {
            Id = _warehouseId,
            TenantId = _tenantId,
            Name = "WH",
            Code = "WH",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        // Two locations — Code "A" sorts before "B" alphabetically
        _context.StorageLocations.AddRange(
            new StorageLocation { Id = _locationAId, TenantId = _tenantId, WarehouseId = _warehouseId, Code = "A", CreatedAt = DateTime.UtcNow, CreatedBy = "test" },
            new StorageLocation { Id = _locationBId, TenantId = _tenantId, WarehouseId = _warehouseId, Code = "B", CreatedAt = DateTime.UtcNow, CreatedBy = "test" });

        _context.Products.Add(new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Code = "P01",
            Name = "Product",
            DefaultPrice = 10m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.DocumentTypes.AddRange(
            new DocumentType
            {
                Id = _documentTypeInId,
                TenantId = _tenantId,
                Name = "Acquisto",
                Code = "ACQ",
                IsStockIncrease = true,
                DefaultWarehouseId = _warehouseId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            },
            new DocumentType
            {
                Id = _documentTypeOutId,
                TenantId = _tenantId,
                Name = "Vendita",
                Code = "VEN",
                IsStockIncrease = false,
                DefaultWarehouseId = _warehouseId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            },
            new DocumentType
            {
                Id = _documentTypeInvId,
                TenantId = _tenantId,
                Name = "Inventario",
                Code = "INV",
                IsStockIncrease = true,
                IsInventoryDocument = true,
                DefaultWarehouseId = _warehouseId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            });

        // Initial stock at location A
        _context.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = _productId,
            StorageLocationId = _locationAId,
            Quantity = 100m,
            ReservedQuantity = 0m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.SaveChanges();
    }

    private DocumentHeader MakeClosedApprovedHeader(Guid documentTypeId, string number)
        => new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = documentTypeId,
            Number = number,
            Date = DateTime.UtcNow,
            Status = Prym.DTOs.Common.DocumentStatus.Archived,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

    private DocumentRow MakeRow(Guid documentHeaderId, Guid? locationId = null)
        => new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = documentHeaderId,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 10m,
            UnitPrice = 5m,
            LocationId = locationId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1: AND filter — document Approved-but-Open must be excluded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RebuildMovements_ApprovedButOpenDocument_ExcludedByAndFilter()
    {
        // Approved but still Open (not Closed) → should NOT generate movements
        var header = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = _documentTypeInId,
            Number = "DOC-OPEN",
            Date = DateTime.UtcNow,
            Status = Prym.DTOs.Common.DocumentStatus.Active,        // Open
            ApprovalStatus = ApprovalStatus.Approved,             // Approved
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(header);
        _context.DocumentRows.Add(MakeRow(header.Id, _locationAId));
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = true };
        var result = await _reconciliationService.RebuildMissingMovementsFromDocumentsAsync(request, "test");

        // Default filter: Approved + Closed (AND).  Open document must be excluded.
        Assert.Equal(0, result.DocumentsScanned);
        Assert.Equal(0, result.RowsScanned);
        Assert.Equal(0, result.MovementsCreated);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2: AND filter — document Closed-but-not-Approved must be excluded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RebuildMovements_ClosedButNotApprovedDocument_ExcludedByAndFilter()
    {
        var header = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = _documentTypeInId,
            Number = "DOC-NOTAPPROVED",
            Date = DateTime.UtcNow,
            Status = Prym.DTOs.Common.DocumentStatus.Archived,     // Closed
            ApprovalStatus = ApprovalStatus.None,                 // NOT approved
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(header);
        _context.DocumentRows.Add(MakeRow(header.Id, _locationAId));
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = true };
        var result = await _reconciliationService.RebuildMissingMovementsFromDocumentsAsync(request, "test");

        Assert.Equal(0, result.DocumentsScanned);
        Assert.Equal(0, result.MovementsCreated);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3: AND filter — Approved+Closed document IS included
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RebuildMovements_ApprovedAndClosedDocument_IsIncluded()
    {
        var header = MakeClosedApprovedHeader(_documentTypeInId, "DOC-OK");
        _context.DocumentHeaders.Add(header);
        _context.DocumentRows.Add(MakeRow(header.Id, _locationAId));
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = true };
        var result = await _reconciliationService.RebuildMissingMovementsFromDocumentsAsync(request, "test");

        Assert.Equal(1, result.DocumentsScanned);
        Assert.Equal(1, result.RowsScanned);
        Assert.Equal(1, result.MovementsCreated);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 4: Deterministic location resolution — row without LocationId should
    //         always resolve to the location with the lowest Code ("A" < "B").
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RebuildMovements_RowWithoutLocationId_ResolvesToLowestCodeLocation()
    {
        // Row has no explicit LocationId → service should resolve via warehouse fallback
        var header = MakeClosedApprovedHeader(_documentTypeInId, "DOC-NOLOC");
        header.DestinationWarehouseId = _warehouseId;
        _context.DocumentHeaders.Add(header);
        _context.DocumentRows.Add(MakeRow(header.Id, locationId: null)); // no location
        await _context.SaveChangesAsync();

        var request = new RebuildMovementsRequestDto { DryRun = false };
        var result = await _reconciliationService.RebuildMissingMovementsFromDocumentsAsync(request, "test");

        Assert.Equal(1, result.MovementsCreated);

        // The created movement should be at location "A" (lowest Code)
        var movement = await _context.StockMovements
            .FirstOrDefaultAsync(sm => sm.ProductId == _productId && !sm.IsDeleted);
        Assert.NotNull(movement);
        Assert.Equal(_locationAId, movement.ToLocationId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 5: UpdateExisting=true updates an already-existing movement rather
    //         than creating a duplicate
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RebuildMovements_UpdateExisting_UpdatesMovementNotDuplicate()
    {
        var header = MakeClosedApprovedHeader(_documentTypeInId, "DOC-UPD");
        _context.DocumentHeaders.Add(header);
        var row = MakeRow(header.Id, _locationAId);
        _context.DocumentRows.Add(row);
        await _context.SaveChangesAsync();

        // First rebuild — creates the movement
        var firstRequest = new RebuildMovementsRequestDto { DryRun = false };
        var firstResult = await _reconciliationService.RebuildMissingMovementsFromDocumentsAsync(firstRequest, "test");
        Assert.Equal(1, firstResult.MovementsCreated);
        Assert.Equal(0, firstResult.MovementsUpdated);

        // Change the row quantity so an update is needed
        var dbRow = await _context.DocumentRows.FindAsync(row.Id);
        Assert.NotNull(dbRow);
        dbRow.Quantity = 25m;
        await _context.SaveChangesAsync();

        // Second rebuild with UpdateExisting=true — should UPDATE, not create a new movement
        var secondRequest = new RebuildMovementsRequestDto { DryRun = false, UpdateExisting = true };
        var secondResult = await _reconciliationService.RebuildMissingMovementsFromDocumentsAsync(secondRequest, "test");

        Assert.Equal(0, secondResult.MovementsCreated);
        Assert.Equal(1, secondResult.MovementsUpdated);

        // Exactly one non-deleted movement should exist
        var movements = await _context.StockMovements
            .Where(sm => sm.DocumentRowId == row.Id && !sm.IsDeleted)
            .ToListAsync();
        Assert.Single(movements);
        // Quantity should have been updated to match the new BaseQuantity ?? Quantity
        Assert.Equal(25m, movements[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 6: Reason is correctly set for inbound (Purchase) and outbound (Sale)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RebuildMovements_InboundDocument_ReasonIsPurchase()
    {
        var header = MakeClosedApprovedHeader(_documentTypeInId, "DOC-IN-REASON");
        _context.DocumentHeaders.Add(header);
        _context.DocumentRows.Add(MakeRow(header.Id, _locationAId));
        await _context.SaveChangesAsync();

        await _reconciliationService.RebuildMissingMovementsFromDocumentsAsync(
            new RebuildMovementsRequestDto { DryRun = false }, "test");

        var movement = await _context.StockMovements
            .FirstOrDefaultAsync(sm => sm.ProductId == _productId && !sm.IsDeleted);
        Assert.NotNull(movement);
        Assert.Equal(StockMovementReason.Purchase, movement.Reason);
    }

    [Fact]
    public async Task RebuildMovements_OutboundDocument_ReasonIsSale()
    {
        var header = MakeClosedApprovedHeader(_documentTypeOutId, "DOC-OUT-REASON");
        _context.DocumentHeaders.Add(header);
        _context.DocumentRows.Add(MakeRow(header.Id, _locationAId));
        await _context.SaveChangesAsync();

        await _reconciliationService.RebuildMissingMovementsFromDocumentsAsync(
            new RebuildMovementsRequestDto { DryRun = false }, "test");

        var movement = await _context.StockMovements
            .FirstOrDefaultAsync(sm => sm.ProductId == _productId && !sm.IsDeleted);
        Assert.NotNull(movement);
        Assert.Equal(StockMovementReason.Sale, movement.Reason);
    }

    [Fact]
    public async Task CalculateReconciliation_ExcludesTechnicalReconciliationAdjustments()
    {
        var header = MakeClosedApprovedHeader(_documentTypeInId, "DOC-REC-EXCL");
        _context.DocumentHeaders.Add(header);
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = header.Id,
            ProductId = _productId,
            Description = "Reconciliation exclusion test",
            Quantity = 100m,
            UnitPrice = 5m,
            LocationId = _locationAId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            MovementType = StockMovementType.Adjustment,
            ProductId = _productId,
            FromLocationId = _locationAId,
            Quantity = 20m,
            Reason = StockMovementReason.Adjustment,
            Notes = "Stock Reconciliation - legacy technical adjustment",
            IsReconciliationAdjustment = true,
            ReconciliationRunId = Guid.NewGuid(),
            Status = MovementStatus.Completed,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await _context.SaveChangesAsync();

        var result = await _reconciliationService.CalculateReconciledStockAsync(new StockReconciliationRequestDto
        {
            ProductId = _productId,
            LocationId = _locationAId,
            IncludeInventories = false,
            IncludeDocuments = true,
            IncludeStockMovements = true
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(100m, item.CurrentQuantity);
        Assert.Equal(100m, item.CalculatedQuantity);
        Assert.Equal(0m, item.Difference);
        Assert.Equal(0, item.TotalManualMovements);
    }

    [Fact]
    public async Task CalculateReconciliation_IncludesNonTechnicalManualAdjustments()
    {
        var header = MakeClosedApprovedHeader(_documentTypeInId, "DOC-REC-INCL");
        _context.DocumentHeaders.Add(header);
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = header.Id,
            ProductId = _productId,
            Description = "Reconciliation include test",
            Quantity = 100m,
            UnitPrice = 5m,
            LocationId = _locationAId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            MovementType = StockMovementType.Adjustment,
            ProductId = _productId,
            FromLocationId = _locationAId,
            Quantity = 20m,
            Reason = StockMovementReason.Adjustment,
            Notes = "Manual adjustment for spoilage",
            IsReconciliationAdjustment = false,
            Status = MovementStatus.Completed,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await _context.SaveChangesAsync();

        var result = await _reconciliationService.CalculateReconciledStockAsync(new StockReconciliationRequestDto
        {
            ProductId = _productId,
            LocationId = _locationAId,
            IncludeInventories = false,
            IncludeDocuments = true,
            IncludeStockMovements = true
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(100m, item.CurrentQuantity);
        Assert.Equal(80m, item.CalculatedQuantity);
        Assert.Equal(-20m, item.Difference);
        Assert.Equal(1, item.TotalManualMovements);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test: Fix A — inventory row with null LocationId matched via warehouse fallback
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateReconciliation_InventoryWithNullLocationId_MatchedViaWarehouseFallback()
    {
        // Arrange: inventory document header references the warehouse; row has no LocationId (legacy data).
        var invHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = _documentTypeInvId,
            Number = "INV-NOLOC",
            Date = DateTime.UtcNow.AddDays(-30),
            Status = Prym.DTOs.Common.DocumentStatus.Archived,
            ApprovalStatus = ApprovalStatus.Approved,
            SourceWarehouseId = _warehouseId,   // warehouse set on header, not on row
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(invHeader);

        _context.DocumentRows.Add(new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = invHeader.Id,
            ProductId = _productId,
            Description = "Inventario legacy",
            Quantity = 50m,     // inventory snapshot: 50 units
            UnitPrice = 0m,
            LocationId = null,  // null — the legacy scenario we're fixing
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _reconciliationService.CalculateReconciledStockAsync(new StockReconciliationRequestDto
        {
            ProductId = _productId,
            LocationId = _locationAId,
            IncludeInventories = true,
            IncludeDocuments = false,
            IncludeStockMovements = false
        });

        // Assert: the inventory snapshot (50) should have been picked up via the warehouse fallback
        var item = Assert.Single(result.Items);
        Assert.Equal(50m, item.CalculatedQuantity);
        Assert.Equal(1, item.TotalInventories);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test: Fix C — closed inventory document with ApprovalStatus=None is excluded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateReconciliation_ClosedInventoryWithApprovalStatusNone_IsExcluded()
    {
        // Arrange: an inventory document that is Closed but NOT approved (ApprovalStatus=None).
        // The reconciliation query requires both Status==Closed AND ApprovalStatus==Approved,
        // so this document should contribute 0 to CalculatedQuantity.
        var invHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = _documentTypeInvId,
            Number = "INV-NOTAPPROVED",
            Date = DateTime.UtcNow.AddDays(-10),
            Status = Prym.DTOs.Common.DocumentStatus.Archived,
            ApprovalStatus = ApprovalStatus.None,   // NOT approved
            SourceWarehouseId = _warehouseId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(invHeader);

        _context.DocumentRows.Add(new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = invHeader.Id,
            ProductId = _productId,
            Description = "Inventario non approvato",
            Quantity = 999m,    // should never be counted
            UnitPrice = 0m,
            LocationId = _locationAId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _reconciliationService.CalculateReconciledStockAsync(new StockReconciliationRequestDto
        {
            ProductId = _productId,
            LocationId = _locationAId,
            IncludeInventories = true,
            IncludeDocuments = false,
            IncludeStockMovements = false
        });

        // Assert: the unapproved inventory snapshot should be ignored.
        var item = Assert.Single(result.Items);
        Assert.Equal(0m, item.CalculatedQuantity);
        Assert.Equal(0, item.TotalInventories);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test: Fix B — legacy reconciliation movement excluded by Reference fallback
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateReconciliation_LegacyReconciliationMovementWithReferenceText_IsExcluded()
    {
        // Arrange: a movement with IsReconciliationAdjustment=false (legacy, pre-flag) but
        // whose Reference contains "Stock Reconciliation" — should be excluded.
        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            MovementType = StockMovementType.Adjustment,
            ProductId = _productId,
            FromLocationId = _locationAId,
            Quantity = 30m,
            Reference = "Stock Reconciliation 2023-01",   // legacy reference text
            Reason = StockMovementReason.Adjustment,
            IsReconciliationAdjustment = false,           // flag not set (pre-flag era)
            Status = MovementStatus.Completed,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _reconciliationService.CalculateReconciledStockAsync(new StockReconciliationRequestDto
        {
            ProductId = _productId,
            LocationId = _locationAId,
            IncludeInventories = false,
            IncludeDocuments = false,
            IncludeStockMovements = true
        });

        // Assert: the legacy reconciliation movement should be excluded; no manual movements counted
        var item = Assert.Single(result.Items);
        Assert.Equal(0m, item.CalculatedQuantity);
        Assert.Equal(0, item.TotalManualMovements);
    }

    public void Dispose() => _context?.Dispose();
}
