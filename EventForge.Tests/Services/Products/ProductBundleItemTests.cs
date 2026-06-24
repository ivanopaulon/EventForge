using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.CodeGeneration;

using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Products;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for bundle item CRUD in <see cref="ProductService"/>:
/// Add, Update (A1 — ComponentProductName populated, A2 — duplicate guard),
/// and Remove.
/// </summary>
public class ProductBundleItemTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly ProductService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    // Entities seeded once per test-class instance
    private readonly Guid _bundleId;
    private readonly Guid _comp1Id;
    private readonly Guid _comp2Id;

    public ProductBundleItemTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns((Guid?)_tenantId);

        _service = new ProductService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<ProductService>>().Object,
            new Mock<IDailyCodeGenerator>().Object);

        _bundleId = Guid.NewGuid();
        _comp1Id = Guid.NewGuid();
        _comp2Id = Guid.NewGuid();

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.Products.AddRange(
            new Product { Id = _bundleId, TenantId = _tenantId, Name = "Bundle Alpha", Code = "BA001", IsBundle = true },
            new Product { Id = _comp1Id, TenantId = _tenantId, Name = "Component One", Code = "C001" },
            new Product { Id = _comp2Id, TenantId = _tenantId, Name = "Component Two", Code = "C002" }
        );
        _context.SaveChanges();
    }

    // ─── AddProductBundleItemAsync ───────────────────────────────────────────

    [Fact]
    public async Task AddProductBundleItemAsync_ValidRequest_ReturnsPopulatedDto()
    {
        var dto = new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp1Id,
            Quantity = 3
        };

        var result = await _service.AddProductBundleItemAsync(dto, "test-user");

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_bundleId, result.BundleProductId);
        Assert.Equal(_comp1Id, result.ComponentProductId);
        Assert.Equal(3, result.Quantity);
        // Navigation properties must be populated (A1 for Add)
        Assert.Equal("Component One", result.ComponentProductName);
        Assert.Equal("C001", result.ComponentProductCode);
    }

    [Fact]
    public async Task AddProductBundleItemAsync_SelfReference_ThrowsArgumentException()
    {
        var dto = new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _bundleId,
            Quantity = 1
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddProductBundleItemAsync(dto, "test-user"));
    }

    [Fact]
    public async Task AddProductBundleItemAsync_DuplicateComponent_ThrowsInvalidOperationException()
    {
        // Add first time — should succeed
        var dto = new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp1Id,
            Quantity = 1
        };
        await _service.AddProductBundleItemAsync(dto, "test-user");

        // Add same component again — should fail (BUG-2 guard)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddProductBundleItemAsync(dto, "test-user"));
    }

    [Fact]
    public async Task AddProductBundleItemAsync_NonBundleParent_ThrowsInvalidOperationException()
    {
        // _comp1Id is NOT a bundle
        var dto = new CreateProductBundleItemDto
        {
            BundleProductId = _comp1Id,
            ComponentProductId = _comp2Id,
            Quantity = 1
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddProductBundleItemAsync(dto, "test-user"));
    }

    [Fact]
    public async Task AddProductBundleItemAsync_ComponentInOtherTenant_ThrowsArgumentException()
    {
        var foreignProductId = Guid.NewGuid();
        _context.Products.Add(new Product
        {
            Id = foreignProductId,
            TenantId = Guid.NewGuid(), // different tenant
            Name = "Foreign Product",
            Code = "FP001"
        });
        await _context.SaveChangesAsync();

        var dto = new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = foreignProductId,
            Quantity = 1
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddProductBundleItemAsync(dto, "test-user"));
    }

    // ─── UpdateProductBundleItemAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdateProductBundleItemAsync_ChangesComponent_ReturnsPopulatedDto()
    {
        // Arrange — create an item first
        var created = await _service.AddProductBundleItemAsync(new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp1Id,
            Quantity = 1
        }, "test-user");

        // Act — change to comp2
        var updateDto = new UpdateProductBundleItemDto
        {
            ComponentProductId = _comp2Id,
            Quantity = 5
        };
        var result = await _service.UpdateProductBundleItemAsync(created.Id, updateDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_comp2Id, result.ComponentProductId);
        Assert.Equal(5, result.Quantity);
        // BUG-1 fix: ComponentProductName/Code must not be null after update
        Assert.Equal("Component Two", result.ComponentProductName);
        Assert.Equal("C002", result.ComponentProductCode);
    }

    [Fact]
    public async Task UpdateProductBundleItemAsync_ChangeToAlreadyPresentComponent_ThrowsInvalidOperationException()
    {
        // Arrange — add both components to the bundle
        await _service.AddProductBundleItemAsync(new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp1Id,
            Quantity = 1
        }, "test-user");
        var item2 = await _service.AddProductBundleItemAsync(new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp2Id,
            Quantity = 1
        }, "test-user");

        // Act — try to update item2's component to comp1 (already present in same bundle)
        var updateDto = new UpdateProductBundleItemDto
        {
            ComponentProductId = _comp1Id, // already in bundle as item1
            Quantity = 2
        };

        // Assert — BUG-2 fix: must reject duplicate component
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateProductBundleItemAsync(item2.Id, updateDto, "test-user"));
    }

    [Fact]
    public async Task UpdateProductBundleItemAsync_SameComponent_DoesNotThrowDuplicate()
    {
        // Updating quantity only (same ComponentProductId) must not trigger the duplicate guard
        var created = await _service.AddProductBundleItemAsync(new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp1Id,
            Quantity = 1
        }, "test-user");

        var updateDto = new UpdateProductBundleItemDto
        {
            ComponentProductId = _comp1Id, // unchanged
            Quantity = 10
        };

        var result = await _service.UpdateProductBundleItemAsync(created.Id, updateDto, "test-user");

        Assert.NotNull(result);
        Assert.Equal(10, result.Quantity);
    }

    [Fact]
    public async Task UpdateProductBundleItemAsync_NonExistentId_ReturnsNull()
    {
        var updateDto = new UpdateProductBundleItemDto
        {
            ComponentProductId = _comp1Id,
            Quantity = 1
        };

        var result = await _service.UpdateProductBundleItemAsync(Guid.NewGuid(), updateDto, "test-user");

        Assert.Null(result);
    }

    // ─── RemoveProductBundleItemAsync ─────────────────────────────────────────

    [Fact]
    public async Task RemoveProductBundleItemAsync_ExistingItem_ReturnsTrueAndSoftDeletes()
    {
        var created = await _service.AddProductBundleItemAsync(new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp1Id,
            Quantity = 1
        }, "test-user");

        var removed = await _service.RemoveProductBundleItemAsync(created.Id, "test-user");

        Assert.True(removed);

        // Item must be soft-deleted in the DB (bypass global filter with IgnoreQueryFilters)
        var inDb = await _context.ProductBundleItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(bi => bi.Id == created.Id);

        Assert.NotNull(inDb);
        Assert.True(inDb.IsDeleted);
        Assert.Equal("test-user", inDb.DeletedBy);
        Assert.NotNull(inDb.DeletedAt);
    }

    [Fact]
    public async Task RemoveProductBundleItemAsync_NonExistentId_ReturnsFalse()
    {
        var removed = await _service.RemoveProductBundleItemAsync(Guid.NewGuid(), "test-user");

        Assert.False(removed);
    }

    [Fact]
    public async Task RemoveProductBundleItemAsync_AllowsReAddAfterSoftDelete()
    {
        // Add and remove
        var created = await _service.AddProductBundleItemAsync(new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp1Id,
            Quantity = 1
        }, "test-user");
        await _service.RemoveProductBundleItemAsync(created.Id, "test-user");

        // Re-adding the same component should succeed because the previous row is soft-deleted
        var reAdded = await _service.AddProductBundleItemAsync(new CreateProductBundleItemDto
        {
            BundleProductId = _bundleId,
            ComponentProductId = _comp1Id,
            Quantity = 2
        }, "test-user");

        Assert.NotNull(reAdded);
        Assert.Equal(2, reAdded.Quantity);
    }

    public void Dispose() => _context.Dispose();
}
