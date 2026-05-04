using Prym.DTOs.Warehouse;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Warehouse;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PriceListDirection = Prym.DTOs.Common.PriceListDirection;
using EntityDocumentStatus = Prym.DTOs.Common.DocumentStatus;

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
        Assert.Equal(10m * 19.99m, dto.TotalSaleValue);    // 10 * SalePrice (fallback to DefaultPrice)
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
    // 14. No active price list → SalePrice falls back to Product.DefaultPrice
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_NoActivePriceList_FallsBackToDefaultPrice()
    {
        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 1m, RefDate.AddDays(-1)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(19.99m, result[0].SalePrice);
        Assert.False(result[0].IsPriceFromList);
        Assert.Null(result[0].PriceListName);
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

    // ─────────────────────────────────────────────────────────────────────────
    // 18. Active Output price list valid at referenceDate → SalePrice from list
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_ActiveOutputPriceList_UsesPriceListPrice()
    {
        // Seed price list
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Listino Vendita Test",
            Direction = PriceListDirection.Output,
            Status = PriceListStatus.Active,
            Priority = 0,
            ValidFrom = RefDate.AddDays(-30),
            ValidTo = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var entry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PriceListId = priceList.Id,
            ProductId = _productId,
            Price = 25.00m,
            Status = PriceListEntryStatus.Active,
            MinQuantity = 0,
            MaxQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(entry);

        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 5m, RefDate.AddDays(-1)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(25.00m, result[0].SalePrice);
        Assert.True(result[0].IsPriceFromList);
        Assert.Equal("Listino Vendita Test", result[0].PriceListName);
        Assert.Equal(5m * 25.00m, result[0].TotalSaleValue);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 19. Expired price list (ValidTo < referenceDate) → fallback to DefaultPrice
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_ExpiredPriceList_FallsBackToDefaultPrice()
    {
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Listino Scaduto",
            Direction = PriceListDirection.Output,
            Status = PriceListStatus.Active,
            Priority = 0,
            ValidFrom = RefDate.AddDays(-60),
            ValidTo = RefDate.AddDays(-1),   // expired before referenceDate
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);
        _context.PriceListEntries.Add(new PriceListEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PriceListId = priceList.Id,
            ProductId = _productId,
            Price = 99.99m,
            Status = PriceListEntryStatus.Active,
            MinQuantity = 0,
            MaxQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 2m, RefDate.AddDays(-2)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(19.99m, result[0].SalePrice);   // Product.DefaultPrice
        Assert.False(result[0].IsPriceFromList);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 20. Not-yet-started price list (ValidFrom > referenceDate) → fallback
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_FuturePriceList_FallsBackToDefaultPrice()
    {
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Listino Futuro",
            Direction = PriceListDirection.Output,
            Status = PriceListStatus.Active,
            Priority = 0,
            ValidFrom = RefDate.AddDays(1),   // starts after referenceDate
            ValidTo = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);
        _context.PriceListEntries.Add(new PriceListEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PriceListId = priceList.Id,
            ProductId = _productId,
            Price = 88.00m,
            Status = PriceListEntryStatus.Active,
            MinQuantity = 0,
            MaxQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 3m, RefDate.AddDays(-1)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(19.99m, result[0].SalePrice);
        Assert.False(result[0].IsPriceFromList);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 21. Highest priority (lowest Priority value) price list wins
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_MultiplePriceLists_HighestPriorityWins()
    {
        var pl1 = new PriceList
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, Name = "Listino P10",
            Direction = PriceListDirection.Output, Status = PriceListStatus.Active,
            Priority = 10, ValidFrom = null, ValidTo = null,
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        var pl2 = new PriceList
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, Name = "Listino P1",
            Direction = PriceListDirection.Output, Status = PriceListStatus.Active,
            Priority = 1,  ValidFrom = null, ValidTo = null,  // higher priority
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _context.PriceLists.AddRange(pl1, pl2);

        _context.PriceListEntries.AddRange(
            new PriceListEntry
            {
                Id = Guid.NewGuid(), TenantId = _tenantId, PriceListId = pl1.Id,
                ProductId = _productId, Price = 50.00m,
                Status = PriceListEntryStatus.Active, MinQuantity = 0, MaxQuantity = 0,
                CreatedAt = DateTime.UtcNow, CreatedBy = "test"
            },
            new PriceListEntry
            {
                Id = Guid.NewGuid(), TenantId = _tenantId, PriceListId = pl2.Id,
                ProductId = _productId, Price = 30.00m,  // this should win (Priority=1)
                Status = PriceListEntryStatus.Active, MinQuantity = 0, MaxQuantity = 0,
                CreatedAt = DateTime.UtcNow, CreatedBy = "test"
            });

        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 1m, RefDate.AddDays(-1)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(30.00m, result[0].SalePrice);   // Priority=1 wins over Priority=10
        Assert.Equal("Listino P1", result[0].PriceListName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 22. Input price list (Purchase direction) → not used for sale price
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_InputPriceList_NotUsedForSalePrice()
    {
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, Name = "Listino Acquisto",
            Direction = PriceListDirection.Input,   // wrong direction for sale price
            Status = PriceListStatus.Active,
            Priority = 0, ValidFrom = null, ValidTo = null,
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);
        _context.PriceListEntries.Add(new PriceListEntry
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, PriceListId = priceList.Id,
            ProductId = _productId, Price = 77.00m,
            Status = PriceListEntryStatus.Active, MinQuantity = 0, MaxQuantity = 0,
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });

        _context.StockMovements.Add(
            MakeMovement(_productId, _locationId, null, 1m, RefDate.AddDays(-1)));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(19.99m, result[0].SalePrice);   // fallback: Input list not used
        Assert.False(result[0].IsPriceFromList);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Inventory-document anchor tests
    // ─────────────────────────────────────────────────────────────────────────

    private Guid _inventoryDocTypeId = Guid.NewGuid();

    private void SeedInventoryDocumentType()
    {
        _context.DocumentTypes.Add(new DocumentType
        {
            Id = _inventoryDocTypeId,
            TenantId = _tenantId,
            Code = "INVENTORY",
            Name = "Inventory Document",
            IsInventoryDocument = true,
            CreatesStockMovements = false,
            IsStockIncrease = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
    }

    private DocumentHeader MakeInventoryHeader(
        DateTime date,
        EntityDocumentStatus status = EntityDocumentStatus.Closed)
    {
        return new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = _inventoryDocTypeId,
            Number = $"INV-{Guid.NewGuid():N}",
            Date = date,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    private DocumentRow MakeInventoryRow(
        Guid documentHeaderId,
        Guid productId,
        Guid locationId,
        decimal quantity)
    {
        return new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = documentHeaderId,
            ProductId = productId,
            LocationId = locationId,
            Description = "Inventory count",
            Quantity = quantity,
            UnitPrice = 0m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    /// <summary>
    /// When a closed inventory document exists before the reference date, the snapshot must use
    /// the inventory quantity as the starting point and apply only movements that occurred AFTER
    /// the inventory document date (day boundary, inclusive of the next day onward).
    /// </summary>
    [Fact]
    public async Task GetStockSnapshotAsync_InventoryAnchor_UsesInventoryAsStartingPoint()
    {
        // Arrange
        SeedInventoryDocumentType();

        var jan01 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var feb06 = new DateTime(2025, 2, 6, 0, 0, 0, DateTimeKind.Utc);
        var feb10 = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc);
        var feb15 = new DateTime(2025, 2, 15, 0, 0, 0, DateTimeKind.Utc);

        // Two inbound movements on Jan 01
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 40m, jan01),
            MakeMovement(_productId, _locationId, null, 60m, jan01.AddHours(1)));

        // Inventory document on Feb 06 saying stock is 80 (anchor)
        var inventoryHeader = MakeInventoryHeader(feb06);
        _context.DocumentHeaders.Add(inventoryHeader);
        _context.DocumentRows.Add(MakeInventoryRow(inventoryHeader.Id, _productId, _locationId, 80m));

        // One outbound movement on Feb 10 (after inventory)
        _context.StockMovements.Add(MakeMovement(_productId, null, _locationId, 15m, feb10));

        await _context.SaveChangesAsync();

        // Act: snapshot at Feb 15
        var result = (await _stockService.GetStockSnapshotAsync(feb15)).ToList();

        // Assert: should be 80 (inventory anchor) - 15 (post-inventory outbound) = 65
        // NOT 40+60-15 = 85 (which would ignore the inventory anchor)
        Assert.Single(result);
        Assert.Equal(65m, result[0].Quantity);
    }

    /// <summary>
    /// When the reference date is BEFORE the inventory document date, the inventory anchor
    /// must be ignored and the full movement history must be used.
    /// </summary>
    [Fact]
    public async Task GetStockSnapshotAsync_SnapshotBeforeInventory_IgnoresAnchor()
    {
        // Arrange
        SeedInventoryDocumentType();

        var jan01 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var feb06 = new DateTime(2025, 2, 6, 0, 0, 0, DateTimeKind.Utc);
        var feb05 = feb06.AddDays(-1); // one day BEFORE the inventory document

        // Inbound movements before the inventory
        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 50m, jan01),
            MakeMovement(_productId, _locationId, null, 30m, jan01.AddDays(10)));

        // Inventory document on Feb 06 (after the reference date)
        var inventoryHeader = MakeInventoryHeader(feb06);
        _context.DocumentHeaders.Add(inventoryHeader);
        _context.DocumentRows.Add(MakeInventoryRow(inventoryHeader.Id, _productId, _locationId, 99m));

        await _context.SaveChangesAsync();

        // Act: snapshot at Feb 05 (before the inventory document)
        var result = (await _stockService.GetStockSnapshotAsync(feb05)).ToList();

        // Assert: should be 50+30 = 80 (full movement history, inventory is ignored)
        Assert.Single(result);
        Assert.Equal(80m, result[0].Quantity);
    }

    /// <summary>
    /// When no inventory document exists, the snapshot must behave exactly as before
    /// (accumulate from zero using the full movement history).
    /// </summary>
    [Fact]
    public async Task GetStockSnapshotAsync_NoInventoryDocument_UsesFullMovementHistory()
    {
        // Arrange — no inventory document type seeded, no DocumentRows
        var jan01 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mar01 = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 100m, jan01),
            MakeMovement(_productId, null, _locationId, 25m,  jan01.AddDays(30)));

        await _context.SaveChangesAsync();

        // Act
        var result = (await _stockService.GetStockSnapshotAsync(mar01)).ToList();

        // Assert: 100 - 25 = 75 (standard behaviour)
        Assert.Single(result);
        Assert.Equal(75m, result[0].Quantity);
    }

    /// <summary>
    /// An inventory document that is NOT in Closed status must NOT be used as an anchor.
    /// Only closed inventory documents are authoritative.
    /// </summary>
    [Fact]
    public async Task GetStockSnapshotAsync_OpenInventoryDocument_IsIgnoredAsAnchor()
    {
        // Arrange
        SeedInventoryDocumentType();

        var jan01 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var feb06 = new DateTime(2025, 2, 6, 0, 0, 0, DateTimeKind.Utc);
        var mar01 = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        _context.StockMovements.AddRange(
            MakeMovement(_productId, _locationId, null, 60m, jan01),
            MakeMovement(_productId, null, _locationId, 10m, jan01.AddDays(5)));

        // Inventory document that is Open (not Closed) — must not be used as anchor
        var inventoryHeader = MakeInventoryHeader(feb06, EntityDocumentStatus.Open);
        _context.DocumentHeaders.Add(inventoryHeader);
        _context.DocumentRows.Add(MakeInventoryRow(inventoryHeader.Id, _productId, _locationId, 999m));

        await _context.SaveChangesAsync();

        // Act
        var result = (await _stockService.GetStockSnapshotAsync(mar01)).ToList();

        // Assert: 60 - 10 = 50 (inventory is not closed, so full history is used)
        Assert.Single(result);
        Assert.Equal(50m, result[0].Quantity);
    }

    public void Dispose() => _context?.Dispose();
}

// ─────────────────────────────────────────────────────────────────────────────
// Additional tests added as part of stock-snapshot correctness review
// (negative-stored quantities, pure outbound, transfer warehouse filter)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Supplemental tests that verify the Math.Abs defensive fix and edge-cases
/// not covered by the original test class.
/// </summary>
[Trait("Category", "Unit")]
public class StockSnapshotNegativeQuantityTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly StockService _stockService;

    private readonly Guid _tenantId  = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _locationId  = Guid.NewGuid();
    private readonly Guid _location2Id = Guid.NewGuid();
    private readonly Guid _productId   = Guid.NewGuid();

    private static readonly DateTime RefDate = new DateTime(2025, 6, 15);

    public StockSnapshotNegativeQuantityTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        var mockAudit   = new Mock<IAuditLogService>();
        var mockTenant  = new Mock<ITenantContext>();
        var mockLogger  = new Mock<ILogger<StockService>>();

        mockTenant.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        _stockService = new StockService(
            _context,
            mockAudit.Object,
            mockTenant.Object,
            mockLogger.Object);

        // Seed warehouse + two locations + product
        _context.StorageFacilities.Add(new StorageFacility
        {
            Id = _warehouseId, TenantId = _tenantId, Name = "WH", Code = "WH",
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        _context.StorageLocations.AddRange(
            new StorageLocation { Id = _locationId,  TenantId = _tenantId, WarehouseId = _warehouseId, Code = "LOC-A", CreatedAt = DateTime.UtcNow, CreatedBy = "test" },
            new StorageLocation { Id = _location2Id, TenantId = _tenantId, WarehouseId = _warehouseId, Code = "LOC-B", CreatedAt = DateTime.UtcNow, CreatedBy = "test" });
        _context.Products.Add(new Product
        {
            Id = _productId, TenantId = _tenantId, Code = "P01", Name = "Product",
            DefaultPrice = 10m, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        _context.SaveChanges();
    }

    private StockMovement MakeMovement(Guid? toId, Guid? fromId, decimal quantity, MovementStatus status = MovementStatus.Completed)
        => new StockMovement
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = _productId,
            ToLocationId = toId, FromLocationId = fromId,
            Quantity = quantity,
            MovementType = toId.HasValue && fromId.HasValue
                ? StockMovementType.Transfer
                : toId.HasValue ? StockMovementType.Inbound : StockMovementType.Outbound,
            Status = status,
            MovementDate = RefDate.AddDays(-1),
            Reason = StockMovementReason.Purchase,
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };

    // ─────────────────────────────────────────────────────────────────────────
    // 23. Inbound movement stored with a negative Quantity → Math.Abs treats it
    //     as a positive inflow (defensive fix for legacy data).
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_InboundWithNegativeStoredQuantity_TreatedAsPositiveInflow()
    {
        // Simulate legacy data: an inbound movement with Quantity=-50 in DB
        _context.StockMovements.Add(MakeMovement(toId: _locationId, fromId: null, quantity: -50m));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        // Math.Abs(-50) = 50 → quantity should be +50, not -50
        Assert.Equal(50m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 24. Outbound movement stored with a negative Quantity → Math.Abs prevents
    //     double-negation (would add stock instead of reducing it without fix).
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_OutboundWithNegativeStoredQuantity_TreatedAsReduction()
    {
        // 100 in (normal positive), then 30 out stored as -30 (legacy)
        _context.StockMovements.AddRange(
            MakeMovement(toId: _locationId,   fromId: null,      quantity: 100m),
            MakeMovement(toId: null,           fromId: _locationId, quantity: -30m));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        // Without Math.Abs: 100 - (-30) = 130 (wrong)
        // With    Math.Abs: 100 -   30  =  70 (correct)
        Assert.Equal(70m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 25. Pure outbound-only row (no prior inbound for that location) produces
    //     a negative quantity entry in the snapshot (oversold/data gap).
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_PureOutbound_ProducesNegativeQuantity()
    {
        _context.StockMovements.Add(MakeMovement(toId: null, fromId: _locationId, quantity: 40m));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate)).ToList();

        Assert.Single(result);
        Assert.Equal(-40m, result[0].Quantity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 26. Transfer with warehouseId filter: both sides (src and dst) belong to
    //     the same warehouse, so both location entries are returned.
    //     Total stock across the warehouse is conserved.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockSnapshotAsync_TransferWithWarehouseFilter_BothEntriesReturnedNetZero()
    {
        // Seed 100 into LOC-A, then transfer 40 from LOC-A to LOC-B
        _context.StockMovements.AddRange(
            MakeMovement(toId: _locationId,  fromId: null,       quantity: 100m),
            MakeMovement(toId: _location2Id, fromId: _locationId, quantity: 40m));
        await _context.SaveChangesAsync();

        var result = (await _stockService.GetStockSnapshotAsync(RefDate, warehouseId: _warehouseId)).ToList();

        // Both location entries are included (same warehouse)
        Assert.Equal(2, result.Count);
        var locA = result.First(r => r.LocationId == _locationId);
        var locB = result.First(r => r.LocationId == _location2Id);

        Assert.Equal(60m, locA.Quantity);  // 100 - 40
        Assert.Equal(40m, locB.Quantity);  // 0   + 40

        // Net across warehouse is conserved (100 in, 0 out of warehouse)
        Assert.Equal(100m, locA.Quantity + locB.Quantity);
    }

    public void Dispose() => _context?.Dispose();
}
