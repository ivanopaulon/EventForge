using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Chat;
using EventForge.Server.Hubs;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Chat;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Chat;

namespace EventForge.Tests.Services.Chat;

/// <summary>
/// Cross-tenant isolation tests for <see cref="ChatService"/>, verifying that single-record
/// update/delete operations cannot read or mutate resources belonging to a different tenant
/// when a tenant scope is supplied by the caller.
/// Closes the gap described in PROMPT_21_TENANT_ISOLATION_SECURITY_FIX.md (Level 3).
///
/// Unlike other services in this suite, ChatService has no injected ITenantContext: the
/// established convention (pre-existing GetChatByIdAsync) is to pass tenant scope as an
/// optional Guid? parameter, applying a null-permissive filter. This test suite verifies the
/// same optional-tenantId parameter added to Update/Delete/member-management methods behaves
/// correctly when a tenant scope IS supplied by the caller (e.g. controllers/hubs).
/// </summary>
public class ChatServiceTenantIsolationTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly ChatService _chatService;
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _chatAId;

    public ChatServiceTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventForgeDbContext(options);

        var mockAuditLogService = new Mock<IAuditLogService>();
        var mockLogger = new Mock<ILogger<ChatService>>();
        var mockHubContext = new Mock<IHubContext<ChatHub>>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());
        var mockOnlineUserTracker = new Mock<IOnlineUserTracker>();

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
        mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        _chatService = new ChatService(
            _context,
            mockAuditLogService.Object,
            mockLogger.Object,
            mockHubContext.Object,
            mockEnvironment.Object,
            new MemoryCache(new MemoryCacheOptions()),
            mockOnlineUserTracker.Object,
            new HtmlSanitizerService());

        _chatAId = Guid.NewGuid();

        _context.ChatThreads.Add(new ChatThread
        {
            Id = _chatAId,
            TenantId = _tenantAId,
            Type = ChatType.Group,
            Name = "Chat A",
            IsPrivate = true,
            CreatedBy = _userId.ToString()
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task UpdateChatAsync_CrossTenant_Throws()
    {
        var updateDto = new UpdateChatDto { Name = "Hacked Name" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _chatService.UpdateChatAsync(_chatAId, updateDto, _userId, tenantId: _tenantBId));

        var unchanged = await _context.ChatThreads.FirstAsync(t => t.Id == _chatAId);
        Assert.Equal("Chat A", unchanged.Name);
    }

    [Fact]
    public async Task UpdateChatAsync_SameTenant_Succeeds()
    {
        var updateDto = new UpdateChatDto { Name = "Updated Name" };

        var result = await _chatService.UpdateChatAsync(_chatAId, updateDto, _userId, tenantId: _tenantAId);

        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task DeleteChatAsync_CrossTenant_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _chatService.DeleteChatAsync(_chatAId, _userId, tenantId: _tenantBId));

        var stillPresent = await _context.ChatThreads.FirstOrDefaultAsync(t => t.Id == _chatAId && !t.IsDeleted);
        Assert.NotNull(stillPresent);
    }

    [Fact]
    public async Task DeleteChatAsync_SameTenant_Succeeds()
    {
        var result = await _chatService.DeleteChatAsync(_chatAId, _userId, tenantId: _tenantAId);

        Assert.True(result.Success);
        var deleted = await _context.ChatThreads.IgnoreQueryFilters().FirstAsync(t => t.Id == _chatAId);
        Assert.True(deleted.IsDeleted);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
