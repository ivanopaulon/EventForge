using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;

namespace EventForge.Tests.Services.Documents;

[Trait("Category", "Unit")]
public class DocumentStatusServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly DocumentStatusService _service;

    public DocumentStatusServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(x => x.CurrentTenantId).Returns(Guid.NewGuid());

        _service = new DocumentStatusService(
            _context,
            tenantContext.Object,
            new HttpContextAccessor(),
            Mock.Of<ILogger<DocumentStatusService>>());
    }

    [Fact]
    public async Task GetAvailableTransitionsAsync_WhenDocumentIsDraft_ReturnsDraftTransitions()
    {
        var document = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "DOC-1",
            Date = DateTime.UtcNow,
            BusinessPartyId = Guid.NewGuid(),
            Status = DocumentStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        _context.DocumentHeaders.Add(document);
        await _context.SaveChangesAsync();

        var transitions = await _service.GetAvailableTransitionsAsync(document.Id);

        Assert.Equal(2, transitions.Count);
        Assert.Contains(DocumentStatus.Active, transitions);
        Assert.Contains(DocumentStatus.Cancelled, transitions);
    }

    [Fact]
    public async Task GetAvailableTransitionsAsync_WhenDocumentDoesNotExist_ReturnsEmpty()
    {
        var transitions = await _service.GetAvailableTransitionsAsync(Guid.NewGuid());

        Assert.Empty(transitions);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
