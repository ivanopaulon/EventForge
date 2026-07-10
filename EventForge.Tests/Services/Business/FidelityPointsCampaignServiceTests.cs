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
public class FidelityPointsCampaignServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly FidelityPointsCampaignService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public FidelityPointsCampaignServiceTests()
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
                It.IsAny<FidelityPointsCampaign>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FidelityPointsCampaign?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityChangeLog>());

        _service = new FidelityPointsCampaignService(_context, _auditLogServiceMock.Object, _tenantContextMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithOverlappingCampaign_ThrowsInvalidOperationException()
    {
        _context.FidelityPointsCampaigns.Add(new FidelityPointsCampaign
        {
            TenantId = _tenantId,
            Name = "Existing Campaign",
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 31),
            Multiplier = 2m,
            RoundingMode = FidelityPointsRoundingMode.Floor
        });
        await _context.SaveChangesAsync();

        var newCampaign = new FidelityPointsCampaign
        {
            Name = "Overlapping Campaign",
            StartDate = new DateTime(2026, 7, 15),
            EndDate = new DateTime(2026, 8, 15),
            Multiplier = 3m,
            RoundingMode = FidelityPointsRoundingMode.Ceiling,
            IsActive = true
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(newCampaign, "tester"));

        Assert.Contains("Existing Campaign", exception.Message);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
