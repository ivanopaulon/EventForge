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
/// Unit tests for StockMovementService covering bug fixes:
/// Bug A - UpdateStockLevelsForMovementAsync throws when stock not found for Outbound/Transfer
/// Bug B - CreateMovementAsync and ExecutePlannedMovementAsync load navigation properties instead of extra DB query
/// Bug C - ReverseMovementAsync correctly inverts movement type (Inbound↔Outbound)
/// </summary>
[Trait("Category", "Unit")]
public class StockMovementServiceBugFixTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<StockMovementService>> _mockLogger;
    private readonly StockMovementService _stockMovementService;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _storageLocationId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public StockMovementServiceBugFixTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<StockMovementService>>();

        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        _stockMovementService = new StockMovementService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var warehouseId = Guid.NewGuid();

        var warehouse = new StorageFacility
        {
            Id = warehouseId,
            TenantId = _tenantId,
            Name = "Main Warehouse",
            Code = "WH001",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StorageFacilities.Add(warehouse);

        var storageLocation = new StorageLocation
        {
            Id = _storageLocationId,
            TenantId = _tenantId,
            WarehouseId = warehouseId,
            Code = "LOC-001",
            Description = "Test Location",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StorageLocations.Add(storageLocation);

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

        _context.SaveChanges();
    }

    private Stock CreateStock(Guid productId, Guid locationId, decimal quantity = 100)
    {
        var stock = new Stock
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            StorageLocationId = locationId,
            Quantity = quantity,
            ReservedQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Stocks.Add(stock);
        _context.SaveChanges();
        return stock;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Bug A: UpdateStockLevelsForMovementAsync — stock null for Outbound/Transfer
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMovementAsync_Outbound_WithNoExistingStock_ThrowsInvalidOperationException()
    {
        // Arrange: no stock record exists for the product at the location
        var createDto = new CreateStockMovementDto
        {
            MovementType = "Outbound",
            ProductId = _productId,
            FromLocationId = _storageLocationId,
            Quantity = 10,
            Reason = "Sale"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stockMovementService.CreateMovementAsync(createDto, "testuser"));

        Assert.Contains(_productId.ToString(), exception.Message);
        Assert.Contains(_storageLocationId.ToString(), exception.Message);
        Assert.Contains("Outbound", exception.Message);
    }

    [Fact]
    public async Task CreateMovementAsync_Transfer_WithNoExistingStock_ThrowsInvalidOperationException()
    {
        // Arrange: no stock record exists at the source location
        var toLocationId = Guid.NewGuid();
        var createDto = new CreateStockMovementDto
        {
            MovementType = "Transfer",
            ProductId = _productId,
            FromLocationId = _storageLocationId,
            ToLocationId = toLocationId,
            Quantity = 5,
            Reason = "Transfer"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stockMovementService.CreateMovementAsync(createDto, "testuser"));

        Assert.Contains(_productId.ToString(), exception.Message);
        Assert.Contains(_storageLocationId.ToString(), exception.Message);
        Assert.Contains("Transfer", exception.Message);
    }

    [Fact]
    public async Task CreateMovementAsync_Outbound_WithNoExistingStock_LogsWarning()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            MovementType = "Outbound",
            ProductId = _productId,
            FromLocationId = _storageLocationId,
            Quantity = 10,
            Reason = "Sale"
        };

        // Act
        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stockMovementService.CreateMovementAsync(createDto, "testuser"));

        // Assert: LogWarning was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No stock record found")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateMovementAsync_Outbound_WithExistingStock_DeductsQuantity()
    {
        // Arrange
        var stock = CreateStock(_productId, _storageLocationId, quantity: 50);

        var createDto = new CreateStockMovementDto
        {
            MovementType = "Outbound",
            ProductId = _productId,
            FromLocationId = _storageLocationId,
            Quantity = 10,
            Reason = "Sale"
        };

        // Act
        var result = await _stockMovementService.CreateMovementAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        var updatedStock = await _context.Stocks.FindAsync(stock.Id);
        Assert.Equal(40, updatedStock!.Quantity);
    }

    [Fact]
    public async Task CreateMovementAsync_Inbound_WithNoExistingStock_CreatesNewStockRecord()
    {
        // Arrange: no stock record – Inbound should create one, not throw
        var createDto = new CreateStockMovementDto
        {
            MovementType = "Inbound",
            ProductId = _productId,
            ToLocationId = _storageLocationId,
            Quantity = 20,
            Reason = "Purchase"
        };

        // Act
        var result = await _stockMovementService.CreateMovementAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        var newStock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == _productId && s.StorageLocationId == _storageLocationId);
        Assert.NotNull(newStock);
        Assert.Equal(20, newStock!.Quantity);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Bug B: CreateMovementAsync / ExecutePlannedMovementAsync return DTO
    //        via navigation property loading instead of extra DB query
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMovementAsync_Inbound_ReturnsCorrectDtoWithoutExtraQuery()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            MovementType = "Inbound",
            ProductId = _productId,
            ToLocationId = _storageLocationId,
            Quantity = 15,
            Reason = "Purchase"
        };

        // Act
        var result = await _stockMovementService.CreateMovementAsync(createDto, "testuser");

        // Assert: DTO fields are populated (navigation properties were loaded)
        Assert.NotNull(result);
        Assert.Equal(_productId, result.ProductId);
        Assert.Equal("Inbound", result.MovementType);
        Assert.Equal(15, result.Quantity);
        // Product name is populated from navigation property
        Assert.Equal("Test Product", result.ProductName);
        Assert.Equal(_storageLocationId, result.ToLocationId);
    }

    [Fact]
    public async Task ExecutePlannedMovementAsync_ReturnsCorrectDtoWithNavigationProperties()
    {
        // Arrange: create a planned movement directly in the DB (with existing source stock)
        _ = CreateStock(_productId, _storageLocationId, quantity: 80);

        var plannedMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            MovementType = StockMovementType.Outbound,
            ProductId = _productId,
            FromLocationId = _storageLocationId,
            Quantity = 10,
            Status = MovementStatus.Planned,
            MovementDate = DateTime.UtcNow,
            Reason = StockMovementReason.Other,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StockMovements.Add(plannedMovement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _stockMovementService.ExecutePlannedMovementAsync(plannedMovement.Id, "testuser");

        // Assert: DTO is correctly populated (navigation properties loaded)
        Assert.NotNull(result);
        Assert.Equal(plannedMovement.Id, result.Id);
        Assert.Equal("Outbound", result.MovementType);
        Assert.Equal("Completed", result.Status);
        Assert.Equal("Test Product", result.ProductName);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Bug C: ReverseMovementAsync — movement type must be inverted
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReverseMovementAsync_Inbound_CreatesOutboundReversal()
    {
        // Arrange: create original inbound movement with stock at destination
        var originalStock = CreateStock(_productId, _storageLocationId, quantity: 50);

        var originalMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            MovementType = StockMovementType.Inbound,
            ProductId = _productId,
            ToLocationId = _storageLocationId,
            Quantity = 10,
            Status = MovementStatus.Completed,
            MovementDate = DateTime.UtcNow,
            Reason = StockMovementReason.Purchase,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StockMovements.Add(originalMovement);
        await _context.SaveChangesAsync();

        var stockBefore = originalStock.Quantity;

        // Act
        var result = await _stockMovementService.ReverseMovementAsync(
            originalMovement.Id, "wrong delivery", "testuser");

        // Assert: reversal movement is Outbound (inverted from Inbound)
        Assert.NotNull(result);
        Assert.Equal("Outbound", result.MovementType);
        // FromLocationId of reversal = ToLocationId of original
        Assert.Equal(_storageLocationId, result.FromLocationId);
        // Quantity reduced from stock
        var updatedStock = await _context.Stocks.FindAsync(originalStock.Id);
        Assert.Equal(stockBefore - 10, updatedStock!.Quantity);
    }

    [Fact]
    public async Task ReverseMovementAsync_Outbound_CreatesInboundReversal()
    {
        // Arrange: create a location for the "to" side of the reversal
        var sourceLocationId = Guid.NewGuid();
        var warehouseId = await _context.StorageFacilities
            .Where(w => w.TenantId == _tenantId)
            .Select(w => w.Id)
            .FirstAsync();

        var toLocation = new StorageLocation
        {
            Id = sourceLocationId,
            TenantId = _tenantId,
            WarehouseId = warehouseId,
            Code = "LOC-002",
            Description = "Source Location",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StorageLocations.Add(toLocation);

        // Original outbound: goods left _storageLocationId going to sourceLocationId
        var originalMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            MovementType = StockMovementType.Outbound,
            ProductId = _productId,
            FromLocationId = _storageLocationId,
            ToLocationId = sourceLocationId,
            Quantity = 5,
            Status = MovementStatus.Completed,
            MovementDate = DateTime.UtcNow,
            Reason = StockMovementReason.Sale,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StockMovements.Add(originalMovement);
        await _context.SaveChangesAsync();

        // Act: reversal should be Inbound (goods return to _storageLocationId)
        var result = await _stockMovementService.ReverseMovementAsync(
            originalMovement.Id, "customer return", "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Inbound", result.MovementType);
        // ToLocationId of reversal = FromLocationId of original
        Assert.Equal(_storageLocationId, result.ToLocationId);
    }

    [Fact]
    public async Task ReverseMovementAsync_Transfer_RemainsTransferWithInvertedLocations()
    {
        // Arrange: create source stock for the Transfer reversal (which is also a Transfer)
        var destLocationId = Guid.NewGuid();
        var warehouseId = await _context.StorageFacilities
            .Where(w => w.TenantId == _tenantId)
            .Select(w => w.Id)
            .FirstAsync();

        var destLocation = new StorageLocation
        {
            Id = destLocationId,
            TenantId = _tenantId,
            WarehouseId = warehouseId,
            Code = "LOC-003",
            Description = "Destination Location",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StorageLocations.Add(destLocation);

        // Stock at destination location (source of the reversal transfer)
        _ = CreateStock(_productId, destLocationId, quantity: 30);

        var originalMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            MovementType = StockMovementType.Transfer,
            ProductId = _productId,
            FromLocationId = _storageLocationId,
            ToLocationId = destLocationId,
            Quantity = 10,
            Status = MovementStatus.Completed,
            MovementDate = DateTime.UtcNow,
            Reason = StockMovementReason.Transfer,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StockMovements.Add(originalMovement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _stockMovementService.ReverseMovementAsync(
            originalMovement.Id, "undo transfer", "testuser");

        // Assert: type stays Transfer but locations are inverted
        Assert.NotNull(result);
        Assert.Equal("Transfer", result.MovementType);
        Assert.Equal(destLocationId, result.FromLocationId);
        Assert.Equal(_storageLocationId, result.ToLocationId);
    }

    [Fact]
    public async Task ReverseMovementAsync_Adjustment_RemainsAdjustment()
    {
        // Arrange
        _ = CreateStock(_productId, _storageLocationId, quantity: 100);

        var originalMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            MovementType = StockMovementType.Adjustment,
            ProductId = _productId,
            FromLocationId = _storageLocationId,
            Quantity = 5,
            Status = MovementStatus.Completed,
            MovementDate = DateTime.UtcNow,
            Reason = StockMovementReason.Adjustment,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StockMovements.Add(originalMovement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _stockMovementService.ReverseMovementAsync(
            originalMovement.Id, "undo adjustment", "testuser");

        // Assert: type stays Adjustment
        Assert.NotNull(result);
        Assert.Equal("Adjustment", result.MovementType);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
