using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EventForge.Tests.Services.Business;

[Trait("Category", "Unit")]
public class FidelityPointsBaseRateServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly FidelityPointsBaseRateService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public FidelityPointsBaseRateServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new EventForgeDbContext(options);
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _tenantContextMock = new Mock<ITenantContext>();
        _ = _tenantContextMock.Setup(x => x.CurrentTenantId).Returns(_tenantId);
        _ = _auditLogServiceMock
            .Setup(x => x.TrackEntityChangesAsync(
                It.IsAny<FidelityPointsBaseRate>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FidelityPointsBaseRate?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityChangeLog>());

        _service = new FidelityPointsBaseRateService(_context, _auditLogServiceMock.Object, _tenantContextMock.Object);
    }

    [Fact]
    public async Task CreateAsync_NewCurrentRate_AutoClosesPreviousCurrentRate()
    {
        var previousCurrentRate = new FidelityPointsBaseRate
        {
            TenantId = _tenantId,
            Rate = 1m,
            RoundingMode = FidelityPointsRoundingMode.Floor,
            EffectiveFrom = new DateTime(2026, 1, 1),
            EffectiveTo = null
        };

        _context.FidelityPointsBaseRates.Add(previousCurrentRate);
        await _context.SaveChangesAsync();

        var createdRate = await _service.CreateAsync(new FidelityPointsBaseRate
        {
            Rate = 2m,
            RoundingMode = FidelityPointsRoundingMode.Ceiling,
            EffectiveFrom = new DateTime(2026, 8, 1),
            EffectiveTo = null
        }, "tester");

        var reloadedPreviousRate = await _context.FidelityPointsBaseRates
            .AsNoTracking()
            .FirstAsync(rate => rate.Id == previousCurrentRate.Id);

        Assert.Equal(new DateTime(2026, 7, 31), reloadedPreviousRate.EffectiveTo);
        Assert.Null(createdRate.EffectiveTo);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
