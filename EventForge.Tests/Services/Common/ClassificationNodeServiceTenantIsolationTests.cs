using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Common;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;

namespace EventForge.Tests.Services.Common;

/// <summary>
/// Cross-tenant isolation tests for <see cref="ClassificationNodeService"/>, verifying that
/// get/update/delete operations cannot read or mutate resources belonging to a different tenant.
/// Closes the gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 3).
/// </summary>
public class ClassificationNodeServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _nodeAId;

    public ClassificationNodeServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _nodeAId = Guid.NewGuid();

        _context.ClassificationNodes.Add(new ClassificationNode
        {
            Id = _nodeAId,
            TenantId = _tenantAId,
            Code = "CAT01",
            Name = "Category A"
        });

        _context.SaveChanges();
    }

    private ClassificationNodeService CreateService(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new ClassificationNodeService(
            _context,
            new Mock<IAuditLogService>().Object,
            mock.Object,
            new Mock<ILogger<ClassificationNodeService>>().Object);
    }

    [Fact]
    public async Task GetClassificationNodeByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetClassificationNodeByIdAsync(_nodeAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateClassificationNodeAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);
        var dto = new UpdateClassificationNodeDto
        {
            Name = "Hacked",
            Type = ProductClassificationType.Category,
            Status = ProductClassificationNodeStatus.Active
        };

        var result = await service.UpdateClassificationNodeAsync(_nodeAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.ClassificationNodes.AsNoTracking().FirstAsync(n => n.Id == _nodeAId);
        Assert.Equal("Category A", unchanged.Name);
    }

    [Fact]
    public async Task DeleteClassificationNodeAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteClassificationNodeAsync(_nodeAId, "attacker", Array.Empty<byte>());

        Assert.False(result);
        var unchanged = await _context.ClassificationNodes.AsNoTracking().FirstAsync(n => n.Id == _nodeAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdateClassificationNodeAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);
        var dto = new UpdateClassificationNodeDto
        {
            Name = "X",
            Type = ProductClassificationType.Category,
            Status = ProductClassificationNodeStatus.Active
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateClassificationNodeAsync(_nodeAId, dto, "user"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
