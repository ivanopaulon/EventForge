using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Common;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.UnitOfMeasures;

namespace EventForge.Tests.Services.UnitOfMeasures;

/// <summary>
/// Cross-tenant isolation tests for <see cref="UMService"/>, verifying that
/// update/delete operations cannot mutate resources belonging to a different tenant.
/// Closes the gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 3).
/// </summary>
public class UMServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _umAId;

    public UMServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _umAId = Guid.NewGuid();

        _context.UMs.Add(new UM
        {
            Id = _umAId,
            TenantId = _tenantAId,
            Name = "Kilogram",
            Symbol = "kg"
        });

        _context.SaveChanges();
    }

    private UMService CreateService(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new UMService(
            _context,
            new Mock<IAuditLogService>().Object,
            mock.Object,
            new Mock<ILogger<UMService>>().Object);
    }

    [Fact]
    public async Task UpdateUMAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);
        var dto = new UpdateUMDto { Name = "Hacked", Description = null, IsDefault = false };

        var result = await service.UpdateUMAsync(_umAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.UMs.AsNoTracking().FirstAsync(u => u.Id == _umAId);
        Assert.Equal("Kilogram", unchanged.Name);
    }

    [Fact]
    public async Task DeleteUMAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteUMAsync(_umAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.UMs.AsNoTracking().FirstAsync(u => u.Id == _umAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdateUMAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);
        var dto = new UpdateUMDto { Name = "X", Description = null, IsDefault = false };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateUMAsync(_umAId, dto, "user"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
