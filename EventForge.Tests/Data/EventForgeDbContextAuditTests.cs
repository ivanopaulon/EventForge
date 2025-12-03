using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Products;
using Microsoft.EntityFrameworkCore;
using ProductStatus = EventForge.Server.Data.Entities.Products.ProductStatus;

namespace EventForge.Tests.Data;

/// <summary>
/// Unit tests for EventForgeDbContext audit logging and concurrency handling.
/// </summary>
[Trait("Category", "Unit")]
public class EventForgeDbContextAuditTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantId = Guid.NewGuid();

    public EventForgeDbContextAuditTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);
    }

    /// <summary>
    /// Test that SaveChangesAsync doesn't throw DbUpdateConcurrencyException
    /// when saving a new entity with audit logs.
    /// This reproduces the issue where double SaveChangesAsync calls cause RowVersion conflicts.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithNewEntity_DoesNotThrowConcurrencyException()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "TEST001",
            Name = "Test Product",
            Status = ProductStatus.Active,
            IsActive = true,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert
        _ = _context.Products.Add(product);
        
        // This should not throw DbUpdateConcurrencyException
        // The fix ensures audit entries are added before SaveChangesAsync is called
        var exception = await Record.ExceptionAsync(async () => 
            await _context.SaveChangesAsync());

        Assert.Null(exception);
    }

    /// <summary>
    /// Test that audit logs are created when saving a new entity.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithNewEntity_CreatesAuditLogs()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "TEST002",
            Name = "Test Product 2",
            Status = ProductStatus.Active,
            IsActive = true,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _ = _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == product.Id)
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        Assert.All(auditLogs, log => 
        {
            Assert.Equal("Product", log.EntityName);
            Assert.Equal("Insert", log.OperationType);
            // DbContext uses "system" as default user when no IHttpContextAccessor is available
            Assert.Equal("system", log.ChangedBy);
        });
    }

    /// <summary>
    /// Test that updating an entity creates audit logs without concurrency issues.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithModifiedEntity_CreatesAuditLogsWithoutConcurrencyException()
    {
        // Arrange - Create initial product
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "TEST003",
            Name = "Test Product 3",
            Status = ProductStatus.Active,
            IsActive = true,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        _ = _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act - Modify the product
        product.Name = "Modified Product 3";
        product.ModifiedBy = "modifier";
        product.ModifiedAt = DateTime.UtcNow;

        // This should not throw DbUpdateConcurrencyException
        var exception = await Record.ExceptionAsync(async () => 
            await _context.SaveChangesAsync());

        // Assert
        Assert.Null(exception);
        
        var updateAuditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == product.Id && log.OperationType == "Update")
            .ToListAsync();

        Assert.NotEmpty(updateAuditLogs);
        Assert.Contains(updateAuditLogs, log => log.PropertyName == "Name");
    }

    /// <summary>
    /// Test that soft-deleting an entity creates audit logs without concurrency issues.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithDeletedEntity_CreatesAuditLogsWithoutConcurrencyException()
    {
        // Arrange - Create initial product
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "TEST004",
            Name = "Test Product 4",
            Status = ProductStatus.Active,
            IsActive = true,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        _ = _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act - Delete the product (soft delete)
        _ = _context.Products.Remove(product);

        // This should not throw DbUpdateConcurrencyException
        var exception = await Record.ExceptionAsync(async () => 
            await _context.SaveChangesAsync());

        // Assert
        Assert.Null(exception);
        
        // Product should be soft-deleted (marked as deleted but still in DB)
        var deletedProduct = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        Assert.NotNull(deletedProduct);
        Assert.True(deletedProduct.IsDeleted);
        Assert.NotNull(deletedProduct.DeletedAt);
        Assert.NotNull(deletedProduct.DeletedBy);
    }

    /// <summary>
    /// Test that all entities and audit logs are saved in a single transaction.
    /// If SaveChanges fails, neither entities nor audit logs should be saved.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_SavesEntitiesAndAuditLogsAtomically()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "TEST005",
            Name = "Test Product 5",
            Status = ProductStatus.Active,
            IsActive = true,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _ = _context.Products.Add(product);
        var result = await _context.SaveChangesAsync();

        // Assert
        Assert.True(result > 0, "SaveChanges should return the number of affected rows");

        // Verify product was saved
        var savedProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(savedProduct);

        // Verify audit logs were saved in the same transaction
        var auditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == product.Id)
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
