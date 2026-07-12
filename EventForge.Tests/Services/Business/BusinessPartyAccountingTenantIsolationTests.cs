using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Business;

namespace EventForge.Tests.Services.Business;

/// <summary>
/// Cross-tenant isolation tests for the accounting-related methods of <see cref="BusinessPartyService"/>,
/// verifying that update/delete operations cannot mutate resources belonging to a different tenant.
/// Closes the gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 3).
/// </summary>
public class BusinessPartyAccountingTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _accountingAId;

    public BusinessPartyAccountingTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _accountingAId = Guid.NewGuid();

        _context.BusinessPartyAccountings.Add(new BusinessPartyAccounting
        {
            Id = _accountingAId,
            TenantId = _tenantAId,
            BusinessPartyId = Guid.NewGuid(),
            Iban = "IT00A0000000000000000000000",
            CreditLimit = 1000m
        });

        _context.SaveChanges();
    }

    private BusinessPartyService CreateService(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new BusinessPartyService(
            _context,
            new Mock<IAuditLogService>().Object,
            mock.Object,
            new Mock<ILogger<BusinessPartyService>>().Object);
    }

    [Fact]
    public async Task UpdateBusinessPartyAccountingAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);
        var dto = new UpdateBusinessPartyAccountingDto
        {
            BusinessPartyId = Guid.NewGuid(),
            Iban = "HACKED",
            CreditLimit = 999999m
        };

        var result = await service.UpdateBusinessPartyAccountingAsync(_accountingAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.BusinessPartyAccountings.AsNoTracking().FirstAsync(a => a.Id == _accountingAId);
        Assert.Equal("IT00A0000000000000000000000", unchanged.Iban);
    }

    [Fact]
    public async Task DeleteBusinessPartyAccountingAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteBusinessPartyAccountingAsync(_accountingAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.BusinessPartyAccountings.AsNoTracking().FirstAsync(a => a.Id == _accountingAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdateBusinessPartyAccountingAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);
        var dto = new UpdateBusinessPartyAccountingDto { BusinessPartyId = Guid.NewGuid(), Iban = "X" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateBusinessPartyAccountingAsync(_accountingAId, dto, "user"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
