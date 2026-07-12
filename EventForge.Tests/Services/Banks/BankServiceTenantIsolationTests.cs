using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Common;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Banks;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Banks;

namespace EventForge.Tests.Services.Banks;

/// <summary>
/// Cross-tenant isolation tests for <see cref="BankService"/>, verifying that single-record
/// get/update/delete operations cannot read or mutate resources belonging to a different tenant.
/// Closes the gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 3).
/// </summary>
public class BankServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _bankAId;

    public BankServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _bankAId = Guid.NewGuid();

        _context.Banks.Add(new Bank
        {
            Id = _bankAId,
            TenantId = _tenantAId,
            Name = "Bank A",
            Code = "BNKA"
        });

        _context.SaveChanges();
    }

    private BankService CreateService(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new BankService(
            _context,
            new Mock<IAuditLogService>().Object,
            mock.Object,
            new Mock<ILogger<BankService>>().Object);
    }

    [Fact]
    public async Task GetBankByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.GetBankByIdAsync(_bankAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateBankAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);
        var dto = new UpdateBankDto { Name = "Hacked" };

        var result = await service.UpdateBankAsync(_bankAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.Banks.AsNoTracking().FirstAsync(b => b.Id == _bankAId);
        Assert.Equal("Bank A", unchanged.Name);
    }

    [Fact]
    public async Task DeleteBankAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteBankAsync(_bankAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.Banks.AsNoTracking().FirstAsync(b => b.Id == _bankAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdateBankAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);
        var dto = new UpdateBankDto { Name = "X" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateBankAsync(_bankAId, dto, "user"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
