using Prym.DTOs.Warehouse;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Warehouse;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Warehouse;

/// <summary>
/// Unit tests for StockService.GetStockSnapshotAsync.
/// Uses EF Core InMemory provider; warehouse-level DB filtering is verified through
/// the locationId path (which translates directly to scalar FK comparisons).
/// </summary>
[Trait("Category", "Unit")]
public class StockSnapshotServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<StockService>> _mockLogger;
    private readonly StockService _stockService;

    private readonly Guid _tenantId = Guid.NewGuid();

    // Shared entities
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();
    private readonly Guid _location2Id = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _product2Id = Guid.NewGuid();

    public StockSnapshotServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<StockService>>();

        _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        _stockService = new StockService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);

        SeedBaseEntities();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void SeedBaseEntities()
    {
        _context.StorageFacilities.Add(new StorageFacility
        {
            Id = _warehouseId,
            TenantId = _tenantId,
            Name = "Main Warehouse",
            Code = "WH01",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.StorageLocations.AddRange(
            new StorageLocation
            {
                Id = _locationId,
                TenantId = _tenantId,
                WarehouseId = _warehouseId,
                Code = "LOC-A",
                Description = "Location A",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            },
            new StorageLocation
            {
                Id = _location2Id,
                TenantId = _tenantId,
                WarehouseId = _warehouseId,
                Code = "LOC-B",
                Description = "Location B",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            });

        _context.Products.AddRange(
            new Product
            {
                Id = _productId,
                TenantId = _tenantId,
                Code = "PROD-001",
                Name = "Alpha Product",
                DefaultPrice = 19.99m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            },
            new Product
            {
                Id = _product2Id,
                TenantId = _tenantId,
                Code = "PROD-002",
                Name = "Beta Product",
                DefaultPrice = 9.50m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            });

        _context.SaveChanges();
    }

    private StockMovement MakeMovement(
        Guid productId,
        Guid? toLocationId,
        Guid? fromLocationId,
        decimal quantity,
        DateTime date,
        decimal? unitCost = null,
        MovementStatus status = MovementStatus.Completed,
        Guid? lotId = null)
    {
        return new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            ToLocationId = toLocationId,
            FromLocationId = fromLocationId,
            LotId = lotId,
            Quantity = quantity,
            UnitCost = unitCost,
            MovementDate = date,
            MovementType = toLocationId.HasValue && fromLocationId.HasValue
                ? StockMovementType.Transfer
                : toLocationId.HasValue ? StockMovementType.Inbound : StockMovementType.Outbound,
            Status = status,
            Reason = StockMovementReason.Purchase,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    private static readonly DateTime RefDate = new DateTime(2025, 6, 15);

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Empty result when no movements exist
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_NoMovements_ReturnsEmpty()
    {
        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();
        Assert.Empty(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Single inbound movement produces correct quantity
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_SingleInbound_CorrectQuantity()
    {
        _context.StockMovements.Add(MakeMovement(_productId, _locationId, null, 50m, RefDate.AddDays(-5)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(50m, dto.Quantity);
        Assert.Equal(_productId, dto.ProductId);
        Assert.Equal("PROD-001", dto.ProductCode);
        Assert.Equal("Alpha Product", dto.ProductName);
        Assert.Equal(RefDate.Date, dto.ReferenceDate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Inbound then outbound produces net quantity
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_InboundThenOutbound_NetQuantity()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 100m, RefDate.AddDays(-10)),
            MakeMovement(_productId, null, _locationId, 30m,  RefDate.AddDays(-3)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(70m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Movements AFTER referenceDate are excluded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_MovementAfterReferenceDate_Excluded()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 100m, RefDate.AddDays(-1)),
            MakeMovement(_productId, _locationId, null, 999m, RefDate.AddDays(1))); // future
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(100m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Movements on the exact reference date (same day, end of day) are included
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_MovementOnReferenceDate_Included()
    {
        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 77m, RefDate.Date.AddHours(23).AddMinutes(59)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(77m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Cancelled movements are excluded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_CancelledMovements_Excluded()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 40m, RefDate.AddDays(-5), status: MovementStatus.Completed),
            MakeMovement(_productId, _locationId, null, 999m, RefDate.AddDays(-4), status: MovementStatus.Cancelled),
            MakeMovement(_productId, _locationId, null, 888m, RefDate.AddDays(-3), status: MovementStatus.Failed));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(40m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. Transfer movement: decrements source, increments destination
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_TransferMovement_DecrementsSrcIncrementsDst()
    {
        // Seed location A with 100 units first
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 100m, RefDate.AddDays(-10)),
            MakeMovement(_productId, _location2Id, _locationId, 30m, RefDate.AddDays(-5)));  // transfer 30 from A→B
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate))
            .OrderBy(r => r.LocationCode)
            .ToList();

        Assert.Equal(2, result.Count);
        var locA = result.First(r => r.LocationId == _locationId);
        var locB = result.First(r => r.LocationId == _location2Id);
        Assert.Equal(70m, locA.Quantity);  // 100 - 30
        Assert.Equal(30m, locB.Quantity);  // 0  + 30
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. Location filter restricts results
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_LocationFilter_RestrictsResults()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId,  null, 80m, RefDate.AddDays(-5)),
            MakeMovement(_productId, _location2Id, null, 20m, RefDate.AddDays(-5)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate, locationId: _locationId)).ToList();

        Assert.Single(result);
        Assert.Equal(_locationId, result[0].LocationId);
        Assert.Equal(80m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9. Search filter matches product name and code (case-insensitive)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_SearchByName_ReturnsMatchOnly()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_productId,  _locationId, null, 10m, RefDate.AddDays(-1)),  // Alpha
            MakeMovement(_product2Id, _locationId, null, 20m, RefDate.AddDays(-1))); // Beta
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate, searchTerm: "alpha")).ToList();

        Assert.Single(result);
        Assert.Equal("PROD-001", result[0].ProductCode);
    }

    [Fact]
    public async Task GetStockSnapshotAsync_SearchByCode_ReturnsMatchOnly()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_productId,  _locationId, null, 10m, RefDate.AddDays(-1)),
            MakeMovement(_product2Id, _locationId, null, 20m, RefDate.AddDays(-1)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate, searchTerm: "PROD-002")).ToList();

        Assert.Single(result);
        Assert.Equal("PROD-002", result[0].ProductCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 10. UnitCost from last inbound movement
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_UnitCost_LastInboundWins()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 50m, RefDate.AddDays(-10), unitCost: 5.00m),
            MakeMovement(_productId, _locationId, null, 50m, RefDate.AddDays(-5),  unitCost: 7.50m)); // later → wins
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(7.50m, result[0].UnitCost);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 11. UnitCost fallback to Stock.UnitCost when no inbound has a cost
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_UnitCost_FallsBackToStockUnitCost()
    {
        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 10m, RefDate.AddDays(-2), unitCost: null));
        _context.Stocks.Add(new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = _productId,
            StorageLocationId = _locationId,
            Quantity = 10m,
            UnitCost = 3.33m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(3.33m, result[0].UnitCost);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 12. TotalCostValue and TotalSaleValue are correctly computed
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_TotalValues_ComputedCorrectly()
    {
        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 10m, RefDate.AddDays(-1), unitCost: 5.00m));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(50.00m, dto.TotalCostValue);          // 10 * 5
        Assert.Equal(10m * 19.99m, dto.TotalSaleValue);    // 10 * DefaultPrice
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 13. TotalCostValue is computed even when quantity is negative (oversold)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_NegativeQuantity_TotalValuesStillComputed()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 5m,  RefDate.AddDays(-5), unitCost: 10.00m),
            MakeMovement(_productId, null, _locationId, 20m, RefDate.AddDays(-2))); // oversold
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(-15m, dto.Quantity);
        Assert.Equal(-150m, dto.TotalCostValue);  // -15 * 10
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 14. DefaultPrice comes from Product.DefaultPrice
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_DefaultPrice_FromProduct()
    {
        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 1m, RefDate.AddDays(-1)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(19.99m, result[0].DefaultPrice);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 15. Movements from other tenants are excluded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_OtherTenantMovements_Excluded()
    {
        var otherTenantId = Guid.NewGuid();
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 100m, RefDate.AddDays(-1)), // own tenant
            new StockMovement // different tenant
            {
                Id = Guid.NewGuid(),
                TenantId = otherTenantId,
                ProductId = _productId,
                ToLocationId = _locationId,
                Quantity = 999m,
                MovementDate = RefDate.AddDays(-1),
                MovementType = StockMovementType.Inbound,
                Status = MovementStatus.Completed,
                Reason = StockMovementReason.Purchase,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "other"
            });
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(100m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 16. Results are ordered by ProductCode then LocationCode
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_OrderedByProductCodeThenLocationCode()
    {
        _context.StockMovements.AddRange(
            MakeMovement(_product2Id, _locationId,  null, 1m, RefDate.AddDays(-1)), // PROD-002 LOC-A
            MakeMovement(_productId,  _location2Id, null, 1m, RefDate.AddDays(-1)), // PROD-001 LOC-B
            MakeMovement(_productId,  _locationId,  null, 1m, RefDate.AddDays(-1)), // PROD-001 LOC-A
            MakeMovement(_product2Id, _location2Id, null, 1m, RefDate.AddDays(-1)));// PROD-002 LOC-B
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Equal(4, result.Count);
        Assert.Equal("PROD-001", result[0].ProductCode); Assert.Equal("LOC-A", result[0].LocationCode);
        Assert.Equal("PROD-001", result[1].ProductCode); Assert.Equal("LOC-B", result[1].LocationCode);
        Assert.Equal("PROD-002", result[2].ProductCode); Assert.Equal("LOC-A", result[2].LocationCode);
        Assert.Equal("PROD-002", result[3].ProductCode); Assert.Equal("LOC-B", result[3].LocationCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 17. Lot grouping: same product / location with two different lots → two rows
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_LotGrouping_TwoLotsProduceTwoRows()
    {
        var lot1 = Guid.NewGuid();
        var lot2 = Guid.NewGuid();
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 10m, RefDate.AddDays(-5), lotId: lot1),
            MakeMovement(_productId, _locationId, null, 20m, RefDate.AddDays(-5), lotId: lot2));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Equal(2, result.Count);
        var quantities = result.Select(r => r.Quantity).OrderBy(q => q).ToList();
        Assert.Equal(new[] { 10m, 20m }, quantities);
    }

    public void Dispose() => _context?.Dispose();
}
