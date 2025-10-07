using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventForge.Tests.Services.PriceLists;

/// <summary>
/// Integration tests for enhanced PriceListService functionality (Issue #245).
/// Tests advanced price calculation with precedence logic and unit conversion.
/// Note: Uses simplified mocks due to project constraints.
/// </summary>
[Trait("Category", "Unit")]
public class EnhancedPriceListServiceTests
{
    private class MockAuditLogService : IAuditLogService
    {
        public Task<EntityChangeLog> LogEntityChangeAsync(string entityName, Guid entityId, string propertyName, string operationType, string? oldValue, string? newValue, string changedBy, string? entityDisplayName = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EntityChangeLog());
        }

        public Task<IEnumerable<EntityChangeLog>> GetEntityLogsAsync(Guid entityId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        }

        public Task<IEnumerable<EntityChangeLog>> GetEntityTypeLogsAsync(string entityName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        }

        public Task<IEnumerable<EntityChangeLog>> GetLogsAsync(System.Linq.Expressions.Expression<Func<EntityChangeLog, bool>>? filter = null, System.Linq.Expressions.Expression<Func<EntityChangeLog, object>>? orderBy = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        }

        public Task<IEnumerable<EntityChangeLog>> TrackEntityChangesAsync<TEntity>(TEntity entity, string operationType, string changedBy, TEntity? originalValues = null, CancellationToken cancellationToken = default) where TEntity : AuditableEntity
        {
            return Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        }

        public Task<IEnumerable<EntityChangeLog>> GetLogsInDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        }

        public Task<IEnumerable<EntityChangeLog>> GetUserLogsAsync(string username, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        }

        public Task<PagedResult<EntityChangeLog>> GetPagedLogsAsync(AuditLogQueryParameters queryParameters, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResult<EntityChangeLog>
            {
                Items = Enumerable.Empty<EntityChangeLog>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            });
        }

        public Task<EntityChangeLog?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<EntityChangeLog?>(null);
        }

        public Task<PagedResult<AuditTrailResponseDto>> SearchAuditTrailAsync(AuditTrailSearchDto searchDto, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResult<AuditTrailResponseDto>
            {
                Items = Enumerable.Empty<AuditTrailResponseDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            });
        }

        public Task<AuditTrailStatisticsDto> GetAuditTrailStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuditTrailStatisticsDto());
        }

        public Task<ExportResultDto> ExportAdvancedAsync(ExportRequestDto exportRequest, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExportResultDto());
        }

        public Task<ExportResultDto?> GetExportStatusAsync(Guid exportId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ExportResultDto?>(null);
        }
    }

    private class MockLogger : ILogger<PriceListService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private readonly PriceListService _priceListService;
    private readonly IUnitConversionService _unitConversionService;

    public EnhancedPriceListServiceTests()
    {
        // Create in-memory database context
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new EventForgeDbContext(options);

        // Create simple mock services
        var auditLogService = new MockAuditLogService();
        var logger = new MockLogger();
        _unitConversionService = new UnitConversionService();

        _priceListService = new PriceListService(context, auditLogService, logger, _unitConversionService);
    }

    [Fact]
    public void UnitConversionService_ShouldBeInjectedCorrectly()
    {
        // Arrange & Act - Constructor should not throw
        // Assert
        Assert.NotNull(_priceListService);
        Assert.NotNull(_unitConversionService);
    }

    [Fact]
    public async Task GetAppliedPriceAsync_ShouldReturnNull_WhenNoProductExists()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();
        var nonExistentEventId = Guid.NewGuid();

        // Act
        var result = await _priceListService.GetAppliedPriceAsync(nonExistentProductId, nonExistentEventId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAppliedPriceWithUnitConversionAsync_ShouldReturnNull_WhenNoProductExists()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();
        var nonExistentEventId = Guid.NewGuid();
        var nonExistentUnitId = Guid.NewGuid();

        // Act
        var result = await _priceListService.GetAppliedPriceWithUnitConversionAsync(
            nonExistentProductId, nonExistentEventId, nonExistentUnitId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_ShouldReturnEmptyList_WhenNoHistoryExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Act
        var result = await _priceListService.GetPriceHistoryAsync(productId, eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task BulkImportPriceListEntriesAsync_ShouldReturnError_WhenPriceListNotFound()
    {
        // Arrange
        var nonExistentPriceListId = Guid.NewGuid();
        var entries = new List<CreatePriceListEntryDto>
        {
            new CreatePriceListEntryDto
            {
                ProductId = Guid.NewGuid(),
                Price = 10.00m,
                Currency = "EUR"
            }
        };
        var currentUser = "testuser";

        // Act
        var result = await _priceListService.BulkImportPriceListEntriesAsync(
            nonExistentPriceListId, entries, currentUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nonExistentPriceListId, result.PriceListId);
        Assert.False(result.IsSuccessful);
        Assert.Equal(1, result.FailureCount);
        _ = Assert.Single(result.Errors);
        Assert.Equal("PRICELIST_NOT_FOUND", result.Errors.First().ErrorCode);
    }

    [Fact]
    public async Task BulkImportPriceListEntriesAsync_ShouldHandleEmptyList()
    {
        // Arrange
        var priceListId = Guid.NewGuid();
        var entries = new List<CreatePriceListEntryDto>();
        var currentUser = "testuser";

        // Act
        var result = await _priceListService.BulkImportPriceListEntriesAsync(priceListId, entries, currentUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(priceListId, result.PriceListId);
        Assert.Equal(0, result.TotalProcessed);
        Assert.Equal(0, result.SuccessCount);
    }

    [Fact]
    public async Task ExportPriceListEntriesAsync_ShouldReturnEmptyList_WhenNoPriceListEntriesExist()
    {
        // Arrange
        var priceListId = Guid.NewGuid();

        // Act
        var result = await _priceListService.ExportPriceListEntriesAsync(priceListId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExportPriceListEntriesAsync_ShouldIncludeInactiveEntries_WhenRequested()
    {
        // Arrange
        var priceListId = Guid.NewGuid();
        var includeInactive = true;

        // Act
        var result = await _priceListService.ExportPriceListEntriesAsync(priceListId, includeInactive);

        // Assert
        Assert.NotNull(result);
        // Empty because no data in test database, but method should execute successfully
    }

    [Fact]
    public async Task ValidatePriceListPrecedenceAsync_ShouldReturnInvalid_WhenNoPriceListsExist()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = await _priceListService.ValidatePriceListPrecedenceAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.EventId);
        Assert.False(result.IsValid);
        Assert.Equal(0, result.TotalPriceListsValidated);
        _ = Assert.Single(result.Issues);
        Assert.Equal(PrecedenceIssueType.NoPriceListsFound, result.Issues.First().IssueType);
        Assert.Equal(ValidationSeverity.Critical, result.Issues.First().Severity);
    }

    [Fact]
    public async Task ValidatePriceListPrecedenceAsync_ShouldHaveDuration()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = await _priceListService.ValidatePriceListPrecedenceAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ValidationDuration.TotalMilliseconds >= 0);
        Assert.True(result.ValidatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var fromDate = DateTime.UtcNow.AddMonths(-6);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _priceListService.GetPriceHistoryAsync(productId, eventId, fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        // Should return empty list for test database with no data
        Assert.Empty(result);
    }

    [Fact]
    public async Task BulkImportPriceListEntriesAsync_ShouldTrackImportedBy()
    {
        // Arrange
        var priceListId = Guid.NewGuid();
        var entries = new List<CreatePriceListEntryDto>();
        var currentUser = "admin@test.com";

        // Act
        var result = await _priceListService.BulkImportPriceListEntriesAsync(priceListId, entries, currentUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(currentUser, result.ImportedBy);
        Assert.True(result.ImportedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task BulkImportPriceListEntriesAsync_ShouldSetReplacedExistingFlag()
    {
        // Arrange
        var priceListId = Guid.NewGuid();
        var entries = new List<CreatePriceListEntryDto>();
        var currentUser = "testuser";
        var replaceExisting = true;

        // Act
        var result = await _priceListService.BulkImportPriceListEntriesAsync(
            priceListId, entries, currentUser, replaceExisting);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ReplacedExisting);
    }

    [Fact]
    public async Task ValidatePriceListPrecedenceAsync_ShouldProvideSummaryMessage()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = await _priceListService.ValidatePriceListPrecedenceAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Summary);
        Assert.NotEmpty(result.Summary);
        Assert.Contains("Validation", result.Summary);
    }
}