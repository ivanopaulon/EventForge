using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Tests.Data;

/// <summary>
/// Unit tests to verify that Sales entities are excluded from automatic audit tracking
/// and don't cause DbUpdateConcurrencyException.
/// </summary>
[Trait("Category", "Unit")]
public class EventForgeDbContextSalesAuditTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantId = Guid.NewGuid();

    public EventForgeDbContextSalesAuditTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);
    }

    /// <summary>
    /// Test that SaleSession entities don't trigger automatic audit logs.
    /// Sales entities should use manual audit logging in SaleSessionService.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithSaleSession_DoesNotCreateAutomaticAuditLogs()
    {
        // Arrange
        var session = new SaleSession
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OperatorId = Guid.NewGuid(),
            PosId = Guid.NewGuid(),
            Status = SaleSessionStatus.Open,
            Currency = "EUR",
            OriginalTotal = 0,
            DiscountAmount = 0,
            FinalTotal = 0,
            TaxAmount = 0,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _ = _context.SaleSessions.Add(session);
        await _context.SaveChangesAsync();

        // Assert - No automatic audit logs should be created for Sales entities
        var auditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == session.Id)
            .ToListAsync();

        Assert.Empty(auditLogs);
    }

    /// <summary>
    /// Test that modifying SaleSession and adding SaleItem in same SaveChanges doesn't create automatic audit logs.
    /// This simulates the AddItemAsync scenario where session totals are updated along with item addition.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithSaleSessionAndItems_DoesNotCreateAutomaticAuditLogs()
    {
        // Arrange - Create a session and item together (like AddItemAsync does)
        var session = new SaleSession
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OperatorId = Guid.NewGuid(),
            PosId = Guid.NewGuid(),
            Status = SaleSessionStatus.Open,
            Currency = "EUR",
            OriginalTotal = 0,
            DiscountAmount = 0,
            FinalTotal = 0,
            TaxAmount = 0,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SaleSessionId = session.Id,
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            UnitPrice = 10.00m,
            Quantity = 2,
            TotalAmount = 20.00m,
            TaxRate = 22,
            TaxAmount = 4.40m,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act - Add both session and item in one operation
        session.Items.Add(item);
        
        // Simulate CalculateTotalsInline - update session totals
        session.OriginalTotal = 20.00m;
        session.FinalTotal = 20.00m;
        session.TaxAmount = 4.40m;

        _ = _context.SaleSessions.Add(session);
        
        // This should NOT throw DbUpdateConcurrencyException and should not create audit logs
        var exception = await Record.ExceptionAsync(async () => 
            await _context.SaveChangesAsync());

        // Assert
        Assert.Null(exception);
        
        // Verify no automatic audit logs were created for Sales entities
        var auditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == session.Id || log.EntityId == item.Id)
            .ToListAsync();

        Assert.Empty(auditLogs);
    }

    /// <summary>
    /// Test that SalePayment entities don't trigger automatic audit logs.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithSalePayment_DoesNotCreateAutomaticAuditLogs()
    {
        // Arrange - Create session first
        var session = new SaleSession
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OperatorId = Guid.NewGuid(),
            PosId = Guid.NewGuid(),
            Status = SaleSessionStatus.Open,
            Currency = "EUR",
            OriginalTotal = 100.00m,
            FinalTotal = 100.00m,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        _ = _context.SaleSessions.Add(session);
        await _context.SaveChangesAsync();

        var payment = new SalePayment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SaleSessionId = session.Id,
            PaymentMethodId = Guid.NewGuid(),
            Amount = 100.00m,
            Status = PaymentStatus.Completed,
            PaymentDate = DateTime.UtcNow,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _ = _context.SalePayments.Add(payment);
        await _context.SaveChangesAsync();

        // Assert - No automatic audit logs should be created
        var auditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == payment.Id)
            .ToListAsync();

        Assert.Empty(auditLogs);
    }

    /// <summary>
    /// Test that SessionNote entities don't trigger automatic audit logs.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithSessionNote_DoesNotCreateAutomaticAuditLogs()
    {
        // Arrange - Create session first
        var session = new SaleSession
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OperatorId = Guid.NewGuid(),
            PosId = Guid.NewGuid(),
            Status = SaleSessionStatus.Open,
            Currency = "EUR",
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        _ = _context.SaleSessions.Add(session);
        await _context.SaveChangesAsync();

        var note = new SessionNote
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SaleSessionId = session.Id,
            NoteFlagId = Guid.NewGuid(),
            Text = "Test note",
            CreatedByUserId = Guid.NewGuid(),
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _ = _context.SessionNotes.Add(note);
        await _context.SaveChangesAsync();

        // Assert - No automatic audit logs should be created
        var auditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == note.Id)
            .ToListAsync();

        Assert.Empty(auditLogs);
    }

    /// <summary>
    /// Test that non-Sales entities still get automatic audit logs.
    /// This ensures the exclusion only affects Sales entities.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_WithNonSalesEntity_StillCreatesAutomaticAuditLogs()
    {
        // Arrange - Create a Product entity (not a Sales entity)
        var product = new Server.Data.Entities.Products.Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "TEST001",
            Name = "Test Product",
            Status = Server.Data.Entities.Products.ProductStatus.Active,
            IsActive = true,
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _ = _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assert - Automatic audit logs should still be created for non-Sales entities
        var auditLogs = await _context.EntityChangeLogs
            .Where(log => log.EntityId == product.Id)
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        Assert.Contains(auditLogs, log => log.EntityName == "Product" && log.OperationType == "Insert");
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
