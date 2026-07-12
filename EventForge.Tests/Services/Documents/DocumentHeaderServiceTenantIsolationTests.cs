using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Cross-tenant isolation tests for <see cref="DocumentHeaderService"/>.
/// Verifies that single-record update/delete operations on document headers and rows
/// cannot mutate resources belonging to a different tenant, closing the security gap
/// described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 2).
/// </summary>
public class DocumentHeaderServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _documentHeaderAId;
    private readonly Guid _documentRowAId;

    public DocumentHeaderServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _documentHeaderAId = Guid.NewGuid();
        _documentRowAId = Guid.NewGuid();

        SeedTenantAData();
    }

    private void SeedTenantAData()
    {
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

        _context.SaveChanges();
    }

    private DocumentHeaderService CreateService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentHeaderService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<IDocumentCounterService>().Object,
            new Mock<IStockMovementService>().Object,
            new Mock<IUnitConversionService>().Object,
            new Mock<ILogger<DocumentHeaderService>>().Object);
    }

    [Fact]
    public async Task UpdateDocumentHeaderAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.UpdateDocumentHeaderAsync(_documentHeaderAId, new UpdateDocumentHeaderDto
        {
            DocumentTypeId = Guid.NewGuid(),
            Number = "Hacked",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Currency = "EUR"
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentHeaders.AsNoTracking().FirstAsync(dh => dh.Id == _documentHeaderAId);
        Assert.Equal("A-0001", stillOriginal.Number);
    }

    [Fact]
    public async Task DeleteDocumentHeaderAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteDocumentHeaderAsync(_documentHeaderAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.DocumentHeaders.AsNoTracking().FirstAsync(dh => dh.Id == _documentHeaderAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task UpdateDocumentRowAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateService(_tenantBId);

        var result = await service.UpdateDocumentRowAsync(_documentRowAId, new UpdateDocumentRowDto
        {
            Description = "Hacked",
            UnitPrice = 999m,
            Quantity = 1m
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _documentRowAId);
        Assert.Equal("Row A", stillOriginal.Description);
    }

    [Fact]
    public async Task DeleteDocumentRowAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateService(_tenantBId);

        var result = await service.DeleteDocumentRowAsync(_documentRowAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _documentRowAId);
        Assert.False(stillExists.IsDeleted);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
