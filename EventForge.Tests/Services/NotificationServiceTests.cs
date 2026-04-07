using EventForge.DTOs.Notifications;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Data.Entities.Notifications;
using EventForge.Server.Hubs;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services;

/// <summary>
/// Tests for NotificationService to verify bulk operations, preferences, localization, and expiry cleanup
/// </summary>
[Trait("Category", "Unit")]
public class NotificationServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
    private readonly NotificationService _service;
    private readonly Guid _testTenantId;
    private readonly Guid _testUserId;

    public NotificationServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventForgeDbContext(options);
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _hubContextMock = new Mock<IHubContext<NotificationHub>>();

        // Setup SignalR mock
        var clientProxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _service = new NotificationService(
            _context,
            _auditLogServiceMock.Object,
            _loggerMock.Object,
            _hubContextMock.Object
        );

        _testTenantId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();

        // Seed test user
        _context.Users.Add(new User
        {
            Id = _testUserId,
            TenantId = _testTenantId,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    #region SendBulkNotificationsAsync Tests

    [Fact]
    public async Task SendBulkNotificationsAsync_WithEmptyList_ReturnsZeroResults()
    {
        // Arrange
        var notifications = new List<CreateNotificationDto>();

        // Act
        var result = await _service.SendBulkNotificationsAsync(notifications);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task SendBulkNotificationsAsync_WithInvalidBatchSize_ThrowsException()
    {
        // Arrange
        var notifications = new List<CreateNotificationDto>
        {
            new() { Type = NotificationTypes.System, Payload = new NotificationPayloadDto { Title = "Test", Message = "Test" } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SendBulkNotificationsAsync(notifications, batchSize: 0));
    }

    [Fact]
    public async Task SendBulkNotificationsAsync_WithValidNotifications_CreatesInDatabase()
    {
        // Arrange
        var notifications = new List<CreateNotificationDto>
        {
            new()
            {
                TenantId = _testTenantId,
                Type = NotificationTypes.System,
                Priority = NotificationPriority.Normal,
                RecipientIds = new List<Guid> { _testUserId },
                Payload = new NotificationPayloadDto
                {
                    Title = "Test Notification 1",
                    Message = "Test Message 1"
                }
            },
            new()
            {
                TenantId = _testTenantId,
                Type = NotificationTypes.Event,
                Priority = NotificationPriority.High,
                RecipientIds = new List<Guid> { _testUserId },
                Payload = new NotificationPayloadDto
                {
                    Title = "Test Notification 2",
                    Message = "Test Message 2"
                }
            }
        };

        // Act
        var result = await _service.SendBulkNotificationsAsync(notifications);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, r => Assert.True(r.Success));

        // Verify database
        var savedNotifications = await _context.Notifications.ToListAsync();
        Assert.Equal(2, savedNotifications.Count);

        var savedRecipients = await _context.NotificationRecipients.ToListAsync();
        Assert.Equal(2, savedRecipients.Count);
    }

    [Fact]
    public async Task SendBulkNotificationsAsync_WithBatching_ProcessesInBatches()
    {
        // Arrange
        var notifications = Enumerable.Range(0, 150)
            .Select(i => new CreateNotificationDto
            {
                TenantId = _testTenantId,
                Type = NotificationTypes.System,
                Priority = NotificationPriority.Normal,
                RecipientIds = new List<Guid> { _testUserId },
                Payload = new NotificationPayloadDto
                {
                    Title = $"Test Notification {i}",
                    Message = $"Test Message {i}"
                }
            })
            .ToList();

        // Act
        var result = await _service.SendBulkNotificationsAsync(notifications, batchSize: 100);

        // Assert
        Assert.Equal(150, result.TotalCount);
        Assert.Equal(150, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        // Verify all saved to database
        var savedNotifications = await _context.Notifications.ToListAsync();
        Assert.Equal(150, savedNotifications.Count);
    }

    #endregion

    #region GetUserPreferencesAsync Tests

    [Fact]
    public async Task GetUserPreferencesAsync_WithNoStoredPreferences_ReturnsDefaults()
    {
        // Act
        var result = await _service.GetUserPreferencesAsync(_testUserId, _testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal(_testTenantId, result.TenantId);
        Assert.True(result.NotificationsEnabled);
        Assert.Equal(NotificationPriority.Low, result.MinPriority);
        Assert.Equal("en-US", result.PreferredLocale);
        Assert.True(result.SoundEnabled);
        Assert.Equal(30, result.AutoArchiveAfterDays);
    }

    [Fact]
    public async Task GetUserPreferencesAsync_WithStoredPreferences_ReturnsStoredValues()
    {
        // Arrange
        var user = await _context.Users.FindAsync(_testUserId);
        var preferences = new NotificationPreferencesDto
        {
            UserId = _testUserId,
            TenantId = _testTenantId,
            NotificationsEnabled = false,
            MinPriority = NotificationPriority.High,
            EnabledTypes = new List<NotificationTypes> { NotificationTypes.Security },
            PreferredLocale = "it-IT",
            SoundEnabled = false,
            AutoArchiveAfterDays = 60
        };

        var metadata = new Dictionary<string, object>
        {
            ["NotificationPreferences"] = new
            {
                preferences.NotificationsEnabled,
                preferences.MinPriority,
                preferences.EnabledTypes,
                preferences.PreferredLocale,
                preferences.SoundEnabled,
                preferences.AutoArchiveAfterDays
            }
        };
        user!.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPreferencesAsync(_testUserId, _testTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.False(result.NotificationsEnabled);
        Assert.Equal(NotificationPriority.High, result.MinPriority);
        Assert.Equal("it-IT", result.PreferredLocale);
        Assert.False(result.SoundEnabled);
        Assert.Equal(60, result.AutoArchiveAfterDays);
    }

    #endregion

    #region UpdateUserPreferencesAsync Tests

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithValidPreferences_UpdatesAndReturns()
    {
        // Arrange
        var preferences = new NotificationPreferencesDto
        {
            UserId = _testUserId,
            TenantId = _testTenantId,
            NotificationsEnabled = false,
            MinPriority = NotificationPriority.Critical,
            EnabledTypes = new List<NotificationTypes> { NotificationTypes.Security, NotificationTypes.Audit },
            PreferredLocale = "it-IT",
            SoundEnabled = false,
            AutoArchiveAfterDays = 90
        };

        // Act
        var result = await _service.UpdateUserPreferencesAsync(preferences);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.False(result.NotificationsEnabled);
        Assert.Equal(NotificationPriority.Critical, result.MinPriority);
        Assert.Equal("it-IT", result.PreferredLocale);

        // Verify database
        var user = await _context.Users.FindAsync(_testUserId);
        Assert.NotNull(user!.MetadataJson);
        Assert.Contains("NotificationPreferences", user.MetadataJson);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var preferences = new NotificationPreferencesDto
        {
            UserId = Guid.NewGuid(),
            TenantId = _testTenantId,
            NotificationsEnabled = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateUserPreferencesAsync(preferences));
    }

    #endregion

    #region LocalizeNotificationAsync Tests

    [Fact]
    public async Task LocalizeNotificationAsync_WithSameLocale_ReturnsUnchanged()
    {
        // Arrange
        var notification = new NotificationResponseDto
        {
            Id = Guid.NewGuid(),
            Type = NotificationTypes.System,
            Priority = NotificationPriority.Normal,
            Payload = new NotificationPayloadDto
            {
                Title = "Test",
                Message = "Test Message",
                Locale = "en-US"
            }
        };

        // Act
        var result = await _service.LocalizeNotificationAsync(notification, "en-US");

        // Assert
        Assert.Equal("en-US", result.Payload.Locale);
    }

    [Fact]
    public async Task LocalizeNotificationAsync_WithDifferentLocale_UpdatesLocale()
    {
        // Arrange
        var notification = new NotificationResponseDto
        {
            Id = Guid.NewGuid(),
            Type = NotificationTypes.System,
            Priority = NotificationPriority.Normal,
            Payload = new NotificationPayloadDto
            {
                Title = "Test",
                Message = "Test Message",
                Locale = "en-US"
            }
        };

        // Act
        var result = await _service.LocalizeNotificationAsync(notification, "it-IT");

        // Assert
        Assert.Equal("it-IT", result.Payload.Locale);
    }

    #endregion

    #region ProcessExpiredNotificationsAsync Tests

    [Fact]
    public async Task ProcessExpiredNotificationsAsync_WithNoExpiredNotifications_ReturnsZero()
    {
        // Arrange
        // No expired notifications in database

        // Act
        var result = await _service.ProcessExpiredNotificationsAsync(_testTenantId);

        // Assert
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0, result.ExpiredCount);
        Assert.Equal(0, result.ArchivedCount);
        Assert.Equal(0, result.DeletedCount);
    }

    [Fact]
    public async Task ProcessExpiredNotificationsAsync_WithRecentlyExpired_MarksAsExpired()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenantId,
            Type = NotificationTypes.System,
            Priority = NotificationPriority.Normal,
            Status = NotificationStatus.Pending,
            Title = "Test",
            Message = "Test",
            ExpiresAt = DateTime.UtcNow.AddDays(-10), // Expired 10 days ago
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessExpiredNotificationsAsync(_testTenantId);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.ExpiredCount);
        Assert.Equal(0, result.ArchivedCount);
        Assert.Equal(0, result.DeletedCount);

        // Verify status changed
        var updated = await _context.Notifications.FindAsync(notification.Id);
        Assert.Equal(NotificationStatus.Expired, updated!.Status);
    }

    [Fact]
    public async Task ProcessExpiredNotificationsAsync_WithOldExpired_Archives()
    {
        // Arrange
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenantId,
            Type = NotificationTypes.System,
            Priority = NotificationPriority.Normal,
            Status = NotificationStatus.Pending,
            Title = "Test",
            Message = "Test",
            ExpiresAt = DateTime.UtcNow.AddDays(-45), // Expired 45 days ago
            CreatedAt = DateTime.UtcNow.AddDays(-50)
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessExpiredNotificationsAsync(_testTenantId);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.ExpiredCount);
        Assert.Equal(1, result.ArchivedCount);
        Assert.Equal(0, result.DeletedCount);

        // Verify status changed
        var updated = await _context.Notifications.FindAsync(notification.Id);
        Assert.Equal(NotificationStatus.Archived, updated!.Status);
        Assert.True(updated.IsArchived);
    }

    [Fact]
    public async Task ProcessExpiredNotificationsAsync_WithVeryOldExpired_Deletes()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = notificationId,
            TenantId = _testTenantId,
            Type = NotificationTypes.System,
            Priority = NotificationPriority.Normal,
            Status = NotificationStatus.Pending,
            Title = "Test",
            Message = "Test",
            ExpiresAt = DateTime.UtcNow.AddDays(-100), // Expired 100 days ago
            CreatedAt = DateTime.UtcNow.AddDays(-105)
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessExpiredNotificationsAsync(_testTenantId);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.ExpiredCount);
        Assert.Equal(0, result.ArchivedCount);
        Assert.Equal(1, result.DeletedCount);

        // Verify deleted (need to detach and reload to see changes in in-memory DB)
        _context.Entry(notification).State = EntityState.Detached;
        var updated = await _context.Notifications.FindAsync(notificationId);
        Assert.Null(updated);
    }

    [Fact]
    public async Task ProcessExpiredNotificationsAsync_WithMixedExpired_ProcessesAll()
    {
        // Arrange
        var notifications = new[]
        {
            new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = _testTenantId,
                Type = NotificationTypes.System,
                Priority = NotificationPriority.Normal,
                Status = NotificationStatus.Pending,
                Title = "Recent",
                Message = "Test",
                ExpiresAt = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = _testTenantId,
                Type = NotificationTypes.System,
                Priority = NotificationPriority.Normal,
                Status = NotificationStatus.Pending,
                Title = "Old",
                Message = "Test",
                ExpiresAt = DateTime.UtcNow.AddDays(-45),
                CreatedAt = DateTime.UtcNow.AddDays(-50)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = _testTenantId,
                Type = NotificationTypes.System,
                Priority = NotificationPriority.Normal,
                Status = NotificationStatus.Pending,
                Title = "Very Old",
                Message = "Test",
                ExpiresAt = DateTime.UtcNow.AddDays(-100),
                CreatedAt = DateTime.UtcNow.AddDays(-105)
            }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessExpiredNotificationsAsync(_testTenantId);

        // Assert
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(1, result.ExpiredCount);
        Assert.Equal(1, result.ArchivedCount);
        Assert.Equal(1, result.DeletedCount);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
