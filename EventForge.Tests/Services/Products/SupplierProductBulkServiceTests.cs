using EventForge.DTOs.Products;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for SupplierProductBulkService.
/// </summary>
public class SupplierProductBulkServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly SupplierProductBulkService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();

    public SupplierProductBulkServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);
        var logger = new Mock<ILogger<SupplierProductBulkService>>();
        _service = new SupplierProductBulkService(_context, logger.Object);

        // Seed initial data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var supplier = new BusinessParty
        {
            Id = _supplierId,
            TenantId = _tenantId,
            Name = "Test Supplier",
            PartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType.Fornitore
        };

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Product 1",
            Code = "P001"
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Product 2",
            Code = "P002"
        };

        var product3 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Product 3",
            Code = "P003"
        };

        _context.BusinessParties.Add(supplier);
        _context.Products.AddRange(product1, product2, product3);

        _context.ProductSuppliers.AddRange(
            new ProductSupplier
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ProductId = product1.Id,
                SupplierId = _supplierId,
                UnitCost = 100.00m,
                Currency = "EUR",
                LeadTimeDays = 5,
                MinOrderQty = 10
            },
            new ProductSupplier
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ProductId = product2.Id,
                SupplierId = _supplierId,
                UnitCost = 50.00m,
                Currency = "EUR",
                LeadTimeDays = 3,
                MinOrderQty = 5
            },
            new ProductSupplier
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ProductId = product3.Id,
                SupplierId = _supplierId,
                UnitCost = 200.00m,
                Currency = "EUR",
                LeadTimeDays = 7,
                MinOrderQty = 1
            }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_SetMode_ShouldSetFixedPrice()
    {
        // Arrange
        var productIds = _context.ProductSuppliers
            .Where(ps => ps.SupplierId == _supplierId)
            .Select(ps => ps.ProductId)
            .Take(2)
            .ToList();

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = productIds,
            UpdateMode = UpdateMode.Set,
            UnitCostValue = 75.00m
        };

        // Act
        var result = await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.False(result.RolledBack);

        var updatedProducts = await _context.ProductSuppliers
            .Where(ps => productIds.Contains(ps.ProductId))
            .ToListAsync();

        Assert.All(updatedProducts, ps => Assert.Equal(75.00m, ps.UnitCost));
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_IncreaseMode_ShouldIncreasePrice()
    {
        // Arrange
        var productSupplier = _context.ProductSuppliers
            .First(ps => ps.SupplierId == _supplierId);

        var originalPrice = productSupplier.UnitCost!.Value;

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = new List<Guid> { productSupplier.ProductId },
            UpdateMode = UpdateMode.Increase,
            UnitCostValue = 10.00m
        };

        // Act
        var result = await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.ProductSuppliers
            .FirstAsync(ps => ps.ProductId == productSupplier.ProductId);

        Assert.Equal(originalPrice + 10.00m, updatedProduct.UnitCost);
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_DecreaseMode_ShouldDecreasePrice()
    {
        // Arrange
        var productSupplier = _context.ProductSuppliers
            .First(ps => ps.SupplierId == _supplierId);

        var originalPrice = productSupplier.UnitCost!.Value;

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = new List<Guid> { productSupplier.ProductId },
            UpdateMode = UpdateMode.Decrease,
            UnitCostValue = 5.00m
        };

        // Act
        var result = await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.ProductSuppliers
            .FirstAsync(ps => ps.ProductId == productSupplier.ProductId);

        Assert.Equal(originalPrice - 5.00m, updatedProduct.UnitCost);
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_PercentageIncreaseMode_ShouldIncreaseByPercentage()
    {
        // Arrange
        var productSupplier = _context.ProductSuppliers
            .First(ps => ps.SupplierId == _supplierId && ps.UnitCost == 100.00m);

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = new List<Guid> { productSupplier.ProductId },
            UpdateMode = UpdateMode.PercentageIncrease,
            UnitCostValue = 10m // 10%
        };

        // Act
        var result = await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.ProductSuppliers
            .FirstAsync(ps => ps.ProductId == productSupplier.ProductId);

        Assert.Equal(110.00m, updatedProduct.UnitCost); // 100 + 10%
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_PercentageDecreaseMode_ShouldDecreaseByPercentage()
    {
        // Arrange
        var productSupplier = _context.ProductSuppliers
            .First(ps => ps.SupplierId == _supplierId && ps.UnitCost == 200.00m);

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = new List<Guid> { productSupplier.ProductId },
            UpdateMode = UpdateMode.PercentageDecrease,
            UnitCostValue = 25m // 25%
        };

        // Act
        var result = await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.ProductSuppliers
            .FirstAsync(ps => ps.ProductId == productSupplier.ProductId);

        Assert.Equal(150.00m, updatedProduct.UnitCost); // 200 - 25%
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_NegativePrice_ShouldRollback()
    {
        // Arrange
        var productSupplier = _context.ProductSuppliers
            .First(ps => ps.SupplierId == _supplierId && ps.UnitCost == 50.00m);

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = new List<Guid> { productSupplier.ProductId },
            UpdateMode = UpdateMode.Decrease,
            UnitCostValue = 100.00m // This would result in negative price
        };

        // Act
        var result = await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.True(result.RolledBack);
        Assert.Single(result.Errors);

        // Verify price wasn't changed
        var unchangedProduct = await _context.ProductSuppliers
            .FirstAsync(ps => ps.ProductId == productSupplier.ProductId);
        Assert.Equal(50.00m, unchangedProduct.UnitCost);
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_UpdateLeadTime_ShouldUpdateLeadTime()
    {
        // Arrange
        var productIds = _context.ProductSuppliers
            .Where(ps => ps.SupplierId == _supplierId)
            .Select(ps => ps.ProductId)
            .ToList();

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = productIds,
            LeadTimeDays = 14
        };

        // Act
        var result = await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        Assert.Equal(3, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProducts = await _context.ProductSuppliers
            .Where(ps => productIds.Contains(ps.ProductId))
            .ToListAsync();

        Assert.All(updatedProducts, ps => Assert.Equal(14, ps.LeadTimeDays));
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_UpdateMultipleFields_ShouldUpdateAll()
    {
        // Arrange
        var productSupplier = _context.ProductSuppliers
            .First(ps => ps.SupplierId == _supplierId);

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = new List<Guid> { productSupplier.ProductId },
            UpdateMode = UpdateMode.Set,
            UnitCostValue = 99.99m,
            LeadTimeDays = 10,
            Currency = "USD",
            MinOrderQuantity = 20,
            IsPreferred = true
        };

        // Act
        var result = await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.RolledBack);

        var updatedProduct = await _context.ProductSuppliers
            .FirstAsync(ps => ps.ProductId == productSupplier.ProductId);

        Assert.Equal(99.99m, updatedProduct.UnitCost);
        Assert.Equal(10, updatedProduct.LeadTimeDays);
        Assert.Equal("USD", updatedProduct.Currency);
        Assert.Equal(20, updatedProduct.MinOrderQty);
        Assert.True(updatedProduct.Preferred);
    }

    [Fact]
    public async Task BulkUpdateSupplierProductsAsync_CreatesAuditLogs()
    {
        // Arrange
        var productSupplier = _context.ProductSuppliers
            .First(ps => ps.SupplierId == _supplierId);

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = new List<Guid> { productSupplier.ProductId },
            UpdateMode = UpdateMode.Set,
            UnitCostValue = 88.88m
        };

        // Act
        await _service.BulkUpdateSupplierProductsAsync(_supplierId, request, "test-user");

        // Assert
        var auditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == productSupplier.Id && log.PropertyName == "UnitCost")
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        var bulkUpdateLog = auditLogs.FirstOrDefault(log => log.OperationType == "BulkUpdate");
        Assert.NotNull(bulkUpdateLog);
        Assert.Equal("test-user", bulkUpdateLog.ChangedBy);
        Assert.Equal("88.880000", bulkUpdateLog.NewValue);
    }

    [Fact]
    public async Task PreviewBulkUpdateAsync_ShouldReturnPreviewWithoutSaving()
    {
        // Arrange
        var productSupplier = _context.ProductSuppliers
            .First(ps => ps.SupplierId == _supplierId && ps.UnitCost == 100.00m);

        var originalPrice = productSupplier.UnitCost;

        var request = new BulkUpdateSupplierProductsRequest
        {
            ProductIds = new List<Guid> { productSupplier.ProductId },
            UpdateMode = UpdateMode.PercentageIncrease,
            UnitCostValue = 20m // 20%
        };

        // Act
        var previews = await _service.PreviewBulkUpdateAsync(_supplierId, request);

        // Assert
        Assert.Single(previews);
        var preview = previews.First();
        Assert.Equal(100.00m, preview.CurrentUnitCost);
        Assert.Equal(120.00m, preview.NewUnitCost);
        Assert.Equal(20.00m, preview.Delta);

        // Verify database was not modified
        var unchangedProduct = await _context.ProductSuppliers
            .FirstAsync(ps => ps.ProductId == productSupplier.ProductId);
        Assert.Equal(originalPrice, unchangedProduct.UnitCost);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
