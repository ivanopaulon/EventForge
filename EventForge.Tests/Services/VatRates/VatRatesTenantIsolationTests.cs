using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Common;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.VatRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;
using Prym.DTOs.VatRates;

namespace EventForge.Tests.Services.VatRates;

/// <summary>
/// Cross-tenant isolation tests for <see cref="VatNatureService"/> and <see cref="VatRateService"/>,
/// verifying that get/update/delete operations cannot read or mutate resources belonging to a
/// different tenant. Closes the gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 3).
/// </summary>
public class VatRatesTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _vatNatureAId;
    private readonly Guid _vatRateAId;

    public VatRatesTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _vatNatureAId = Guid.NewGuid();
        _vatRateAId = Guid.NewGuid();

        _context.VatNatures.Add(new VatNature
        {
            Id = _vatNatureAId,
            TenantId = _tenantAId,
            Code = "N1",
            Name = "Escluse ex art. 15"
        });

        _context.VatRates.Add(new VatRate
        {
            Id = _vatRateAId,
            TenantId = _tenantAId,
            Name = "VAT 22%",
            Percentage = 22m
        });

        _context.SaveChanges();
    }

    private VatNatureService CreateNatureService(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new VatNatureService(
            _context,
            new Mock<IAuditLogService>().Object,
            mock.Object,
            new Mock<ILogger<VatNatureService>>().Object,
            new Mock<ICacheService>().Object);
    }

    private VatRateService CreateRateService(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new VatRateService(
            _context,
            new Mock<IAuditLogService>().Object,
            mock.Object,
            new Mock<ILogger<VatRateService>>().Object);
    }

    // ---- VatNatureService ----

    [Fact]
    public async Task GetVatNatureByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateNatureService(_tenantBId);

        var result = await service.GetVatNatureByIdAsync(_vatNatureAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateVatNatureAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateNatureService(_tenantBId);
        var dto = new UpdateVatNatureDto { Code = "N9", Name = "Hacked" };

        var result = await service.UpdateVatNatureAsync(_vatNatureAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.VatNatures.AsNoTracking().FirstAsync(v => v.Id == _vatNatureAId);
        Assert.Equal("Escluse ex art. 15", unchanged.Name);
    }

    [Fact]
    public async Task DeleteVatNatureAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateNatureService(_tenantBId);

        var result = await service.DeleteVatNatureAsync(_vatNatureAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.VatNatures.AsNoTracking().FirstAsync(v => v.Id == _vatNatureAId);
        Assert.False(unchanged.IsDeleted);
    }

    // ---- VatRateService ----

    [Fact]
    public async Task GetVatRateByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateRateService(_tenantBId);

        var result = await service.GetVatRateByIdAsync(_vatRateAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateVatRateAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateRateService(_tenantBId);
        var dto = new UpdateVatRateDto { Name = "Hacked", Percentage = 0m, Status = VatRateStatus.Active };

        var result = await service.UpdateVatRateAsync(_vatRateAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.VatRates.AsNoTracking().FirstAsync(v => v.Id == _vatRateAId);
        Assert.Equal("VAT 22%", unchanged.Name);
    }

    [Fact]
    public async Task DeleteVatRateAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateRateService(_tenantBId);

        var result = await service.DeleteVatRateAsync(_vatRateAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.VatRates.AsNoTracking().FirstAsync(v => v.Id == _vatRateAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdateVatRateAsync_MissingTenant_Throws()
    {
        var service = CreateRateService(null);
        var dto = new UpdateVatRateDto { Name = "X", Percentage = 0m, Status = VatRateStatus.Active };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateVatRateAsync(_vatRateAId, dto, "user"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
