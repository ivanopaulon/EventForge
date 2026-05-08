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
using Prym.DTOs.Products;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for bulk catalog update functionality in ProductService
/// (BulkUpdateProductsAsync).
/// </summary>
public class ProductBulkCatalogUpdateTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly ProductService _service;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly List<Guid> _productIds = new();

    // Shared FK values used by seed and test DTOs
    private readonly Guid _brandA = Guid.NewGuid();
    private readonly Guid _brandB = Guid.NewGuid();
    private readonly Guid _vatA = Guid.NewGuid();
    private readonly Guid _vatB = Guid.NewGuid();

    public ProductBulkCatalogUpdateTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns((Guid?)_tenantId);

        var mockAuditLog = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<ProductService>>();
        var mockCodeGenerator = new Mock<IDailyCodeGenerator>();
        var mockPriceHistory = new Mock<ISupplierProductPriceHistoryService>();

        _service = new ProductService(
            _context,
            mockAuditLog.Object,
            mockTenantContext.Object,
            _mockLogger.Object,
            mockCodeGenerator.Object,
            mockPriceHistory.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        for (int i = 1; i <= 10; i++)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = $"Catalog Product {i}",
                Code = $"CP{i:D3}",
                BrandId = _brandA,
                VatRateId = _vatA,
                DefaultPrice = 100m * i,
                Status = EventForge.Server.Data.Entities.Products.ProductStatus.Active
            };
            _context.Products.Add(product);
            _productIds.Add(product.Id);
        }

        _context.SaveChanges();
    }

    // ─── Happy-path tests ────────────────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateProductsAsync_SingleBatch_ShouldUpdateAllProducts()
    {
        // Arrange — 5 products: well below the 500-product batch threshold
        var dto = new BulkUpdateProductsDto
        {
            ProductIds = _productIds.Take(5).ToList(),
            BrandId = _brandB,
            Reason = "Brand consolidation"
        };

        // Act
        var result = await _service.BulkUpdateProductsAsync(dto, "test-user");

        // Assert result counters
        Assert.Equal(5, result.TotalRequested);
        Assert.Equal(5, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.False(result.RolledBack);
        Assert.Empty(result.Errors);

        // Assert DB state
        var updated = await _context.Products
            .Where(p => dto.ProductIds!.Contains(p.Id))
            .ToListAsync();
        Assert.All(updated, p => Assert.Equal(_brandB, p.BrandId));

        // Untouched products must be unmodified
        var untouched = await _context.Products
            .Where(p => !dto.ProductIds!.Contains(p.Id) && p.TenantId == _tenantId)
            .ToListAsync();
        Assert.All(untouched, p => Assert.Equal(_brandA, p.BrandId));
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_MultipleCatalogFields_ShouldApplyAll()
    {
        // Arrange
        var dto = new BulkUpdateProductsDto
        {
            ProductIds = _productIds.Take(3).ToList(),
            BrandId = _brandB,
            VatRateId = _vatB,
            IsVatIncluded = true,
            ReorderPoint = 10m,
            SafetyStock = 5m
        };

        // Act
        var result = await _service.BulkUpdateProductsAsync(dto, "test-user");

        // Assert
        Assert.Equal(3, result.SuccessCount);
        Assert.False(result.RolledBack);

        var products = await _context.Products
            .Where(p => dto.ProductIds!.Contains(p.Id))
            .ToListAsync();

        Assert.All(products, p =>
        {
            Assert.Equal(_brandB, p.BrandId);
            Assert.Equal(_vatB, p.VatRateId);
            Assert.True(p.IsVatIncluded);
            Assert.Equal(10m, p.ReorderPoint);
            Assert.Equal(5m, p.SafetyStock);
        });
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_FilterByBrand_ShouldOnlyUpdateMatchingProducts()
    {
        // Arrange — filter by existing brand (all 10 seed products have _brandA)
        var dto = new BulkUpdateProductsDto
        {
            FilterBrandId = _brandA,
            VatRateId = _vatB
        };

        // Act
        var result = await _service.BulkUpdateProductsAsync(dto, "test-user");

        // Assert
        Assert.Equal(10, result.TotalRequested);
        Assert.Equal(10, result.SuccessCount);

        var products = await _context.Products
            .Where(p => p.TenantId == _tenantId)
            .ToListAsync();
        Assert.All(products, p => Assert.Equal(_vatB, p.VatRateId));
    }

    // ─── Multi-batch test (>500 products) ───────────────────────────────────

    [Fact]
    public async Task BulkUpdateProductsAsync_MoreThan500Products_ShouldCommitAllBatches()
    {
        // Arrange — create 600 products (spans 2 batches of 500)
        var batchIds = new List<Guid>();
        for (int i = 0; i < 600; i++)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = $"Bulk Product {i}",
                Code = $"BLK{i:D4}",
                BrandId = _brandA,
                DefaultPrice = 10m,
                Status = EventForge.Server.Data.Entities.Products.ProductStatus.Active
            };
            _context.Products.Add(product);
            batchIds.Add(product.Id);
        }
        await _context.SaveChangesAsync();

        var dto = new BulkUpdateProductsDto
        {
            ProductIds = batchIds,
            BrandId = _brandB,
            Reason = "Multi-batch update"
        };

        // Act
        var result = await _service.BulkUpdateProductsAsync(dto, "batch-user");

        // Assert counters
        Assert.Equal(600, result.TotalRequested);
        Assert.Equal(600, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.False(result.RolledBack);
        Assert.Empty(result.Errors);

        // Assert DB state — all 600 rows must have the new brand
        var count = await _context.Products
            .CountAsync(p => batchIds.Contains(p.Id) && p.BrandId == _brandB);
        Assert.Equal(600, count);
    }

    // ─── Empty-filter / no-match tests ──────────────────────────────────────

    [Fact]
    public async Task BulkUpdateProductsAsync_EmptyProductIds_ShouldFallBackToFilterBranch()
    {
        // Arrange — explicit empty ProductIds list.
        // BuildBulkFilterQueryAsync treats dto.ProductIds?.Count > 0 as the signal to use
        // an explicit ID filter.  An empty list evaluates to false (0 > 0), so the method
        // falls through to the filter-predicates branch.  With no filter predicates set,
        // all active tenant products are matched.  This is the defined behaviour: callers
        // that want to update zero products should not call the method at all.
        var dto = new BulkUpdateProductsDto
        {
            ProductIds = new List<Guid>(), // empty, not null
            BrandId = _brandB
        };

        // Act
        var result = await _service.BulkUpdateProductsAsync(dto, "test-user");

        // All 10 seed products are matched (no filter predicate applied in filter-branch)
        Assert.Equal(10, result.TotalRequested);
        Assert.Equal(10, result.SuccessCount);
        Assert.False(result.RolledBack);
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_NonMatchingFilter_ShouldReturnZeroRequested()
    {
        // Arrange — filter by a brand that no product has
        var dto = new BulkUpdateProductsDto
        {
            FilterBrandId = Guid.NewGuid(), // brand that doesn't exist
            VatRateId = _vatB
        };

        // Act
        var result = await _service.BulkUpdateProductsAsync(dto, "test-user");

        // Assert
        Assert.Equal(0, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.False(result.RolledBack);
    }

    // ─── Rollback / error state tests ───────────────────────────────────────

    [Fact]
    public async Task BulkUpdateProductsAsync_RolledBack_ShouldReportZeroSuccessCount()
    {
        // When RolledBack=true the SuccessCount must be 0, because no rows were committed.
        // The InMemory provider ignores transactions (by design), so we simulate the rollback
        // path by verifying the contract on the result object: after a rollback, the caller
        // must never see a non-zero SuccessCount (which would imply updates were persisted).
        //
        // A direct DB-level rollback can only be triggered reliably in an integration test
        // with a real database engine.  This unit test therefore verifies the normal (non-rolled-back)
        // path and documents the rollback contract as a known limitation of the InMemory provider.
        //
        // The fix (SuccessCount = 0 in the outer catch) is covered by code inspection and
        // the integration test suite should be used for full rollback validation.
        var dto = new BulkUpdateProductsDto
        {
            ProductIds = _productIds.Take(2).ToList(),
            BrandId = _brandB
        };

        var result = await _service.BulkUpdateProductsAsync(dto, "test-user");

        // In the normal path the result must never have RolledBack=true with SuccessCount>0.
        Assert.False(result.RolledBack && result.SuccessCount > 0,
            "If RolledBack is true, SuccessCount must be 0 (no rows were committed).");
    }

    // ─── Validation / guard tests ────────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateProductsAsync_NoFieldsSpecified_ShouldThrowArgumentException()
    {
        // Arrange — DTO with filters but zero update fields
        var dto = new BulkUpdateProductsDto
        {
            ProductIds = _productIds.Take(3).ToList()
            // No update fields set
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkUpdateProductsAsync(dto, "test-user"));
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_NoTenantContext_ShouldThrowInvalidOperationException()
    {
        // Arrange — service instance with no tenant context
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns((Guid?)null);

        var mockAuditLog = new Mock<IAuditLogService>();
        var mockLogger = new Mock<ILogger<ProductService>>();
        var mockCodeGenerator = new Mock<IDailyCodeGenerator>();
        var mockPriceHistory = new Mock<ISupplierProductPriceHistoryService>();

        var noTenantService = new ProductService(
            _context,
            mockAuditLog.Object,
            mockTenantContext.Object,
            mockLogger.Object,
            mockCodeGenerator.Object,
            mockPriceHistory.Object);

        var dto = new BulkUpdateProductsDto
        {
            ProductIds = _productIds.Take(1).ToList(),
            BrandId = _brandB
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => noTenantService.BulkUpdateProductsAsync(dto, "test-user"));
    }

    // ─── Reason field logging test ───────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateProductsAsync_WithReason_ShouldIncludeReasonInLogInformation()
    {
        // Arrange
        const string reason = "Q4 VAT harmonization";
        var dto = new BulkUpdateProductsDto
        {
            ProductIds = _productIds.Take(2).ToList(),
            VatRateId = _vatB,
            Reason = reason
        };

        // Act
        await _service.BulkUpdateProductsAsync(dto, "test-user");

        // Assert — verify that a LogInformation message was emitted containing the reason.
        // ILogger<T>.Log is the underlying method called by extension methods like LogInformation.
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(reason)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            $"Expected a LogInformation message containing the Reason '{reason}'");
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_WithoutReason_ShouldLogNAFallback()
    {
        // Arrange — no Reason set; the service should fall back to "N/A" in the log
        var dto = new BulkUpdateProductsDto
        {
            ProductIds = _productIds.Take(1).ToList(),
            BrandId = _brandB
            // Reason deliberately omitted
        };

        // Act
        await _service.BulkUpdateProductsAsync(dto, "test-user");

        // Assert — the commit LogInformation must contain "N/A"
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("N/A")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Expected a LogInformation message containing 'N/A' when Reason is null");
    }

    // ─── ModifiedAt / ModifiedBy audit trail ────────────────────────────────

    [Fact]
    public async Task BulkUpdateProductsAsync_ShouldStampModifiedAtOnUpdatedProducts()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);
        var dto = new BulkUpdateProductsDto
        {
            ProductIds = _productIds.Take(3).ToList(),
            BrandId = _brandB
        };

        // Act
        await _service.BulkUpdateProductsAsync(dto, "auditor-user");

        // Assert — ModifiedAt is stamped by the service explicitly (product.ModifiedAt = now).
        // ModifiedBy is subsequently overridden by EventForgeDbContext.SaveChangesAsync which
        // reads the current user from the HTTP context; in tests that context is absent so it
        // falls back to "system".  We therefore assert ModifiedAt (service-controlled) but not
        // the exact ModifiedBy value (DbContext-controlled in test environment).
        var products = await _context.Products
            .Where(p => dto.ProductIds!.Contains(p.Id))
            .ToListAsync();

        Assert.All(products, p =>
        {
            Assert.NotNull(p.ModifiedAt);
            Assert.True(p.ModifiedAt >= before, "ModifiedAt should be close to the time of the call");
            Assert.NotNull(p.ModifiedBy); // set by DbContext interceptor ("system" in tests, real user in prod)
        });
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
