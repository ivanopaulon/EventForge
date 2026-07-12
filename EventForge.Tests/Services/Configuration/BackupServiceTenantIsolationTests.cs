using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Configuration;
using EventForge.Server.Hubs;
using EventForge.Server.Services.Configuration;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Configuration;

/// <summary>
/// Cross-tenant isolation tests for <see cref="BackupService"/>.
/// Verifies that <see cref="BackupService.DeleteBackupAsync"/> cannot delete a
/// backup operation belonging to a different tenant, closing the gap described in
/// PROMPT_22_TENANT_ISOLATION_CLOSEOUT.md.
/// </summary>
public class BackupServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _backupAId;

    public BackupServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _backupAId = Guid.NewGuid();

        _context.BackupOperations.Add(new BackupOperation
        {
            Id = _backupAId,
            TenantId = _tenantAId,
            Status = "Completed",
            StartedByUserId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    private BackupService CreateService(Guid? currentTenantId)
    {
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);
        tenantContextMock.Setup(x => x.CurrentUserId).Returns(Guid.NewGuid());
        tenantContextMock.Setup(x => x.IsSuperAdmin).Returns(true);

        return new BackupService(
            _context,
            tenantContextMock.Object,
            new Mock<IHubContext<AppHub>>().Object,
            new Mock<ILogger<BackupService>>().Object,
            new Mock<IWebHostEnvironment>().Object);
    }

    [Fact]
    public async Task DeleteBackupAsync_CrossTenant_ThrowsAndDoesNotDelete()
    {
        var service = CreateService(_tenantBId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteBackupAsync(_backupAId));

        var unchanged = await _context.BackupOperations.AsNoTracking().FirstOrDefaultAsync(b => b.Id == _backupAId);
        Assert.NotNull(unchanged);
    }

    [Fact]
    public async Task DeleteBackupAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteBackupAsync(_backupAId));
    }

    [Fact]
    public async Task DeleteBackupAsync_SameTenant_Succeeds()
    {
        var service = CreateService(_tenantAId);

        await service.DeleteBackupAsync(_backupAId);

        var deleted = await _context.BackupOperations.AsNoTracking().FirstOrDefaultAsync(b => b.Id == _backupAId);
        Assert.Null(deleted);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
