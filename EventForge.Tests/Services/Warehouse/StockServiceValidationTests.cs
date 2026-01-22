using EventForge.DTOs.Warehouse;
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
/// Unit tests for StockService focusing on foreign key validation.
/// Tests the fix for foreign key constraint violations when creating stock entries.
/// </summary>
[Trait("Category", "Unit")]
public class StockServiceValidationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<StockService>> _mockLogger;
    private readonly StockService _stockService;
    private readonly Guid _tenantId = Guid.NewGuid();

    public StockServiceValidationTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        // Create mocks
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<StockService>>();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create StockService
        _stockService = new StockService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateOrUpdateStockAsync_WithNonExistentProduct_ThrowsArgumentException()
    {
        // Arrange
        var validStorageLocation = CreateTestStorageLocation();
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = Guid.NewGuid(), // Non-existent product
            StorageLocationId = validStorageLocation.Id,
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _stockService.CreateOrUpdateStockAsync(createDto, "testuser"));
        
        Assert.Contains("Product with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrUpdateStockAsync_WithNonExistentStorageLocation_ThrowsArgumentException()
    {
        // Arrange
        var validProduct = CreateTestProduct();
        await _context.Products.AddAsync(validProduct);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = Guid.NewGuid(), // Non-existent storage location
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _stockService.CreateOrUpdateStockAsync(createDto, "testuser"));
        
        Assert.Contains("Storage location with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrUpdateStockAsync_WithNonExistentLot_ThrowsArgumentException()
    {
        // Arrange
        var validProduct = CreateTestProduct();
        var validStorageLocation = CreateTestStorageLocation();
        await _context.Products.AddAsync(validProduct);
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = validStorageLocation.Id,
            LotId = Guid.NewGuid(), // Non-existent lot
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _stockService.CreateOrUpdateStockAsync(createDto, "testuser"));
        
        Assert.Contains("Lot with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrUpdateStockAsync_WithDeletedProduct_ThrowsArgumentException()
    {
        // Arrange
        var deletedProduct = CreateTestProduct();
        deletedProduct.IsDeleted = true; // Soft deleted
        var validStorageLocation = CreateTestStorageLocation();
        
        await _context.Products.AddAsync(deletedProduct);
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = deletedProduct.Id,
            StorageLocationId = validStorageLocation.Id,
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _stockService.CreateOrUpdateStockAsync(createDto, "testuser"));
        
        Assert.Contains("Product with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrUpdateStockAsync_WithDeletedStorageLocation_ThrowsArgumentException()
    {
        // Arrange
        var validProduct = CreateTestProduct();
        var deletedLocation = CreateTestStorageLocation();
        deletedLocation.IsDeleted = true; // Soft deleted
        
        await _context.Products.AddAsync(validProduct);
        await _context.StorageLocations.AddAsync(deletedLocation);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = deletedLocation.Id,
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _stockService.CreateOrUpdateStockAsync(createDto, "testuser"));
        
        Assert.Contains("Storage location with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrUpdateStockAsync_WithDeletedLot_ThrowsArgumentException()
    {
        // Arrange
        var validProduct = CreateTestProduct();
        var validStorageLocation = CreateTestStorageLocation();
        var deletedLot = CreateTestLot();
        deletedLot.IsDeleted = true; // Soft deleted
        
        await _context.Products.AddAsync(validProduct);
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.Lots.AddAsync(deletedLot);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = validStorageLocation.Id,
            LotId = deletedLot.Id,
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _stockService.CreateOrUpdateStockAsync(createDto, "testuser"));
        
        Assert.Contains("Lot with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrUpdateStockAsync_WithDifferentTenantProduct_ThrowsArgumentException()
    {
        // Arrange
        var differentTenantId = Guid.NewGuid();
        var productDifferentTenant = CreateTestProduct();
        productDifferentTenant.TenantId = differentTenantId; // Different tenant
        
        var validStorageLocation = CreateTestStorageLocation();
        
        await _context.Products.AddAsync(productDifferentTenant);
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = productDifferentTenant.Id,
            StorageLocationId = validStorageLocation.Id,
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _stockService.CreateOrUpdateStockAsync(createDto, "testuser"));
        
        Assert.Contains("Product with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrUpdateStockAsync_WithDifferentTenantStorageLocation_ThrowsArgumentException()
    {
        // Arrange
        var differentTenantId = Guid.NewGuid();
        var validProduct = CreateTestProduct();
        var locationDifferentTenant = CreateTestStorageLocation();
        locationDifferentTenant.TenantId = differentTenantId; // Different tenant
        
        await _context.Products.AddAsync(validProduct);
        await _context.StorageLocations.AddAsync(locationDifferentTenant);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = locationDifferentTenant.Id,
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _stockService.CreateOrUpdateStockAsync(createDto, "testuser"));
        
        Assert.Contains("Storage location with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact(Skip = "Requires full database setup with navigation properties for in-memory testing")]
    public async Task CreateOrUpdateStockAsync_WithValidData_CreatesStock()
    {
        // Arrange
        var validProduct = CreateTestProduct();
        var validStorageLocation = CreateTestStorageLocation();
        
        await _context.Products.AddAsync(validProduct);
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = validStorageLocation.Id,
            Quantity = 10,
            ReservedQuantity = 2,
            MinimumLevel = 5,
            MaximumLevel = 100
        };

        // Act
        var result = await _stockService.CreateOrUpdateStockAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validProduct.Id, result.ProductId);
        Assert.Equal(validStorageLocation.Id, result.StorageLocationId);
        Assert.Equal(10, result.Quantity);
        Assert.Equal(2, result.ReservedQuantity);
        Assert.Equal(8, result.AvailableQuantity); // Quantity - ReservedQuantity
    }

    [Fact(Skip = "Requires full database setup with navigation properties for in-memory testing")]
    public async Task CreateOrUpdateStockAsync_WithValidDataAndLot_CreatesStock()
    {
        // Arrange
        var validProduct = CreateTestProduct();
        var validStorageLocation = CreateTestStorageLocation();
        var validLot = CreateTestLot();
        
        await _context.Products.AddAsync(validProduct);
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.Lots.AddAsync(validLot);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = validStorageLocation.Id,
            LotId = validLot.Id,
            Quantity = 10
        };

        // Act
        var result = await _stockService.CreateOrUpdateStockAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validProduct.Id, result.ProductId);
        Assert.Equal(validStorageLocation.Id, result.StorageLocationId);
        Assert.Equal(validLot.Id, result.LotId);
        Assert.Equal(10, result.Quantity);
    }

    [Fact(Skip = "Requires full database setup with navigation properties for in-memory testing")]
    public async Task CreateOrUpdateStockAsync_WithExistingStock_UpdatesStock()
    {
        // Arrange
        var validProduct = CreateTestProduct();
        var validStorageLocation = CreateTestStorageLocation();
        
        await _context.Products.AddAsync(validProduct);
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.SaveChangesAsync();

        // Create initial stock
        var initialDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = validStorageLocation.Id,
            Quantity = 10
        };
        var initialResult = await _stockService.CreateOrUpdateStockAsync(initialDto, "testuser");

        // Update stock
        var updateDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = validStorageLocation.Id,
            Quantity = 20,
            MinimumLevel = 5
        };

        // Act
        var result = await _stockService.CreateOrUpdateStockAsync(updateDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(initialResult.Id, result.Id); // Same stock entry
        Assert.Equal(20, result.Quantity); // Updated quantity
        Assert.Equal(5, result.MinimumLevel); // Updated minimum level
    }

    [Fact(Skip = "Requires full database setup with navigation properties for in-memory testing")]
    public async Task CreateOrUpdateStockAsync_WithNullLot_CreatesStockSuccessfully()
    {
        // Arrange
        var validProduct = CreateTestProduct();
        var validStorageLocation = CreateTestStorageLocation();
        
        await _context.Products.AddAsync(validProduct);
        await _context.StorageLocations.AddAsync(validStorageLocation);
        await _context.SaveChangesAsync();

        var createDto = new CreateStockDto
        {
            ProductId = validProduct.Id,
            StorageLocationId = validStorageLocation.Id,
            LotId = null, // Null lot should be acceptable
            Quantity = 10
        };

        // Act
        var result = await _stockService.CreateOrUpdateStockAsync(createDto, "testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validProduct.Id, result.ProductId);
        Assert.Equal(validStorageLocation.Id, result.StorageLocationId);
        Assert.Null(result.LotId);
        Assert.Equal(10, result.Quantity);
    }

    private Product CreateTestProduct()
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = $"TEST-{Guid.NewGuid():N}",
            Name = "Test Product",
            Status = EventForge.Server.Data.Entities.Products.ProductStatus.Active,
            IsDeleted = false,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };
    }

    private StorageLocation CreateTestStorageLocation()
    {
        return new StorageLocation
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = $"LOC-{Guid.NewGuid():N}",
            Description = "Test Location",
            WarehouseId = Guid.NewGuid(), // Simplified - not validating warehouse here
            IsDeleted = false,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };
    }

    private Lot CreateTestLot()
    {
        return new Lot
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = $"LOT-{Guid.NewGuid():N}",
            ProductId = Guid.NewGuid(), // Simplified - not validating product here
            IsDeleted = false,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
