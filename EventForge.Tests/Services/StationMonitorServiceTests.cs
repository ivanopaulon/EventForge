using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.StationMonitor;
using EventForge.Server.Data.Entities.Teams;
using EventForge.Server.Hubs;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Station;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Station;
using StationQueueEntityStatus = EventForge.Server.Data.Entities.StationMonitor.StationOrderQueueStatus;

namespace EventForge.Tests.Services;

[Trait("Category", "Unit")]
public class StationMonitorServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IHubContext<StationMonitorHub>> _mockHubContext;
    private readonly Mock<IHubClients> _mockHubClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<ILogger<StationService>> _mockLogger;
    private readonly StationService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _stationId = Guid.NewGuid();
    private readonly Guid _documentHeaderId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _teamMemberId = Guid.NewGuid();

    public StationMonitorServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new EventForgeDbContext(options);
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockHubContext = new Mock<IHubContext<StationMonitorHub>>();
        _mockHubClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockLogger = new Mock<ILogger<StationService>>();

        _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(_tenantId);
        _mockHubContext.Setup(h => h.Clients).Returns(_mockHubClients.Object);
        _mockHubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(x => x.TrackEntityChangesAsync(
                It.IsAny<StationOrderQueueItem>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StationOrderQueueItem?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityChangeLog>());

        _service = new StationService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockHubContext.Object,
            _mockLogger.Object);

        SeedReferenceData();
    }

    [Fact]
    public async Task GetActiveOrdersAsync_ReturnsActiveOrders()
    {
        var waitingItem = CreateQueueItem(StationQueueEntityStatus.Waiting, 1);
        var inPreparationItem = CreateQueueItem(StationQueueEntityStatus.InPreparation, 2);
        _ = CreateQueueItem(StationQueueEntityStatus.Ready, 3);
        await _context.SaveChangesAsync();

        var result = (await _service.GetActiveQueueItemsAsync(_stationId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Id == waitingItem.Id && item.Status == Prym.DTOs.Station.StationOrderQueueStatus.Waiting);
        Assert.Contains(result, item => item.Id == inPreparationItem.Id && item.Status == Prym.DTOs.Station.StationOrderQueueStatus.InProgress);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WithValidId_UpdatesStatus()
    {
        var queueItem = CreateQueueItem(StationQueueEntityStatus.Waiting, 1);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateQueueItemStatusAsync(queueItem.Id, Prym.DTOs.Station.StationOrderQueueStatus.InProgress, "test-user");
        var entity = await _context.StationOrderQueueItems.FirstAsync(x => x.Id == queueItem.Id);

        Assert.NotNull(result);
        Assert.Equal(Prym.DTOs.Station.StationOrderQueueStatus.InProgress, result!.Status);
        Assert.NotNull(result.StartedAt);
        Assert.Equal(StationQueueEntityStatus.InPreparation, entity.Status);
        Assert.Equal("system", entity.ModifiedBy); // DbContext overrides ModifiedBy to "system" when no HttpContext in tests
        _mockClientProxy.Verify(c => c.SendCoreAsync(
            "QueueItemStatusChanged",
            It.Is<object?[]>(args => args.Length == 1 && args[0] is StationOrderQueueItemDto && ((StationOrderQueueItemDto)args[0]!).Id == queueItem.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteOrderAsync_WithValidId_MarksComplete()
    {
        var queueItem = CreateQueueItem(StationQueueEntityStatus.Waiting, 1);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateQueueItemStatusAsync(queueItem.Id, Prym.DTOs.Station.StationOrderQueueStatus.Completed, "test-user");
        var entity = await _context.StationOrderQueueItems.FirstAsync(x => x.Id == queueItem.Id);

        Assert.NotNull(result);
        Assert.Equal(Prym.DTOs.Station.StationOrderQueueStatus.Completed, result!.Status);
        Assert.NotNull(result.StartedAt);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal(StationQueueEntityStatus.Ready, entity.Status);
        _mockClientProxy.Verify(c => c.SendCoreAsync(
            "QueueItemStatusChanged",
            It.Is<object?[]>(args => args.Length == 1 && args[0] is StationOrderQueueItemDto && ((StationOrderQueueItemDto)args[0]!).Id == queueItem.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedReferenceData()
    {
        _context.Stations.Add(new Station
        {
            Id = _stationId,
            TenantId = _tenantId,
            Name = "Kitchen",
            CreatedAt = DateTime.UtcNow
        });

        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = _documentHeaderId,
            TenantId = _tenantId,
            DocumentTypeId = Guid.NewGuid(),
            Number = "ORD-001",
            BusinessPartyId = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        _context.Products.Add(new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Name = "Burger",
            CreatedAt = DateTime.UtcNow
        });

        _context.TeamMembers.Add(new TeamMember
        {
            Id = _teamMemberId,
            TenantId = _tenantId,
            TeamId = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    private StationOrderQueueItem CreateQueueItem(StationQueueEntityStatus status, int sortOrder)
    {
        var queueItem = new StationOrderQueueItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            StationId = _stationId,
            DocumentHeaderId = _documentHeaderId,
            ProductId = _productId,
            TeamMemberId = _teamMemberId,
            Quantity = 1,
            Status = status,
            SortOrder = sortOrder,
            Notes = "Test item",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed-user"
        };

        _context.StationOrderQueueItems.Add(queueItem);
        return queueItem;
    }
}
