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
/// Cross-tenant isolation tests for <see cref="DocumentAttachmentService"/>, <see cref="DocumentCommentService"/>
/// and <see cref="DocumentCounterService"/>.
/// Verifies that single-record get/update/delete operations cannot read or mutate resources
/// belonging to a different tenant, closing the security gap described in
/// PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 2).
/// </summary>
public class DocumentSupportServicesTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _attachmentAId;
    private readonly Guid _commentAId;
    private readonly Guid _counterAId;
    private readonly Guid _documentTypeAId;

    public DocumentSupportServicesTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _attachmentAId = Guid.NewGuid();
        _commentAId = Guid.NewGuid();
        _counterAId = Guid.NewGuid();
        _documentTypeAId = Guid.NewGuid();

        SeedTenantAData();
    }

    private void SeedTenantAData()
    {
        _context.DocumentAttachments.Add(new DocumentAttachment
        {
            Id = _attachmentAId,
            TenantId = _tenantAId,
            FileName = "file-a.pdf",
            StoragePath = "/files/file-a.pdf",
            MimeType = "application/pdf",
            Title = "Attachment A"
        });

        _context.DocumentComments.Add(new DocumentComment
        {
            Id = _commentAId,
            TenantId = _tenantAId,
            Content = "Comment A"
        });

        _context.DocumentCounters.Add(new DocumentCounter
        {
            Id = _counterAId,
            TenantId = _tenantAId,
            DocumentTypeId = _documentTypeAId,
            Series = "A",
            CurrentValue = 1
        });

        _context.SaveChanges();
    }

    private DocumentAttachmentService CreateAttachmentService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentAttachmentService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentAttachmentService>>().Object);
    }

    private DocumentCommentService CreateCommentService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentCommentService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentCommentService>>().Object);
    }

    private DocumentCounterService CreateCounterService(Guid? currentTenantId)
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.CurrentTenantId).Returns(currentTenantId);

        return new DocumentCounterService(
            _context,
            new Mock<IAuditLogService>().Object,
            mockTenantContext.Object,
            new Mock<ILogger<DocumentCounterService>>().Object);
    }

    [Fact]
    public async Task UpdateAttachmentAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateAttachmentService(_tenantBId);

        var result = await service.UpdateAttachmentAsync(_attachmentAId, new UpdateDocumentAttachmentDto
        {
            Title = "Hacked"
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentAttachments.AsNoTracking().FirstAsync(a => a.Id == _attachmentAId);
        Assert.Equal("Attachment A", stillOriginal.Title);
    }

    [Fact]
    public async Task DeleteAttachmentAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateAttachmentService(_tenantBId);

        var result = await service.DeleteAttachmentAsync(_attachmentAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.DocumentAttachments.AsNoTracking().FirstAsync(a => a.Id == _attachmentAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task UpdateCommentAsync_FromOtherTenant_ReturnsNull()
    {
        var service = CreateCommentService(_tenantBId);

        var result = await service.UpdateCommentAsync(_commentAId, new UpdateDocumentCommentDto
        {
            Content = "Hacked"
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentComments.AsNoTracking().FirstAsync(c => c.Id == _commentAId);
        Assert.Equal("Comment A", stillOriginal.Content);
    }

    [Fact]
    public async Task DeleteCommentAsync_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateCommentService(_tenantBId);

        var result = await service.DeleteCommentAsync(_commentAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.DocumentComments.AsNoTracking().FirstAsync(c => c.Id == _commentAId);
        Assert.False(stillExists.IsDeleted);
    }

    [Fact]
    public async Task GetByIdAsync_Counter_FromOtherTenant_ReturnsNull()
    {
        var service = CreateCounterService(_tenantBId);

        var result = await service.GetByIdAsync(_counterAId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Counter_FromOtherTenant_ReturnsNull()
    {
        var service = CreateCounterService(_tenantBId);

        var result = await service.UpdateAsync(_counterAId, new UpdateDocumentCounterDto
        {
            CurrentValue = 999
        }, "attacker");

        Assert.Null(result);

        var stillOriginal = await _context.DocumentCounters.AsNoTracking().FirstAsync(c => c.Id == _counterAId);
        Assert.Equal(1, stillOriginal.CurrentValue);
    }

    [Fact]
    public async Task DeleteAsync_Counter_FromOtherTenant_ReturnsFalse()
    {
        var service = CreateCounterService(_tenantBId);

        var result = await service.DeleteAsync(_counterAId, "attacker");

        Assert.False(result);

        var stillExists = await _context.DocumentCounters.AsNoTracking().FirstAsync(c => c.Id == _counterAId);
        Assert.False(stillExists.IsDeleted);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
