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
/// Cross-tenant isolation tests for <see cref="ProductService"/>.
/// Verifies that single-record get/update/delete operations cannot read or mutate
/// resources belonging to a different tenant, closing the security gap described in
/// PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 1).
/// </summary>
public class ProductServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _productAId;
    private readonly Guid _productCodeAId;
    private readonly Guid _productUnitAId;

    public ProductServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _productAId = Guid.NewGuid();
        _productCodeAId = Guid.NewGuid();
        _productUnitAId = Guid.NewGuid();

        SeedTenantAData();
    }

    private void SeedTenantAData()
    {
        _context.Products.Add(new Product
        {
            Id = _productAId,
            TenantId = _tenantAId,
            Name = "Product A",
            Code = "PROD-A"
        });

        _context.ProductCodes.Add(new ProductCode
        {
            Id = _productCodeAId,
            TenantId = _tenantAId,
            ProductId = _productAId,
            CodeType = "EAN",
            Code = "1234567890123"
        });

        _context.ProductUnits.Add(new ProductUnit
        {
            Id = _productUnitAId,
            TenantId = _tenantAId,
            ProductId = _productAId,
            UnitOfMeasureId = Guid.NewGuid(),
            ConversionFactor = 1m,
            UnitType = "Base"
        });

        _context.SaveChanges();
    }

    private ProductService CreateService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new ProductService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<ProductService>>().Object,
            new Mock<IDailyCodeGenerator>().Object);
    }

    [Fact]
    public async Task UpdateProductAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.UpdateProductAsync(_productAId, new UpdateProductDto
        {
            Name = "Hacked",
            Status = Prym.DTOs.Common.ProductStatus.Active
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == _productAId);
        Assert.Equal("Product A", stillOriginal.Name);
    }

    [Fact]
    public async Task DeleteProductAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteProductAsync(_productAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == _productAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task UpdateProductCodeAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.UpdateProductCodeAsync(_productCodeAId, new UpdateProductCodeDto
        {
            CodeType = "EAN",
            Code = "9999999999999",
            Status = Prym.DTOs.Common.ProductCodeStatus.Active
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.ProductCodes.AsNoTracking().FirstAsync(c => c.Id == _productCodeAId);
        Assert.Equal("1234567890123", stillOriginal.Code);
    }

    [Fact]
    public async Task RemoveProductCodeAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.RemoveProductCodeAsync(_productCodeAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.ProductCodes.AsNoTracking().FirstAsync(c => c.Id == _productCodeAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task UpdateProductUnitAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.UpdateProductUnitAsync(_productUnitAId, new UpdateProductUnitDto
        {
            UnitOfMeasureId = Guid.NewGuid(),
            ConversionFactor = 99m,
            UnitType = "Pack",
            Status = Prym.DTOs.Common.ProductUnitStatus.Active
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.ProductUnits.AsNoTracking().FirstAsync(u => u.Id == _productUnitAId);
        Assert.Equal(1m, stillOriginal.ConversionFactor);
    }

    [Fact]
    public async Task RemoveProductUnitAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.RemoveProductUnitAsync(_productUnitAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.ProductUnits.AsNoTracking().FirstAsync(u => u.Id == _productUnitAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task GetProductByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetProductByIdAsync(_productAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_SameTenant_ReturnsResult()
    {
        var service = CreateService(_tenantAId);

        var result = await service.GetProductByIdAsync(_productAId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetProductByIdAsync(_productAId));
    }

    [Fact]
    public async Task GetProductDetailAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetProductDetailAsync(_productAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductDetailAsync_SameTenant_ReturnsResult()
    {
        var service = CreateService(_tenantAId);

        var result = await service.GetProductDetailAsync(_productAId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProductDetailAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetProductDetailAsync(_productAId));
    }

    [Fact]
    public async Task GetProductCodeByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetProductCodeByIdAsync(_productCodeAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductCodeByIdAsync_SameTenant_ReturnsResult()
    {
        var service = CreateService(_tenantAId);

        var result = await service.GetProductCodeByIdAsync(_productCodeAId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProductCodeByIdAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetProductCodeByIdAsync(_productCodeAId));
    }

    [Fact]
    public async Task GetProductByCodeAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetProductByCodeAsync("1234567890123");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByCodeAsync_SameTenant_ReturnsResult()
    {
        var service = CreateService(_tenantAId);

        var result = await service.GetProductByCodeAsync("1234567890123");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProductByCodeAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetProductByCodeAsync("1234567890123"));
    }

    [Fact]
    public async Task GetProductUnitByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetProductUnitByIdAsync(_productUnitAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductUnitByIdAsync_SameTenant_ReturnsResult()
    {
        var service = CreateService(_tenantAId);

        var result = await service.GetProductUnitByIdAsync(_productUnitAId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProductUnitByIdAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetProductUnitByIdAsync(_productUnitAId));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
