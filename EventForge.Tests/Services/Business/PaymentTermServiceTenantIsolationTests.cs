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
/// Cross-tenant isolation tests for <see cref="PaymentTermService"/>, verifying that
/// update/delete operations cannot mutate resources belonging to a different tenant.
/// Closes the gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 3).
/// </summary>
public class PaymentTermServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _paymentTermAId;

    public PaymentTermServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _paymentTermAId = Guid.NewGuid();

        _context.PaymentTerms.Add(new PaymentTerm
        {
            Id = _paymentTermAId,
            TenantId = _tenantAId,
            Name = "Term A",
            DueDays = 30,
            PaymentMethod = PaymentMethod.BankTransfer
        });

        _context.SaveChanges();
    }

    private PaymentTermService CreateService(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new PaymentTermService(
            _context,
            new Mock<IAuditLogService>().Object,
            mock.Object,
            new Mock<ILogger<PaymentTermService>>().Object);
    }

    [Fact]
    public async Task UpdatePaymentTermAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);
        var dto = new UpdatePaymentTermDto { Name = "Hacked", DueDays = 1, PaymentMethod = Prym.DTOs.Common.PaymentMethod.Cash };

        var result = await service.UpdatePaymentTermAsync(_paymentTermAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.PaymentTerms.AsNoTracking().FirstAsync(pt => pt.Id == _paymentTermAId);
        Assert.Equal("Term A", unchanged.Name);
    }

    [Fact]
    public async Task DeletePaymentTermAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeletePaymentTermAsync(_paymentTermAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.PaymentTerms.AsNoTracking().FirstAsync(pt => pt.Id == _paymentTermAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdatePaymentTermAsync_MissingTenant_Throws()
    {
        var service = CreateService(null);
        var dto = new UpdatePaymentTermDto { Name = "X", DueDays = 1, PaymentMethod = Prym.DTOs.Common.PaymentMethod.Cash };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdatePaymentTermAsync(_paymentTermAId, dto, "user"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
