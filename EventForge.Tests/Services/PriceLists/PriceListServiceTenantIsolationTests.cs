using EventForge.Server.Data;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.PriceLists;

namespace EventForge.Tests.Services.PriceLists;

/// <summary>
/// Cross-tenant isolation tests for <see cref="PriceListService"/>, <see cref="PriceListBusinessPartyService"/>
/// and <see cref="PriceListBulkOperationsService"/>.
/// Verifies that single-record get/update/delete operations cannot read or mutate resources
/// belonging to a different tenant, closing the security gap described in
/// PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 1).
/// </summary>
public class PriceListServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _priceListAId;
    private readonly Guid _priceListEntryAId;
    private readonly Guid _businessPartyAId;

    public PriceListServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _priceListAId = Guid.NewGuid();
        _priceListEntryAId = Guid.NewGuid();
        _businessPartyAId = Guid.NewGuid();

        SeedTenantAData();
    }

    private void SeedTenantAData()
    {
        _context.PriceLists.Add(new PriceList
        {
            Id = _priceListAId,
            TenantId = _tenantAId,
            Name = "Price List A",
            Description = "Tenant A price list"
        });

        _context.PriceListEntries.Add(new PriceListEntry
        {
            Id = _priceListEntryAId,
            TenantId = _tenantAId,
            PriceListId = _priceListAId,
            Price = 10m
        });

        _context.BusinessParties.Add(new EventForge.Server.Data.Entities.Business.BusinessParty
        {
            Id = _businessPartyAId,
            TenantId = _tenantAId,
            Name = "Business Party A"
        });

        _context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantAId,
            PriceListId = _priceListAId,
            BusinessPartyId = _businessPartyAId,
            Status = PriceListBusinessPartyStatus.Active
        });

        _context.SaveChanges();
    }

    private PriceListService CreatePriceListService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new PriceListService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<PriceListService>>().Object,
            new Mock<Server.Services.UnitOfMeasures.IUnitConversionService>().Object,
            new Mock<IPriceListGenerationService>().Object,
            new Mock<IPriceCalculationService>().Object,
            new Mock<IPriceListBusinessPartyService>().Object,
            new Mock<IPriceListBulkOperationsService>().Object);
    }

    private PriceListBusinessPartyService CreateBusinessPartyService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new PriceListBusinessPartyService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<PriceListBusinessPartyService>>().Object);
    }

    private PriceListBulkOperationsService CreateBulkOperationsService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new PriceListBulkOperationsService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<PriceListBulkOperationsService>>().Object);
    }

    [Fact]
    public async Task GetPriceListByIdAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreatePriceListService(_tenantBId);

        var result = await service.GetPriceListByIdAsync(_priceListAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePriceListAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreatePriceListService(_tenantBId);

        var result = await service.UpdatePriceListAsync(_priceListAId, new UpdatePriceListDto
        {
            Name = "Hacked",
            Priority = 1
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.PriceLists.AsNoTracking().FirstAsync(pl => pl.Id == _priceListAId);
        Assert.Equal("Price List A", stillOriginal.Name);
    }

    [Fact]
    public async Task DeletePriceListAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreatePriceListService(_tenantBId);

        var result = await service.DeletePriceListAsync(_priceListAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.PriceLists.AsNoTracking().FirstAsync(pl => pl.Id == _priceListAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task GetPriceListEntryByIdAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreatePriceListService(_tenantBId);

        var result = await service.GetPriceListEntryByIdAsync(_priceListEntryAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePriceListEntryAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreatePriceListService(_tenantBId);

        var result = await service.UpdatePriceListEntryAsync(_priceListEntryAId, new UpdatePriceListEntryDto
        {
            Price = 999m
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.PriceListEntries.AsNoTracking().FirstAsync(e => e.Id == _priceListEntryAId);
        Assert.Equal(10m, stillOriginal.Price);
    }

    [Fact]
    public async Task RemovePriceListEntryAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreatePriceListService(_tenantBId);

        var result = await service.RemovePriceListEntryAsync(_priceListEntryAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.PriceListEntries.AsNoTracking().FirstAsync(e => e.Id == _priceListEntryAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task RemoveBusinessPartyAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateBusinessPartyService(_tenantBId);

        var result = await service.RemoveBusinessPartyAsync(_priceListAId, _businessPartyAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.PriceListBusinessParties.AsNoTracking()
            .FirstAsync(plbp => plbp.PriceListId == _priceListAId && plbp.BusinessPartyId == _businessPartyAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task PreviewBulkUpdateAsync_FromOtherTenant_Throws()
    {
        var service = CreateBulkOperationsService(_tenantBId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewBulkUpdateAsync(
            _priceListAId,
            new BulkPriceUpdateDto { Operation = Prym.DTOs.Common.BulkUpdateOperation.IncreaseByPercentage, Value = 10 }));
    }

    [Fact]
    public async Task BulkUpdatePricesAsync_FromOtherTenant_Throws()
    {
        var service = CreateBulkOperationsService(_tenantBId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkUpdatePricesAsync(
            _priceListAId,
            new BulkPriceUpdateDto { Operation = Prym.DTOs.Common.BulkUpdateOperation.IncreaseByPercentage, Value = 10 },
            "attacker"));

        var stillOriginal = await _context.PriceListEntries.AsNoTracking().FirstAsync(e => e.Id == _priceListEntryAId);
        Assert.Equal(10m, stillOriginal.Price);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
