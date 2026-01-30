using EventForge.DTOs.Chat;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Data.Entities.Chat;
using EventForge.Server.Hubs;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Chat;

/// <summary>
/// Tests for ChatService stub method implementations.
/// </summary>
public class ChatServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<ChatService>> _mockLogger;
    private readonly Mock<IHubContext<ChatHub>> _mockHubContext;
    private readonly ChatService _chatService;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId1 = Guid.NewGuid();
    private readonly Guid _userId2 = Guid.NewGuid();

    public ChatServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventForgeDbContext(options);

        // Setup mocks
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<ChatService>>();
        _mockHubContext = new Mock<IHubContext<ChatHub>>();

        // Setup SignalR hub context mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        _mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
        mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        // Setup audit log service to return successful task
        _mockAuditLogService
            .Setup(x => x.LogEntityChangeAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntityChangeLog
            {
                Id = Guid.NewGuid(),
                EntityName = "Test",
                EntityId = Guid.NewGuid(),
                PropertyName = "Test",
                OperationType = "Test",
                ChangedBy = "Test",
                ChangedAt = DateTime.UtcNow
            });

        _chatService = new ChatService(_context, _mockAuditLogService.Object, _mockLogger.Object, _mockHubContext.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create users
        var user1 = new User
        {
            Id = _userId1,
            Username = "user1",
            Email = "user1@test.com",
            FirstName = "User",
            LastName = "One",
            TenantId = _tenantId,
            PasswordHash = "hash",
            PasswordSalt = "salt",
            PreferredLanguage = "en-US",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsActive = true
        };

        var user2 = new User
        {
            Id = _userId2,
            Username = "user2",
            Email = "user2@test.com",
            FirstName = "User",
            LastName = "Two",
            TenantId = _tenantId,
            PasswordHash = "hash",
            PasswordSalt = "salt",
            PreferredLanguage = "en-US",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.AddRange(user1, user2);

        // Create chat thread
        var chatThread = new ChatThread
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Type = ChatType.DirectMessage,
            Name = "Test Chat",
            IsPrivate = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedBy = _userId1.ToString()
        };

        _context.ChatThreads.Add(chatThread);

        // Create chat members
        var member1 = new ChatMember
        {
            Id = Guid.NewGuid(),
            ChatThreadId = chatThread.Id,
            UserId = _userId1,
            TenantId = _tenantId,
            Role = ChatMemberRole.Owner,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsActive = true
        };

        var member2 = new ChatMember
        {
            Id = Guid.NewGuid(),
            ChatThreadId = chatThread.Id,
            UserId = _userId2,
            TenantId = _tenantId,
            Role = ChatMemberRole.Member,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.ChatMembers.AddRange(member1, member2);

        // Create chat messages
        var message1 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatThreadId = chatThread.Id,
            SenderId = _userId1,
            TenantId = _tenantId,
            Content = "Hello, this is message 1",
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow.AddHours(-2),
            IsDeleted = false,
            IsEdited = false,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ModifiedAt = DateTime.UtcNow.AddHours(-2),
            IsActive = true
        };

        var message2 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatThreadId = chatThread.Id,
            SenderId = _userId2,
            TenantId = _tenantId,
            Content = "Hello, this is message 2",
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow.AddHours(-1),
            IsDeleted = false,
            IsEdited = false,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ModifiedAt = DateTime.UtcNow.AddHours(-1),
            IsActive = true
        };

        var message3 = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatThreadId = chatThread.Id,
            SenderId = _userId1,
            TenantId = _tenantId,
            Content = "This is message 3",
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow,
            IsDeleted = false,
            IsEdited = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.ChatMessages.AddRange(message1, message2, message3);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetMessagesAsync_WithValidChatId_ReturnsMessages()
    {
        // Arrange
        var chatId = _context.ChatThreads.First().Id;
        var searchDto = new MessageSearchDto
        {
            ChatId = chatId,
            TenantId = _tenantId,
            PageNumber = 1,
            PageSize = 10,
            SortOrder = "desc"
        };

        // Act
        var result = await _chatService.GetMessagesAsync(searchDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetMessagesAsync_WithSenderFilter_ReturnsFilteredMessages()
    {
        // Arrange
        var chatId = _context.ChatThreads.First().Id;
        var searchDto = new MessageSearchDto
        {
            ChatId = chatId,
            TenantId = _tenantId,
            SenderId = _userId1,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _chatService.GetMessagesAsync(searchDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, msg => Assert.Equal(_userId1, msg.SenderId));
    }

    [Fact]
    public async Task GetMessagesAsync_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var chatId = _context.ChatThreads.First().Id;
        var searchDto = new MessageSearchDto
        {
            ChatId = chatId,
            TenantId = _tenantId,
            PageNumber = 1,
            PageSize = 2,
            SortOrder = "desc"
        };

        // Act
        var result = await _chatService.GetMessagesAsync(searchDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetMessageByIdAsync_WithValidId_ReturnsMessage()
    {
        // Arrange
        var message = _context.ChatMessages.First();

        // Act
        var result = await _chatService.GetMessageByIdAsync(message.Id, _userId1, _tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(message.Id, result.Id);
        Assert.Equal(message.Content, result.Content);
    }

    [Fact]
    public async Task GetMessageByIdAsync_WithNonMember_ReturnsNull()
    {
        // Arrange
        var message = _context.ChatMessages.First();
        var nonMemberUserId = Guid.NewGuid();

        // Act
        var result = await _chatService.GetMessageByIdAsync(message.Id, nonMemberUserId, _tenantId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMessageByIdAsync_WithWrongTenant_ReturnsNull()
    {
        // Arrange
        var message = _context.ChatMessages.First();
        var wrongTenantId = Guid.NewGuid();

        // Act
        var result = await _chatService.GetMessageByIdAsync(message.Id, _userId1, wrongTenantId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EditMessageAsync_WithValidRequest_UpdatesMessage()
    {
        // Arrange
        var message = _context.ChatMessages.First(m => m.SenderId == _userId1);
        var editDto = new EditMessageDto
        {
            MessageId = message.Id,
            UserId = _userId1,
            Content = "Updated content",
            EditReason = "Test edit"
        };

        // Act
        var result = await _chatService.EditMessageAsync(editDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated content", result.Content);
        Assert.True(result.IsEdited);
        Assert.NotNull(result.EditedAt);
        Assert.True(result.Metadata.ContainsKey("EditReason"));
        Assert.Equal("Test edit", result.Metadata["EditReason"].ToString());
    }

    [Fact]
    public async Task EditMessageAsync_WithNonSender_ThrowsUnauthorizedException()
    {
        // Arrange
        var message = _context.ChatMessages.First(m => m.SenderId == _userId1);
        var editDto = new EditMessageDto
        {
            MessageId = message.Id,
            UserId = _userId2, // Different user
            Content = "Updated content"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _chatService.EditMessageAsync(editDto));
    }

    [Fact]
    public async Task DeleteMessageAsync_WithSoftDelete_MarksAsDeleted()
    {
        // Arrange
        var message = _context.ChatMessages.First(m => m.SenderId == _userId1);

        // Act
        var result = await _chatService.DeleteMessageAsync(message.Id, _userId1, "Test delete", softDelete: true);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(MessageStatus.Deleted, result.NewStatus);

        // Verify in database
        var deletedMessage = await _context.ChatMessages.FindAsync(message.Id);
        Assert.NotNull(deletedMessage);
        Assert.True(deletedMessage.IsDeleted);
        Assert.NotNull(deletedMessage.DeletedAt);
    }

    [Fact]
    public async Task DeleteMessageAsync_WithHardDelete_RemovesFromDatabase()
    {
        // Arrange
        var message = _context.ChatMessages.First(m => m.SenderId == _userId1);
        var messageId = message.Id;

        // Act
        var result = await _chatService.DeleteMessageAsync(messageId, _userId1, "Test hard delete", softDelete: false);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify in database - use a fresh query instead of FindAsync for in-memory database
        var deletedMessage = await _context.ChatMessages
            .Where(m => m.Id == messageId)
            .FirstOrDefaultAsync();
        Assert.Null(deletedMessage);
    }

    [Fact]
    public async Task DeleteMessageAsync_ByAdmin_Succeeds()
    {
        // Arrange
        var message = _context.ChatMessages.First(m => m.SenderId == _userId2);

        // User1 is Owner (admin), trying to delete User2's message
        // Act
        var result = await _chatService.DeleteMessageAsync(message.Id, _userId1, "Admin delete", softDelete: true);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task LocalizeChatMessageAsync_UpdatesLocale()
    {
        // Arrange
        var messageDto = new ChatMessageDto
        {
            Id = Guid.NewGuid(),
            Content = "Test message",
            Locale = "en-US"
        };

        // Act
        var result = await _chatService.LocalizeChatMessageAsync(messageDto, "it-IT", _userId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("it-IT", result.Locale);
        Assert.Contains("LocalizedTo", result.Metadata.Keys);
    }

    [Fact]
    public async Task LocalizeChatMessageAsync_AlreadyLocalized_ReturnsUnchanged()
    {
        // Arrange
        var messageDto = new ChatMessageDto
        {
            Id = Guid.NewGuid(),
            Content = "Test message",
            Locale = "it-IT"
        };

        // Act
        var result = await _chatService.LocalizeChatMessageAsync(messageDto, "it-IT", _userId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("it-IT", result.Locale);
    }

    [Fact]
    public async Task UpdateChatLocalizationAsync_UpdatesUserPreferences()
    {
        // Arrange
        var preferences = new ChatLocalizationPreferencesDto
        {
            UserId = _userId1,
            PreferredLocale = "it-IT",
            AutoTranslate = true
        };

        // Act
        var result = await _chatService.UpdateChatLocalizationAsync(_userId1, preferences);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_userId1, result.UserId);
        Assert.Equal("it-IT", result.PreferredLocale);

        // Verify in database
        var user = await _context.Users.FindAsync(_userId1);
        Assert.NotNull(user);
        Assert.Equal("it-IT", user.PreferredLanguage);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
