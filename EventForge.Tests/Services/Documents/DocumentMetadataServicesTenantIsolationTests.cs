using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Cross-tenant isolation tests for <see cref="DocumentRowService"/>, <see cref="DocumentSummaryLinkService"/>,
/// <see cref="DocumentTemplateService"/> and <see cref="DocumentWorkflowService"/>.
/// Verifies that single-record get/update/delete operations cannot read or mutate resources
/// belonging to a different tenant, closing the security gap described in
/// PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 2).
/// </summary>
public class DocumentMetadataServicesTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _rowAId;
    private readonly Guid _summaryLinkAId;
    private readonly Guid _templateAId;
    private readonly Guid _workflowAId;
    private readonly Guid _documentHeaderAId;

    public DocumentMetadataServicesTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _rowAId = Guid.NewGuid();
        _summaryLinkAId = Guid.NewGuid();
        _templateAId = Guid.NewGuid();
        _workflowAId = Guid.NewGuid();
        _documentHeaderAId = Guid.NewGuid();

        SeedTenantAData();
    }

    private void SeedTenantAData()
    {
        _context.DocumentRows.Add(new DocumentRow
        {
            Id = _rowAId,
            TenantId = _tenantAId,
            DocumentHeaderId = _documentHeaderAId,
            Description = "Row A",
            UnitPrice = 10m,
            Quantity = 1m
        });

        _context.Set<DocumentSummaryLink>().Add(new DocumentSummaryLink
        {
            Id = _summaryLinkAId,
            TenantId = _tenantAId,
            SummaryDocumentId = _documentHeaderAId
        });

        _context.DocumentTemplates.Add(new DocumentTemplate
        {
            Id = _templateAId,
            TenantId = _tenantAId,
            Name = "Template A",
            DocumentTypeId = Guid.NewGuid()
        });

        _context.DocumentWorkflows.Add(new DocumentWorkflow
        {
            Id = _workflowAId,
            TenantId = _tenantAId,
            Name = "Workflow A"
        });

        _context.SaveChanges();
    }

    private DocumentRowService CreateRowService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentRowService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentRowService>>().Object);
    }

    private DocumentSummaryLinkService CreateSummaryLinkService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentSummaryLinkService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentSummaryLinkService>>().Object);
    }

    private DocumentTemplateService CreateTemplateService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentTemplateService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentTemplateService>>().Object);
    }

    private DocumentWorkflowService CreateWorkflowService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentWorkflowService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentWorkflowService>>().Object);
    }

    [Fact]
    public async Task UpdateDocumentRowAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateRowService(_tenantBId);

        var result = await service.UpdateDocumentRowAsync(_rowAId, new UpdateDocumentRowDto
        {
            Description = "Hacked",
            UnitPrice = 999m,
            Quantity = 1m
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _rowAId);
        Assert.Equal("Row A", stillOriginal.Description);
    }

    [Fact]
    public async Task DeleteDocumentRowAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateRowService(_tenantBId);

        var result = await service.DeleteDocumentRowAsync(_rowAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.DocumentRows.AsNoTracking().FirstAsync(r => r.Id == _rowAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task DeleteDocumentSummaryLinkAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateSummaryLinkService(_tenantBId);

        var result = await service.DeleteDocumentSummaryLinkAsync(_summaryLinkAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.Set<DocumentSummaryLink>().AsNoTracking().FirstAsync(l => l.Id == _summaryLinkAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task GetByIdAsync_Template_FromOtherTenant_ReturnsNull()
    {
        var service = CreateTemplateService(_tenantBId);

        var result = await service.GetByIdAsync(_templateAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Template_FromOtherTenant_ReturnsNull()
    {
        var service = CreateTemplateService(_tenantBId);

        var result = await service.UpdateAsync(_templateAId, new UpdateDocumentTemplateDto
        {
            Name = "Hacked"
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentTemplates.AsNoTracking().FirstAsync(t => t.Id == _templateAId);
        Assert.Equal("Template A", stillOriginal.Name);
    }

    [Fact]
    public async Task DeleteAsync_Template_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateTemplateService(_tenantBId);

        var result = await service.DeleteAsync(_templateAId, "attacker");

        Assert.False(result);

        var stillActive = await _context.DocumentTemplates.AsNoTracking().FirstAsync(t => t.Id == _templateAId);
        Assert.True(stillActive.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_Workflow_FromOtherTenant_ReturnsNull()
    {
        var service = CreateWorkflowService(_tenantBId);

        var result = await service.GetByIdAsync(_workflowAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Workflow_FromOtherTenant_ReturnsNull()
    {
        var service = CreateWorkflowService(_tenantBId);

        var result = await service.UpdateAsync(_workflowAId, new UpdateDocumentWorkflowDto
        {
            Name = "Hacked"
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentWorkflows.AsNoTracking().FirstAsync(w => w.Id == _workflowAId);
        Assert.Equal("Workflow A", stillOriginal.Name);
    }

    [Fact]
    public async Task DeleteAsync_Workflow_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateWorkflowService(_tenantBId);

        var result = await service.DeleteAsync(_workflowAId, "attacker");

        Assert.False(result);

        var stillActive = await _context.DocumentWorkflows.AsNoTracking().FirstAsync(w => w.Id == _workflowAId);
        Assert.True(stillActive.IsActive);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
