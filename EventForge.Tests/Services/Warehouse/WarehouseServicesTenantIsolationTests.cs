using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Warehouse;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace EventForge.Tests.Services.Warehouse;

/// <summary>
/// Cross-tenant isolation tests for the last batch of Level 2 warehouse-related services:
/// <see cref="DocumentAnalyticsService"/>, <see cref="StorageFacilityService"/>,
/// <see cref="StorageLocationService"/> and <see cref="WarehouseFacade"/>.
/// Verifies that single-record update/delete operations cannot mutate resources
/// belonging to a different tenant, closing the gaps described in
/// PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 2).
/// </summary>
public class WarehouseServicesTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _facilityAId;
    private readonly Guid _locationAId;
    private readonly Guid _documentHeaderAId;
    private readonly Guid _documentRowAId;
    private readonly Guid _analyticsDocumentHeaderAId;

    public WarehouseServicesTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _facilityAId = Guid.NewGuid();
        _locationAId = Guid.NewGuid();
        _documentHeaderAId = Guid.NewGuid();
        _documentRowAId = Guid.NewGuid();
        _analyticsDocumentHeaderAId = Guid.NewGuid();

        SeedTenantAData();
    }

    private void SeedTenantAData()
    {
        _context.StorageFacilities.Add(new StorageFacility
        {
            Id = _facilityAId,
            TenantId = _tenantAId,
            Name = "Facility A",
            Code = "FAC-A"
        });

        _context.StorageLocations.Add(new StorageLocation
        {
            Id = _locationAId,
            TenantId = _tenantAId,
            WarehouseId = _facilityAId,
            Code = "LOC-A",
            Zone = "ZONE-A",
            Occupancy = 0
        });

        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = _documentHeaderAId,
            TenantId = _tenantAId,
            DocumentTypeId = Guid.NewGuid(),
            Number = "A-0001",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Currency = "EUR"
        });

        _context.DocumentRows.Add(new DocumentRow
        {
            Id = _documentRowAId,
            TenantId = _tenantAId,
            DocumentHeaderId = _documentHeaderAId,
            Description = "Row A",
            UnitPrice = 10m,
            Quantity = 1m
        });

        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = _analyticsDocumentHeaderAId,
            TenantId = _tenantAId,
            DocumentTypeId = Guid.NewGuid(),
            Number = "A-0002",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Currency = "EUR"
        });

        _context.SaveChanges();
    }

    private Mock<ITenantContext> CreateTenantContext(Guid? currentTenantId)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(currentTenantId);
        return mock;
    }

    private StorageFacilityService CreateFacilityService(Guid? currentTenantId)
    {
        return new StorageFacilityService(
            _context,
            new Mock<IAuditLogService>().Object,
            CreateTenantContext(currentTenantId).Object,
            new Mock<ILogger<StorageFacilityService>>().Object,
            new Mock<ICacheService>().Object);
    }

    private StorageLocationService CreateLocationService(Guid? currentTenantId)
    {
        return new StorageLocationService(
            _context,
            new Mock<IAuditLogService>().Object,
            CreateTenantContext(currentTenantId).Object,
            new Mock<ILogger<StorageLocationService>>().Object);
    }

    private DocumentAnalyticsService CreateAnalyticsService(Guid? currentTenantId)
    {
        return new DocumentAnalyticsService(
            _context,
            new Mock<IAuditLogService>().Object,
            CreateTenantContext(currentTenantId).Object,
            new Mock<ILogger<DocumentAnalyticsService>>().Object);
    }

    private WarehouseFacade CreateWarehouseFacade(Guid? currentTenantId)
    {
        return new WarehouseFacade(
            new Mock<IStorageFacilityService>().Object,
            new Mock<IStorageLocationService>().Object,
            new Mock<ILotService>().Object,
            new Mock<IStockService>().Object,
            new Mock<ISerialService>().Object,
            new Mock<IStockMovementService>().Object,
            new Mock<IDocumentHeaderService>().Object,
            new Mock<IProductService>().Object,
            new Mock<IInventoryBulkSeedService>().Object,
            new Mock<IInventoryDiagnosticService>().Object,
            new Mock<IStockReconciliationService>().Object,
            new Mock<IExportService>().Object,
            _context,
            CreateTenantContext(currentTenantId).Object,
            new Mock<ILogger<WarehouseFacade>>().Object);
    }

    private InventoryDiagnosticService CreateInventoryDiagnosticService(Guid? currentTenantId)
    {
        return new InventoryDiagnosticService(
            _context,
            CreateTenantContext(currentTenantId).Object,
            new Mock<ILogger<InventoryDiagnosticService>>().Object);
    }

    // ---- StorageFacilityService ----

    [Fact]
    public async Task UpdateStorageFacilityAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateFacilityService(_tenantBId);
        var dto = new UpdateStorageFacilityDto { Name = "Hacked" };

        var result = await service.UpdateStorageFacilityAsync(_facilityAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.StorageFacilities.AsNoTracking().FirstAsync(f => f.Id == _facilityAId);
        Assert.Equal("Facility A", unchanged.Name);
    }

    [Fact]
    public async Task DeleteStorageFacilityAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateFacilityService(_tenantBId);

        var result = await service.DeleteStorageFacilityAsync(_facilityAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.StorageFacilities.AsNoTracking().FirstAsync(f => f.Id == _facilityAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdateStorageFacilityAsync_MissingTenant_Throws()
    {
        var service = CreateFacilityService(null);
        var dto = new UpdateStorageFacilityDto { Name = "X" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateStorageFacilityAsync(_facilityAId, dto, "user"));
    }

    // ---- StorageLocationService ----

    [Fact]
    public async Task UpdateStorageLocationAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateLocationService(_tenantBId);
        var dto = new UpdateStorageLocationDto { Code = "HACKED" };

        var result = await service.UpdateStorageLocationAsync(_locationAId, dto, "attacker");

        Assert.Null(result);
        var unchanged = await _context.StorageLocations.AsNoTracking().FirstAsync(l => l.Id == _locationAId);
        Assert.Equal("LOC-A", unchanged.Code);
    }

    [Fact]
    public async Task DeleteStorageLocationAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateLocationService(_tenantBId);

        var result = await service.DeleteStorageLocationAsync(_locationAId, "attacker", Array.Empty<byte>());

        Assert.False(result);
        var unchanged = await _context.StorageLocations.AsNoTracking().FirstAsync(l => l.Id == _locationAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task UpdateOccupancyAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateLocationService(_tenantBId);

        var result = await service.UpdateOccupancyAsync(_locationAId, 5, "attacker");

        Assert.Null(result);
        var unchanged = await _context.StorageLocations.AsNoTracking().FirstAsync(l => l.Id == _locationAId);
        Assert.Equal(0, unchanged.Occupancy);
    }

    [Fact]
    public async Task GetStorageLocationByIdAsync_CrossTenant_ReturnsNull()
    {
        var service = CreateLocationService(_tenantBId);

        var result = await service.GetStorageLocationByIdAsync(_locationAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStorageLocationsAsync_CrossTenant_DoesNotReturnOtherTenantData()
    {
        var service = CreateLocationService(_tenantBId);

        var result = await service.GetStorageLocationsAsync(new PaginationParameters { Page = 1, PageSize = 50 });

        Assert.DoesNotContain(result.Items, l => l.Id == _locationAId);
    }

    [Fact]
    public async Task GetLocationsByWarehouseAsync_Paged_CrossTenant_DoesNotReturnOtherTenantData()
    {
        var service = CreateLocationService(_tenantBId);

        var result = await service.GetLocationsByWarehouseAsync(_facilityAId, new PaginationParameters { Page = 1, PageSize = 50 });

        Assert.DoesNotContain(result.Items, l => l.Id == _locationAId);
    }

    [Fact]
    public async Task GetLocationsByZoneAsync_CrossTenant_DoesNotReturnOtherTenantData()
    {
        var service = CreateLocationService(_tenantBId);

        var result = await service.GetLocationsByZoneAsync("ZONE-A", new PaginationParameters { Page = 1, PageSize = 50 });

        Assert.DoesNotContain(result.Items, l => l.Id == _locationAId);
    }

    // ---- DocumentAnalyticsService ----

    [Fact]
    public async Task GetDocumentAnalyticsAsync_MissingTenant_Throws()
    {
        var service = CreateAnalyticsService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDocumentAnalyticsAsync(_analyticsDocumentHeaderAId));
    }

    [Fact]
    public async Task GetDocumentAnalyticsAsync_CrossTenant_ReturnsNull()
    {
        var tenantAService = CreateAnalyticsService(_tenantAId);
        _ = await tenantAService.CreateOrUpdateAnalyticsAsync(_analyticsDocumentHeaderAId, "userA");

        var tenantBService = CreateAnalyticsService(_tenantBId);
        var result = await tenantBService.GetDocumentAnalyticsAsync(_analyticsDocumentHeaderAId);

        Assert.Null(result);
    }

    // ---- WarehouseFacade ----

    [Fact]
    public async Task DeleteInventoryRowAsync_CrossTenant_ReturnsFalse()
    {
        var facade = CreateWarehouseFacade(_tenantBId);

        var result = await facade.DeleteInventoryRowAsync(_documentRowAId, "attacker");

        Assert.False(result);
        var unchanged = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _documentRowAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task CancelInventoryDocumentAsync_CrossTenant_ReturnsFalse()
    {
        var facade = CreateWarehouseFacade(_tenantBId);

        var result = await facade.CancelInventoryDocumentAsync(_documentHeaderAId, "attacker");

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateInventoryRowAsync_CrossTenant_ReturnsFalse()
    {
        var facade = CreateWarehouseFacade(_tenantBId);

        var result = await facade.UpdateInventoryRowAsync(_documentRowAId, null, 99m, null, "hacked", "attacker");

        Assert.False(result);
        var unchanged = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _documentRowAId);
        Assert.Equal(1m, unchanged.Quantity);
    }

    [Fact]
    public async Task UpdateOrMergeInventoryRowAsync_CrossTenant_Throws()
    {
        var facade = CreateWarehouseFacade(_tenantBId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => facade.UpdateOrMergeInventoryRowAsync(_documentHeaderAId, _documentRowAId, 5m, null, "attacker"));
    }

    [Fact]
    public async Task UpdateDocumentHeaderFieldsAsync_CrossTenant_ReturnsFalse()
    {
        var facade = CreateWarehouseFacade(_tenantBId);

        var result = await facade.UpdateDocumentHeaderFieldsAsync(_documentHeaderAId, DateTime.UtcNow, null, "hacked", "attacker");

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateDocumentStatusesBatchAsync_CrossTenant_DoesNotUpdate()
    {
        var facade = CreateWarehouseFacade(_tenantBId);
        var updates = new List<(Guid DocumentId, DocumentStatus Status, string Notes)>
        {
            (_documentHeaderAId, DocumentStatus.Archived, "hacked")
        };

        await facade.UpdateDocumentStatusesBatchAsync(updates, "attacker");

        var unchanged = await _context.DocumentHeaders.AsNoTracking().FirstAsync(d => d.Id == _documentHeaderAId);
        Assert.Equal(DocumentStatus.Active, unchanged.Status);
    }

    [Fact]
    public async Task UpdateDocumentStatusesBatchAsync_MissingTenant_Throws()
    {
        var facade = CreateWarehouseFacade(null);
        var updates = new List<(Guid DocumentId, DocumentStatus Status, string Notes)>
        {
            (_documentHeaderAId, DocumentStatus.Archived, "notes")
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => facade.UpdateDocumentStatusesBatchAsync(updates, "user"));
    }

    [Fact]
    public async Task UpdateDocumentStatusesBatchAsync_SameTenant_Updates()
    {
        var facade = CreateWarehouseFacade(_tenantAId);
        var updates = new List<(Guid DocumentId, DocumentStatus Status, string Notes)>
        {
            (_documentHeaderAId, DocumentStatus.Archived, "closed")
        };

        await facade.UpdateDocumentStatusesBatchAsync(updates, "user");

        var updated = await _context.DocumentHeaders.AsNoTracking().FirstAsync(d => d.Id == _documentHeaderAId);
        Assert.Equal(DocumentStatus.Archived, updated.Status);
    }

    // ---- InventoryDiagnosticService ----

    [Fact]
    public async Task RemoveProblematicRowsAsync_CrossTenant_RemovesNothing()
    {
        var service = CreateInventoryDiagnosticService(_tenantBId);

        var removedCount = await service.RemoveProblematicRowsAsync(
            _documentHeaderAId, new List<Guid> { _documentRowAId }, "attacker");

        Assert.Equal(0, removedCount);
        var unchanged = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _documentRowAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task RemoveProblematicRowsAsync_MissingTenant_Throws()
    {
        var service = CreateInventoryDiagnosticService(null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RemoveProblematicRowsAsync(_documentHeaderAId, new List<Guid> { _documentRowAId }, "user"));
    }

    [Fact]
    public async Task RemoveProblematicRowsAsync_SameTenant_Removes()
    {
        var service = CreateInventoryDiagnosticService(_tenantAId);

        var removedCount = await service.RemoveProblematicRowsAsync(
            _documentHeaderAId, new List<Guid> { _documentRowAId }, "user");

        Assert.Equal(1, removedCount);
        var updated = await _context.DocumentRows.IgnoreQueryFilters().AsNoTracking().FirstAsync(r => r.Id == _documentRowAId);
        Assert.True(updated.IsDeleted);
    }

    [Fact]
    public async Task DiagnoseDocumentAsync_CrossTenant_ReturnsEmptyReport()
    {
        var service = CreateInventoryDiagnosticService(_tenantBId);

        var report = await service.DiagnoseDocumentAsync(_documentHeaderAId);

        Assert.Equal(0, report.TotalRows);
    }

    [Fact]
    public async Task AutoRepairDocumentAsync_CrossTenant_DoesNotModifyOtherTenantRows()
    {
        var service = CreateInventoryDiagnosticService(_tenantBId);
        var options = new InventoryAutoRepairOptionsDto { RemoveInvalidReferences = true };

        _ = await service.AutoRepairDocumentAsync(_documentHeaderAId, options, "attacker");

        var unchanged = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _documentRowAId);
        Assert.False(unchanged.IsDeleted);
    }

    [Fact]
    public async Task RepairRowAsync_CrossTenant_ReturnsFalse()
    {
        var service = CreateInventoryDiagnosticService(_tenantBId);
        var repairData = new InventoryRowRepairDto { NewNotes = "HACKED" };

        var success = await service.RepairRowAsync(_documentHeaderAId, _documentRowAId, repairData, "attacker");

        Assert.False(success);
        var unchanged = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _documentRowAId);
        Assert.NotEqual("HACKED", unchanged.Notes);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
