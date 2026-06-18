using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Bulk;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;
using DtoApprovalStatus = Prym.DTOs.Common.ApprovalStatus;

namespace EventForge.Tests.Services.Documents;

[Trait("Category", "Unit")]
public class DocumentFacadeBulkOperationsTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IDocumentHeaderService> _documentHeaderService = new();
    private readonly Mock<IDocumentStatusService> _documentStatusService = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    public DocumentFacadeBulkOperationsTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);
    }

    [Fact]
    public async Task BulkApproveAsync_WhenDocumentIsDraft_UsesValidatedStateFlowBeforeApproval()
    {
        var documentId = SeedDocument(DocumentStatus.Draft, "DOC-APP-1");
        var facade = CreateFacade();

        _documentStatusService
            .Setup(x => x.ValidateTransitionAsync(documentId, DocumentStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StateTransitionValidationResult.Success());
        _documentStatusService
            .Setup(x => x.ChangeStatusAsync(documentId, DocumentStatus.Active, "bulk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentHeaderDto { Id = documentId, Status = DocumentStatus.Active });
        _documentHeaderService
            .Setup(x => x.ApproveDocumentAsync(documentId, "tester", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentHeaderDto { Id = documentId, Status = DocumentStatus.Active, ApprovalStatus = DtoApprovalStatus.Approved });

        var result = await facade.BulkApproveAsync(
            new BulkApprovalDto { DocumentIds = new List<Guid> { documentId }, ApprovalNotes = "bulk" },
            "tester");

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        _documentStatusService.Verify(x => x.ValidateTransitionAsync(documentId, DocumentStatus.Active, It.IsAny<CancellationToken>()), Times.Once);
        _documentStatusService.Verify(x => x.ChangeStatusAsync(documentId, DocumentStatus.Active, "bulk", It.IsAny<CancellationToken>()), Times.Once);
        _documentHeaderService.Verify(x => x.ApproveDocumentAsync(documentId, "tester", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkStatusChangeAsync_WhenSomeItemsInvalid_ReturnsPerItemErrorsAndContinues()
    {
        var validDocumentId = SeedDocument(DocumentStatus.Active, "DOC-OK");
        var invalidDocumentId = SeedDocument(DocumentStatus.Archived, "DOC-KO");
        var facade = CreateFacade();

        _documentStatusService
            .Setup(x => x.ValidateTransitionAsync(validDocumentId, DocumentStatus.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StateTransitionValidationResult.Success());
        _documentStatusService
            .Setup(x => x.ValidateTransitionAsync(invalidDocumentId, DocumentStatus.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StateTransitionValidationResult.Fail("Invalid transition", StateTransitionErrorCode.InvalidTransition));
        _documentStatusService
            .Setup(x => x.ChangeStatusAsync(validDocumentId, DocumentStatus.Draft, "reset", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentHeaderDto { Id = validDocumentId, Status = DocumentStatus.Draft });

        var result = await facade.BulkStatusChangeAsync(
            new BulkStatusChangeDto
            {
                DocumentIds = new List<Guid> { validDocumentId, invalidDocumentId },
                NewStatus = nameof(DocumentStatus.Draft),
                Reason = "reset"
            },
            "tester");

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains(result.Errors, e => e.ItemId == invalidDocumentId);
        _documentStatusService.Verify(x => x.ChangeStatusAsync(validDocumentId, DocumentStatus.Draft, "reset", It.IsAny<CancellationToken>()), Times.Once);
        _documentStatusService.Verify(x => x.ChangeStatusAsync(invalidDocumentId, DocumentStatus.Draft, "reset", It.IsAny<CancellationToken>()), Times.Never);
    }

    private Guid SeedDocument(DocumentStatus status, string number)
    {
        var documentId = Guid.NewGuid();
        _context.DocumentHeaders.Add(new DocumentHeader
        {
            Id = documentId,
            TenantId = _tenantId,
            DocumentTypeId = Guid.NewGuid(),
            Number = number,
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        _context.SaveChanges();
        return documentId;
    }

    private DocumentFacade CreateFacade()
    {
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(x => x.CurrentTenantId).Returns(_tenantId);

        return new DocumentFacade(
            Mock.Of<IDocumentAttachmentService>(),
            Mock.Of<IDocumentCommentService>(),
            Mock.Of<IDocumentTemplateService>(),
            Mock.Of<IDocumentWorkflowService>(),
            Mock.Of<IDocumentAnalyticsService>(),
            _documentHeaderService.Object,
            Mock.Of<IDocumentTypeService>(),
            _documentStatusService.Object,
            _context,
            tenantContext.Object,
            Mock.Of<ILogger<DocumentFacade>>());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
