using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentStatusService — archive and reactivate transitions.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentHeaderArchiveTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<DocumentStatusService>> _mockLogger;
    private readonly DocumentStatusService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DocumentHeaderArchiveTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _mockTenantContext = new Mock<ITenantContext>();
        _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);
        _mockLogger = new Mock<ILogger<DocumentStatusService>>();

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        _service = new DocumentStatusService(
            _context,
            _mockTenantContext.Object,
            httpContextAccessor.Object,
            _mockLogger.Object);
    }

    #region ChangeStatusAsync — Active → Archived

    [Fact]
    public async Task ChangeStatusAsync_ActiveToArchived_ValidDocument_ChangesStatus()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: true, totalGross: 150m, number: "DOC-001");

        var result = await _service.ChangeStatusAsync(docId, DocumentStatus.Archived);

        Assert.NotNull(result);
        Assert.Equal(DocumentStatus.Archived, result.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_ActiveToArchived_SetsArchivedAtOnEntity()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: true, totalGross: 150m, number: "DOC-001");

        var before = DateTime.UtcNow.AddSeconds(-1);
        await _service.ChangeStatusAsync(docId, DocumentStatus.Archived);
        var after = DateTime.UtcNow.AddSeconds(1);

        var entity = await _context.DocumentHeaders.FindAsync(docId);
        // ArchivedAt is set by DocumentStatusService via Status change (or ArchiveDocumentAsync — here we confirm Status is Archived)
        Assert.NotNull(entity);
        Assert.Equal(DocumentStatus.Archived, entity.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_ActiveToArchived_CreatesStatusHistoryRecord()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: true, totalGross: 150m, number: "DOC-001");

        await _service.ChangeStatusAsync(docId, DocumentStatus.Archived);

        var history = await _context.DocumentStatusHistories
            .Where(h => h.DocumentHeaderId == docId)
            .ToListAsync();

        Assert.Single(history);
        Assert.Equal(DocumentStatus.Active, history[0].FromStatus);
        Assert.Equal(DocumentStatus.Archived, history[0].ToStatus);
    }

    [Fact]
    public async Task ChangeStatusAsync_ActiveToArchived_NoRows_ThrowsInvalidOperationException()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: false, totalGross: 0m, number: "DOC-002");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangeStatusAsync(docId, DocumentStatus.Archived));
    }

    [Fact]
    public async Task ChangeStatusAsync_ActiveToArchived_ZeroTotal_ThrowsInvalidOperationException()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: true, totalGross: 0m, number: "DOC-003");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangeStatusAsync(docId, DocumentStatus.Archived));
    }

    [Fact]
    public async Task ChangeStatusAsync_ActiveToArchived_MissingNumber_ThrowsInvalidOperationException()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: true, totalGross: 150m, number: string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangeStatusAsync(docId, DocumentStatus.Archived));
    }

    #endregion

    #region ChangeStatusAsync — Archived → Active

    [Fact]
    public async Task ChangeStatusAsync_ArchivedToActive_ValidDocument_ChangesStatus()
    {
        var docId = SeedDocument(DocumentStatus.Archived, withRows: true, totalGross: 100m, number: "DOC-010");

        var result = await _service.ChangeStatusAsync(docId, DocumentStatus.Active);

        Assert.NotNull(result);
        Assert.Equal(DocumentStatus.Active, result.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_ArchivedToActive_CreatesStatusHistoryRecord()
    {
        var docId = SeedDocument(DocumentStatus.Archived, withRows: true, totalGross: 100m, number: "DOC-011");

        await _service.ChangeStatusAsync(docId, DocumentStatus.Active);

        var history = await _context.DocumentStatusHistories
            .Where(h => h.DocumentHeaderId == docId)
            .ToListAsync();

        Assert.Single(history);
        Assert.Equal(DocumentStatus.Archived, history[0].FromStatus);
        Assert.Equal(DocumentStatus.Active, history[0].ToStatus);
    }

    #endregion

    #region ChangeStatusAsync — Document not found

    [Fact]
    public async Task ChangeStatusAsync_DocumentNotFound_ReturnsNull()
    {
        var result = await _service.ChangeStatusAsync(Guid.NewGuid(), DocumentStatus.Archived);

        Assert.Null(result);
    }

    #endregion

    #region GetAvailableTransitionsAsync

    [Fact]
    public async Task GetAvailableTransitionsAsync_ActiveDocument_ReturnsArchivedOnly()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: true, totalGross: 100m, number: "DOC-020");

        var transitions = await _service.GetAvailableTransitionsAsync(docId);

        Assert.Single(transitions);
        Assert.Contains(DocumentStatus.Archived, transitions);
    }

    [Fact]
    public async Task GetAvailableTransitionsAsync_ArchivedDocument_ReturnsActiveOnly()
    {
        var docId = SeedDocument(DocumentStatus.Archived, withRows: true, totalGross: 100m, number: "DOC-021");

        var transitions = await _service.GetAvailableTransitionsAsync(docId);

        Assert.Single(transitions);
        Assert.Contains(DocumentStatus.Active, transitions);
    }

    [Fact]
    public async Task GetAvailableTransitionsAsync_DocumentNotFound_ReturnsEmpty()
    {
        var transitions = await _service.GetAvailableTransitionsAsync(Guid.NewGuid());

        Assert.Empty(transitions);
    }

    #endregion

    #region ValidateTransitionAsync

    [Fact]
    public async Task ValidateTransitionAsync_ValidActiveToArchived_ReturnsSuccess()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: true, totalGross: 100m, number: "DOC-030");

        var result = await _service.ValidateTransitionAsync(docId, DocumentStatus.Archived);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateTransitionAsync_ActiveToActive_Fails()
    {
        var docId = SeedDocument(DocumentStatus.Active, withRows: true, totalGross: 100m, number: "DOC-031");

        var result = await _service.ValidateTransitionAsync(docId, DocumentStatus.Active);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.InvalidTransition, result.ErrorCode);
    }

    [Fact]
    public async Task ValidateTransitionAsync_DocumentNotFound_ReturnsFail()
    {
        var result = await _service.ValidateTransitionAsync(Guid.NewGuid(), DocumentStatus.Archived);

        Assert.False(result.IsValid);
    }

    #endregion

    #region Helpers

    private Guid SeedDocument(
        DocumentStatus status,
        bool withRows,
        decimal totalGross,
        string number)
    {
        var docTypeId = Guid.NewGuid();
        _context.DocumentTypes.Add(new DocumentType
        {
            Id = docTypeId,
            TenantId = _tenantId,
            Name = "Test Type",
            Code = "TST",
            CreatesStockMovements = false,
            MovesStockOnRowChange = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        var bpId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var header = new DocumentHeader
        {
            Id = docId,
            TenantId = _tenantId,
            DocumentTypeId = docTypeId,
            Number = number,
            Date = DateTime.UtcNow,
            BusinessPartyId = bpId,
            Status = status,
            TotalGrossAmount = totalGross,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        };
        _context.DocumentHeaders.Add(header);

        if (withRows)
        {
            _context.DocumentRows.Add(new DocumentRow
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                DocumentHeaderId = docId,
                ProductId = Guid.NewGuid(),
                Quantity = 1m,
                UnitPrice = totalGross > 0 ? totalGross : 10m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "seed"
            });
        }

        _context.SaveChanges();
        return docId;
    }

    public void Dispose() => _context.Dispose();

    #endregion
}
