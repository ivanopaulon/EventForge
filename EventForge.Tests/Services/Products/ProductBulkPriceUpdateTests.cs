using EventForge.DTOs.Bulk;
using EventForge.DTOs.Products;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.CodeGeneration;
using EventForge.Server.Services.PriceHistory;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for bulk price update functionality in ProductService.
/// </summary>
public class ProductBulkPriceUpdateTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly ProductService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly List<Guid> _productIds = new();

    public ProductBulkPriceUpdateTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns((Guid?)_tenantId);

        var mockAuditLog = new Mock<IAuditLogService>();
        var mockLogger = new Mock<ILogger<ProductService>>();
        var mockCodeGenerator = new Mock<IDailyCodeGenerator>();
        var mockPriceHistory = new Mock<ISupplierProductPriceHistoryService>();

        _service = new ProductService(
            _context,
            mockAuditLog.Object,
            mockTenantContext.Object,
            mockLogger.Object,
            mockCodeGenerator.Object,
            mockPriceHistory.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test products
        for (int i = 1; i <= 5; i++)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = $"Test Product {i}",
                Code = $"P{i:D3}",
                DefaultPrice = 100m * i,
                Status = EventForge.Server.Data.Entities.Products.ProductStatus.Active
            };
            _context.Products.Add(product);
            _productIds.Add(product.Id);
        }

        _context.SaveChanges();
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_Replace_ShouldSetFixedPrice()
    {
        // Arrange
        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = _productIds.Take(3).ToList(),
            UpdateType = PriceUpdateType.Replace,
            NewPrice = 150m,
            Reason = "Price adjustment"
        };

        // Act
        var result = await _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user");

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.False(result.RolledBack);
        Assert.Empty(result.Errors);

        var updatedProducts = await _context.Products
            .Where(p => bulkUpdateDto.ProductIds.Contains(p.Id))
            .ToListAsync();

        Assert.All(updatedProducts, p => Assert.Equal(150m, p.DefaultPrice));
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_IncreaseByPercentage_ShouldIncreasePrice()
    {
        // Arrange
        var productId = _productIds.First();
        var product = await _context.Products.FindAsync(productId);
        var originalPrice = product!.DefaultPrice!.Value;

        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = new List<Guid> { productId },
            UpdateType = PriceUpdateType.IncreaseByPercentage,
            Percentage = 20m, // 20% increase
            Reason = "Seasonal price increase"
        };

        // Act
        var result = await _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.Products.FindAsync(productId);
        var expectedPrice = originalPrice * 1.20m;
        Assert.Equal(expectedPrice, updatedProduct!.DefaultPrice);
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_DecreaseByPercentage_ShouldDecreasePrice()
    {
        // Arrange
        var productId = _productIds.First();
        var product = await _context.Products.FindAsync(productId);
        var originalPrice = product!.DefaultPrice!.Value;

        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = new List<Guid> { productId },
            UpdateType = PriceUpdateType.DecreaseByPercentage,
            Percentage = 10m, // 10% decrease
            Reason = "Clearance sale"
        };

        // Act
        var result = await _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.Products.FindAsync(productId);
        var expectedPrice = originalPrice * 0.90m;
        Assert.Equal(expectedPrice, updatedProduct!.DefaultPrice);
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_IncreaseByAmount_ShouldIncreasePrice()
    {
        // Arrange
        var productId = _productIds.First();
        var product = await _context.Products.FindAsync(productId);
        var originalPrice = product!.DefaultPrice!.Value;

        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = new List<Guid> { productId },
            UpdateType = PriceUpdateType.IncreaseByAmount,
            Amount = 25m,
            Reason = "Cost increase"
        };

        // Act
        var result = await _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.Products.FindAsync(productId);
        Assert.Equal(originalPrice + 25m, updatedProduct!.DefaultPrice);
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_DecreaseByAmount_ShouldDecreasePrice()
    {
        // Arrange
        var productId = _productIds.First();
        var product = await _context.Products.FindAsync(productId);
        var originalPrice = product!.DefaultPrice!.Value;

        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = new List<Guid> { productId },
            UpdateType = PriceUpdateType.DecreaseByAmount,
            Amount = 30m,
            Reason = "Price correction"
        };

        // Act
        var result = await _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.Products.FindAsync(productId);
        Assert.Equal(originalPrice - 30m, updatedProduct!.DefaultPrice);
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_WithNonExistentProduct_ShouldReportError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = new List<Guid> { _productIds.First(), nonExistentId },
            UpdateType = PriceUpdateType.Replace,
            NewPrice = 150m
        };

        // Act
        var result = await _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user");

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, e => e.ItemId == nonExistentId);
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_ExceedsMaxBatchSize_ShouldThrowException()
    {
        // Arrange
        var tooManyIds = Enumerable.Range(0, 501).Select(_ => Guid.NewGuid()).ToList();
        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = tooManyIds,
            UpdateType = PriceUpdateType.Replace,
            NewPrice = 150m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user"));
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_Replace_WithoutNewPrice_ShouldThrowException()
    {
        // Arrange
        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = _productIds.Take(1).ToList(),
            UpdateType = PriceUpdateType.Replace,
            NewPrice = null // Missing required field
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user"));
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_NegativeResultingPrice_ShouldReportError()
    {
        // Arrange
        var productId = _productIds.First();
        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = new List<Guid> { productId },
            UpdateType = PriceUpdateType.DecreaseByAmount,
            Amount = 200m, // This would result in negative price
            Reason = "Testing negative price"
        };

        // Act
        var result = await _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user");

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Errors);
        Assert.Contains("negative", result.Errors.First().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_BatchOf100Products_ShouldSucceed()
    {
        // Arrange - Create 100 products
        var batchProductIds = new List<Guid>();
        for (int i = 0; i < 100; i++)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = $"Batch Product {i}",
                Code = $"BP{i:D3}",
                DefaultPrice = 50m,
                Status = EventForge.Server.Data.Entities.Products.ProductStatus.Active
            };
            _context.Products.Add(product);
            batchProductIds.Add(product.Id);
        }
        await _context.SaveChangesAsync();

        var bulkUpdateDto = new BulkUpdatePricesDto
        {
            ProductIds = batchProductIds,
            UpdateType = PriceUpdateType.IncreaseByPercentage,
            Percentage = 15m,
            Reason = "Batch price update test"
        };

        // Act
        var result = await _service.BulkUpdatePricesAsync(bulkUpdateDto, "test-user");

        // Assert
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(100, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.False(result.RolledBack);
        Assert.Empty(result.Errors);

        // Verify all products were updated
        var updatedProducts = await _context.Products
            .Where(p => batchProductIds.Contains(p.Id))
            .ToListAsync();

        Assert.All(updatedProducts, p => Assert.Equal(57.50m, p.DefaultPrice)); // 50 * 1.15 = 57.50
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
