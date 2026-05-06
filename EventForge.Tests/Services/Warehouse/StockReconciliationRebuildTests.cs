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
            Status = Prym.DTOs.Common.DocumentStatus.Closed,
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
            Status = Prym.DTOs.Common.DocumentStatus.Open,        // Open
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
            Status = Prym.DTOs.Common.DocumentStatus.Closed,     // Closed
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

    public void Dispose() => _context?.Dispose();
}
