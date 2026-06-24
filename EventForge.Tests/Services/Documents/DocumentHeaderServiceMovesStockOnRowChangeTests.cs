using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Warehouse;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Audit;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;
using Prym.DTOs.SuperAdmin;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Tests for the MovesStockOnRowChange feature:
/// Documents whose type has MovesStockOnRowChange = true should generate/replace/delete
/// stock movements on every row add/update/delete, regardless of document status.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentHeaderServiceMovesStockOnRowChangeTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IStockMovementService> _stockMovementService = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly Mock<IDocumentCounterService> _documentCounterService = new();
    private readonly Mock<IUnitConversionService> _unitConversionService = new();
    private readonly Mock<ILogger<DocumentHeaderService>> _logger = new();
    private readonly StubAuditLogService _auditLogService = new();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _storageLocationId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public DocumentHeaderServiceMovesStockOnRowChangeTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _tenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        SeedBaseData();
    }

    private void SeedBaseData()
    {
        var warehouse = new StorageFacility
        {
            Id = _warehouseId,
            TenantId = _tenantId,
            Name = "Main Warehouse",
            Code = "WH001",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StorageFacilities.Add(warehouse);

        var storageLocation = new StorageLocation
        {
            Id = _storageLocationId,
            TenantId = _tenantId,
            WarehouseId = _warehouseId,
            Code = "LOC-001",
            Description = "Test Location",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.StorageLocations.Add(storageLocation);

        var product = new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Code = "PROD-001",
            Name = "Test Product",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Products.Add(product);

        _context.SaveChanges();
    }

    private DocumentHeaderService CreateService()
        => new(
            _context,
            _auditLogService,
            _tenantContext.Object,
            _documentCounterService.Object,
            _stockMovementService.Object,
            _unitConversionService.Object,
            _logger.Object);

    private (Guid docTypeId, Guid docHeaderId) SeedDocumentWithLiveMode(
        bool movesStockOnRowChange,
        bool isStockIncrease = false,
        DocumentStatus status = DocumentStatus.Active)
    {
        var docTypeId = Guid.NewGuid();
        var docHeaderId = Guid.NewGuid();

        _context.DocumentTypes.Add(new DocumentType
        {
            Id = docTypeId,
            TenantId = _tenantId,
            Name = "Test Type",
            Code = "TST",
            IsStockIncrease = isStockIncrease,
            MovesStockOnRowChange = movesStockOnRowChange,
            CreatesStockMovements = false,
            DefaultWarehouseId = _warehouseId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = docHeaderId,
            TenantId = _tenantId,
            DocumentTypeId = docTypeId,
            Number = "DOC-001",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _context.SaveChanges();
        return (docTypeId, docHeaderId);
    }

    private void SetupOutboundMovement()
    {
        _stockMovementService
            .Setup(x => x.ProcessOutboundMovementAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(),
                It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Prym.DTOs.Warehouse.StockMovementDto());
    }

    private void SetupInboundMovement()
    {
        _stockMovementService
            .Setup(x => x.ProcessInboundMovementAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(),
                It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Prym.DTOs.Warehouse.StockMovementDto());
    }

    // -------------------------------------------------------------------------
    // AddDocumentRowAsync — live mode
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddRow_WithMovesStockOnRowChange_ActiveDocument_CreatesOutboundMovement()
    {
        // Arrange
        var (_, docHeaderId) = SeedDocumentWithLiveMode(movesStockOnRowChange: true, isStockIncrease: false);
        var service = CreateService();
        SetupOutboundMovement();

        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = docHeaderId,
            ProductId = _productId,
            Description = "Test row",
            Quantity = 5,
            UnitPrice = 10m,
            VatRate = 22,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = await service.AddDocumentRowAsync(createDto, "tester");

        // Assert: outbound movement created for live mode + outbound doc type
        _stockMovementService.Verify(x => x.ProcessOutboundMovementAsync(
            _productId, _storageLocationId, 5m,
            It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            docHeaderId, It.IsAny<Guid?>(),
            It.IsAny<string?>(), "tester",
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AddRow_WithMovesStockOnRowChange_ActiveDocument_InboundType_CreatesInboundMovement()
    {
        // Arrange
        var (_, docHeaderId) = SeedDocumentWithLiveMode(movesStockOnRowChange: true, isStockIncrease: true);
        var service = CreateService();
        SetupInboundMovement();

        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = docHeaderId,
            ProductId = _productId,
            Description = "Test row",
            Quantity = 3,
            UnitPrice = 20m,
            VatRate = 22,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        _ = await service.AddDocumentRowAsync(createDto, "tester");

        // Assert: inbound movement because IsStockIncrease = true
        _stockMovementService.Verify(x => x.ProcessInboundMovementAsync(
            _productId, _storageLocationId, 3m,
            It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            docHeaderId, It.IsAny<Guid?>(),
            It.IsAny<string?>(), "tester",
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRow_WithMovesStockOnRowChangeFalse_ActiveDocument_DoesNotCreateMovement()
    {
        // Arrange — standard document type, NOT live mode
        var (_, docHeaderId) = SeedDocumentWithLiveMode(movesStockOnRowChange: false, isStockIncrease: false);
        var service = CreateService();

        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = docHeaderId,
            ProductId = _productId,
            Description = "Test row",
            Quantity = 5,
            UnitPrice = 10m,
            VatRate = 22,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        _ = await service.AddDocumentRowAsync(createDto, "tester");

        // Assert: no movement at all (document is Active and type is standard)
        _stockMovementService.Verify(x => x.ProcessOutboundMovementAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(),
            It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        _stockMovementService.Verify(x => x.ProcessInboundMovementAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(),
            It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddRow_WithMovesStockOnRowChange_ArchivedDocument_StillCreatesMovement()
    {
        // Arrange — live mode document that happens to be archived
        var (_, docHeaderId) = SeedDocumentWithLiveMode(
            movesStockOnRowChange: true,
            isStockIncrease: false,
            status: DocumentStatus.Archived);
        var service = CreateService();
        SetupOutboundMovement();

        var createDto = new CreateDocumentRowDto
        {
            DocumentHeaderId = docHeaderId,
            ProductId = _productId,
            Description = "Row in archived doc",
            Quantity = 2,
            UnitPrice = 50m,
            VatRate = 22,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        _ = await service.AddDocumentRowAsync(createDto, "tester");

        // Assert: movement is still created even when document is Archived
        _stockMovementService.Verify(x => x.ProcessOutboundMovementAsync(
            _productId, _storageLocationId, 2m,
            It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            docHeaderId, It.IsAny<Guid?>(),
            It.IsAny<string?>(), "tester",
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateDocumentRowAsync — live mode: replace (delete + create), not delta
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateRow_WithMovesStockOnRowChange_ReplacesMovement_NotDelta()
    {
        // Arrange
        var (_, docHeaderId) = SeedDocumentWithLiveMode(movesStockOnRowChange: true, isStockIncrease: false);
        var service = CreateService();

        var uomId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = rowId,
            TenantId = _tenantId,
            DocumentHeaderId = docHeaderId,
            ProductId = _productId,
            Description = "Test row",
            Quantity = 5,
            UnitPrice = 10m,
            VatRate = 22,
            UnitOfMeasureId = uomId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "tester"
        });
        await _context.SaveChangesAsync();

        _stockMovementService
            .Setup(x => x.DeleteMovementsForRowAsync(rowId, "tester", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetupOutboundMovement();

        var updateDto = new UpdateDocumentRowDto
        {
            Description = "Updated row",
            Quantity = 8,
            UnitPrice = 10m,
            VatRate = 22,
            UnitOfMeasureId = uomId
        };

        // Act
        _ = await service.UpdateDocumentRowAsync(rowId, updateDto, "tester");

        // Assert: existing movements deleted first, then new movement created with current quantity
        _stockMovementService.Verify(x => x.DeleteMovementsForRowAsync(rowId, "tester", It.IsAny<CancellationToken>()), Times.Once);
        _stockMovementService.Verify(x => x.ProcessOutboundMovementAsync(
            _productId, _storageLocationId, 8m,
            It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            docHeaderId, rowId,
            It.IsAny<string?>(), "tester",
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteDocumentRowAsync — live mode: soft-delete movement, no compensating
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteRow_WithMovesStockOnRowChange_DeletesMovementDirectly_NoCompensating()
    {
        // Arrange
        var (_, docHeaderId) = SeedDocumentWithLiveMode(movesStockOnRowChange: true, isStockIncrease: false);
        var service = CreateService();

        var rowId = Guid.NewGuid();
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = rowId,
            TenantId = _tenantId,
            DocumentHeaderId = docHeaderId,
            ProductId = _productId,
            Description = "Row to delete",
            Quantity = 5,
            UnitPrice = 10m,
            VatRate = 22,
            UnitOfMeasureId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "tester"
        });
        await _context.SaveChangesAsync();

        _stockMovementService
            .Setup(x => x.DeleteMovementsForRowAsync(rowId, "tester", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteDocumentRowAsync(rowId, "tester");

        // Assert: movements soft-deleted directly, no compensating movement created
        Assert.True(result);
        _stockMovementService.Verify(x => x.DeleteMovementsForRowAsync(rowId, "tester", It.IsAny<CancellationToken>()), Times.Once);
        _stockMovementService.Verify(x => x.ProcessOutboundMovementAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(),
            It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        _stockMovementService.Verify(x => x.ProcessInboundMovementAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(),
            It.IsAny<decimal?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Stub for IAuditLogService (generic method requires concrete stub, not Moq)
    // -------------------------------------------------------------------------

    private sealed class StubAuditLogService : IAuditLogService
    {
        public Task<EntityChangeLog> LogEntityChangeAsync(string entityName, Guid entityId, string propertyName, string operationType, string? oldValue, string? newValue, string changedBy, string? entityDisplayName = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new EntityChangeLog());

        public Task<IEnumerable<EntityChangeLog>> GetEntityLogsAsync(Guid entityId, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());

        public Task<IEnumerable<EntityChangeLog>> GetEntityTypeLogsAsync(string entityName, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());

        public Task<IEnumerable<EntityChangeLog>> GetLogsInDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());

        public Task<IEnumerable<EntityChangeLog>> GetUserLogsAsync(string username, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());

        public Task<IEnumerable<EntityChangeLog>> GetLogsAsync(System.Linq.Expressions.Expression<Func<EntityChangeLog, bool>>? filter = null, System.Linq.Expressions.Expression<Func<EntityChangeLog, object>>? orderBy = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());

        public Task<IEnumerable<EntityChangeLog>> TrackEntityChangesAsync<TEntity>(TEntity entity, string operationType, string changedBy, TEntity? originalValues = null, CancellationToken cancellationToken = default) where TEntity : AuditableEntity
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());

        public Task<PagedResult<EntityChangeLog>> GetPagedLogsAsync(AuditLogQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<EntityChangeLog> { Items = Enumerable.Empty<EntityChangeLog>(), TotalCount = 0, Page = 1, PageSize = 10 });

        public Task<EntityChangeLog?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<EntityChangeLog?>(null);

        public Task<PagedResult<Prym.DTOs.Audit.AuditTrailResponseDto>> SearchAuditTrailAsync(Prym.DTOs.Audit.AuditTrailSearchDto searchDto, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<Prym.DTOs.Audit.AuditTrailResponseDto> { Items = Enumerable.Empty<Prym.DTOs.Audit.AuditTrailResponseDto>(), TotalCount = 0, Page = 1, PageSize = 10 });

        public Task<Prym.DTOs.Audit.AuditTrailStatisticsDto> GetAuditTrailStatisticsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new Prym.DTOs.Audit.AuditTrailStatisticsDto());

        public Task<ExportResultDto> ExportAdvancedAsync(ExportRequestDto exportRequest, CancellationToken cancellationToken = default)
            => Task.FromResult(new ExportResultDto());

        public Task<ExportResultDto?> GetExportStatusAsync(Guid exportId, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportResultDto?>(null);

        public Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(PaginationParameters pagination, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<EntityChangeLogDto> { Items = new List<EntityChangeLogDto>(), TotalCount = 0, Page = pagination.Page, PageSize = pagination.PageSize });

        public Task<PagedResult<EntityChangeLogDto>> GetLogsByEntityAsync(string entityType, PaginationParameters pagination, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<EntityChangeLogDto> { Items = new List<EntityChangeLogDto>(), TotalCount = 0, Page = pagination.Page, PageSize = pagination.PageSize });

        public Task<PagedResult<EntityChangeLogDto>> GetLogsByUserAsync(Guid userId, PaginationParameters pagination, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<EntityChangeLogDto> { Items = new List<EntityChangeLogDto>(), TotalCount = 0, Page = pagination.Page, PageSize = pagination.PageSize });

        public Task<PagedResult<EntityChangeLogDto>> GetLogsByDateRangeAsync(DateTime startDate, DateTime? endDate, PaginationParameters pagination, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<EntityChangeLogDto> { Items = new List<EntityChangeLogDto>(), TotalCount = 0, Page = pagination.Page, PageSize = pagination.PageSize });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
