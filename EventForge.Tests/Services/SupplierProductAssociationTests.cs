using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Products;
using EventForge.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services;

/// <summary>
/// Integration tests for the supplier product association feature.
/// These tests verify the bulk association functionality.
/// </summary>
public class SupplierProductAssociationTests
{
    private readonly DbContextOptions<EventForgeDbContext> _dbContextOptions;

    public SupplierProductAssociationTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetProductsWithSupplierAssociation_ShouldReturnAllProducts_WithCorrectAssociationStatus()
    {
        // Arrange
        using var context = new EventForgeDbContext(_dbContextOptions);
        var service = CreateProductService(context);
        
        var supplierId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var product3Id = Guid.NewGuid();

        // Add products
        context.Products.AddRange(
            new Product { Id = product1Id, Name = "Product 1", Code = "P001" },
            new Product { Id = product2Id, Name = "Product 2", Code = "P002" },
            new Product { Id = product3Id, Name = "Product 3", Code = "P003" }
        );

        // Add association for product 1 only
        context.ProductSuppliers.Add(new ProductSupplier
        {
            Id = Guid.NewGuid(),
            ProductId = product1Id,
            SupplierId = supplierId,
            UnitCost = 10.50m,
            SupplierProductCode = "SUP-P001"
        });

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetProductsWithSupplierAssociationAsync(supplierId);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(3, resultList.Count);
        
        var product1Result = resultList.First(p => p.ProductId == product1Id);
        Assert.True(product1Result.IsAssociated);
        Assert.NotNull(product1Result.ProductSupplierId);
        Assert.Equal(10.50m, product1Result.UnitCost);
        Assert.Equal("SUP-P001", product1Result.SupplierProductCode);

        var product2Result = resultList.First(p => p.ProductId == product2Id);
        Assert.False(product2Result.IsAssociated);
        Assert.Null(product2Result.ProductSupplierId);

        var product3Result = resultList.First(p => p.ProductId == product3Id);
        Assert.False(product3Result.IsAssociated);
        Assert.Null(product3Result.ProductSupplierId);
    }

    [Fact]
    public async Task BulkUpdateProductSupplierAssociations_ShouldAddNewAssociations()
    {
        // Arrange
        using var context = new EventForgeDbContext(_dbContextOptions);
        var service = CreateProductService(context);
        
        var supplierId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        context.Products.AddRange(
            new Product { Id = product1Id, Name = "Product 1", Code = "P001" },
            new Product { Id = product2Id, Name = "Product 2", Code = "P002" }
        );
        await context.SaveChangesAsync();

        var productIds = new[] { product1Id, product2Id };

        // Act
        var count = await service.BulkUpdateProductSupplierAssociationsAsync(supplierId, productIds, "test-user");

        // Assert
        Assert.Equal(2, count);
        var associations = await context.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId && !ps.IsDeleted)
            .ToListAsync();
        Assert.Equal(2, associations.Count);
        Assert.Contains(associations, a => a.ProductId == product1Id);
        Assert.Contains(associations, a => a.ProductId == product2Id);
    }

    [Fact]
    public async Task BulkUpdateProductSupplierAssociations_ShouldRemoveUnselectedAssociations()
    {
        // Arrange
        using var context = new EventForgeDbContext(_dbContextOptions);
        var service = CreateProductService(context);
        
        var supplierId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var product3Id = Guid.NewGuid();

        context.Products.AddRange(
            new Product { Id = product1Id, Name = "Product 1", Code = "P001" },
            new Product { Id = product2Id, Name = "Product 2", Code = "P002" },
            new Product { Id = product3Id, Name = "Product 3", Code = "P003" }
        );

        // Add associations for products 1 and 2
        context.ProductSuppliers.AddRange(
            new ProductSupplier { Id = Guid.NewGuid(), ProductId = product1Id, SupplierId = supplierId },
            new ProductSupplier { Id = Guid.NewGuid(), ProductId = product2Id, SupplierId = supplierId }
        );
        await context.SaveChangesAsync();

        // Act - keep only product 1 and add product 3
        var productIds = new[] { product1Id, product3Id };
        await service.BulkUpdateProductSupplierAssociationsAsync(supplierId, productIds, "test-user");

        // Clear the change tracker to ensure we're querying fresh data
        context.ChangeTracker.Clear();

        // Assert
        var allAssociations = await context.ProductSuppliers
            .IgnoreQueryFilters()  // Ignore global IsDeleted filter
            .Where(ps => ps.SupplierId == supplierId)
            .ToListAsync();
        
        var activeAssociations = allAssociations.Where(ps => !ps.IsDeleted).ToList();
        Assert.Equal(2, activeAssociations.Count);
        Assert.Contains(activeAssociations, a => a.ProductId == product1Id);
        Assert.Contains(activeAssociations, a => a.ProductId == product3Id);

        var deletedAssociations = allAssociations.Where(ps => ps.IsDeleted).ToList();
        Assert.Single(deletedAssociations);
        Assert.Equal(product2Id, deletedAssociations[0].ProductId);
    }

    [Fact]
    public async Task BulkUpdateProductSupplierAssociations_ShouldPreserveExistingAssociations()
    {
        // Arrange
        using var context = new EventForgeDbContext(_dbContextOptions);
        var service = CreateProductService(context);
        
        var supplierId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        context.Products.AddRange(
            new Product { Id = product1Id, Name = "Product 1", Code = "P001" },
            new Product { Id = product2Id, Name = "Product 2", Code = "P002" }
        );

        // Add existing association with data
        var existingAssociation = new ProductSupplier
        {
            Id = Guid.NewGuid(),
            ProductId = product1Id,
            SupplierId = supplierId,
            UnitCost = 10.50m,
            SupplierProductCode = "SUP-P001",
            Preferred = true
        };
        context.ProductSuppliers.Add(existingAssociation);
        await context.SaveChangesAsync();

        // Act - submit with product 1 and 2
        var productIds = new[] { product1Id, product2Id };
        await service.BulkUpdateProductSupplierAssociationsAsync(supplierId, productIds, "test-user");

        // Assert
        var associations = await context.ProductSuppliers
            .Where(ps => ps.SupplierId == supplierId && !ps.IsDeleted)
            .ToListAsync();
        Assert.Equal(2, associations.Count);

        // Verify existing association is preserved with its data
        var preservedAssociation = associations.First(a => a.ProductId == product1Id);
        Assert.Equal(10.50m, preservedAssociation.UnitCost);
        Assert.Equal("SUP-P001", preservedAssociation.SupplierProductCode);
        Assert.True(preservedAssociation.Preferred);
    }

    private ProductService CreateProductService(EventForgeDbContext context)
    {
        var loggerMock = new Mock<ILogger<ProductService>>();
        var auditLogServiceMock = new Mock<EventForge.Server.Services.Audit.IAuditLogService>();
        var tenantContextMock = new Mock<EventForge.Server.Services.Tenants.ITenantContext>();
        
        // Setup tenant context to return a valid tenant ID
        tenantContextMock.Setup(x => x.CurrentTenantId).Returns((Guid?)Guid.NewGuid());
        
        return new ProductService(
            context,
            auditLogServiceMock.Object,
            tenantContextMock.Object,
            loggerMock.Object
        );
    }
}
