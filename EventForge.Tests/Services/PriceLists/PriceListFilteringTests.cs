using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using EntityPriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;
using DtoPriceListStatus = EventForge.DTOs.Common.PriceListStatus;

namespace EventForge.Tests.Services.PriceLists;

/// <summary>
/// Tests for server-side filtering in PriceListService.GetPriceListsAsync.
/// Verifies that filters are applied BEFORE pagination for correct results.
/// </summary>
[Trait("Category", "Unit")]
public class PriceListFilteringTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly PriceListService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();

    public PriceListFilteringTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        // Create mock logger
        var logger = new MockLogger();
        
        // Note: PriceListService constructor requires many dependencies.
        // Since we're only testing GetPriceListsAsync, we'll create minimal mocks.
        // In a real scenario, you might use a mocking library like Moq.
        var auditLogService = new MockAuditLogService();
        var unitConversionService = new MockUnitConversionService();
        var generationService = new MockPriceListGenerationService();
        var calculationService = new MockPriceCalculationService();
        var businessPartyService = new MockPriceListBusinessPartyService();
        var bulkOperationsService = new MockPriceListBulkOperationsService();

        _service = new PriceListService(
            _context,
            auditLogService,
            logger,
            unitConversionService,
            generationService,
            calculationService,
            businessPartyService,
            bulkOperationsService);
    }

    [Fact]
    public async Task GetPriceListsAsync_WithDirectionFilter_ReturnsOnlyMatchingDirection()
    {
        // Arrange: Create test data with different directions
        await SeedPriceListsAsync();

        // Act: Filter by Output direction
        var result = await _service.GetPriceListsAsync(
            new PaginationParameters { Page = 1, PageSize = 10 },
            direction: PriceListDirection.Output,
            status: null);

        // Assert: Should return only Output price lists
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // 3 Output price lists
        Assert.Equal(3, result.Items.Count());
        Assert.All(result.Items, pl => Assert.Equal(PriceListDirection.Output, pl.Direction));
    }

    [Fact]
    public async Task GetPriceListsAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        await SeedPriceListsAsync();

        // Act: Filter by Active status
        var result = await _service.GetPriceListsAsync(
            new PaginationParameters { Page = 1, PageSize = 10 },
            direction: null,
            status: DtoPriceListStatus.Active);

        // Assert: Should return only Active price lists
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalCount); // 4 Active price lists
        Assert.Equal(4, result.Items.Count());
        Assert.All(result.Items, pl => Assert.Equal(DtoPriceListStatus.Active, pl.Status));
    }

    [Fact]
    public async Task GetPriceListsAsync_WithDirectionAndStatusFilter_ReturnsMatchingBoth()
    {
        // Arrange
        await SeedPriceListsAsync();

        // Act: Filter by Output direction AND Active status
        var result = await _service.GetPriceListsAsync(
            new PaginationParameters { Page = 1, PageSize = 10 },
            direction: PriceListDirection.Output,
            status: DtoPriceListStatus.Active);

        // Assert: Should return only Active Output price lists
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount); // 2 Active Output price lists
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, pl =>
        {
            Assert.Equal(PriceListDirection.Output, pl.Direction);
            Assert.Equal(DtoPriceListStatus.Active, pl.Status);
        });
    }

    [Fact]
    public async Task GetPriceListsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange: Create enough data to test pagination
        await SeedPriceListsAsync();

        // Act: Get page 1 with pageSize 2, filtering by Output
        var page1 = await _service.GetPriceListsAsync(
            new PaginationParameters { Page = 1, PageSize = 2 },
            direction: PriceListDirection.Output,
            status: null);

        var page2 = await _service.GetPriceListsAsync(
            new PaginationParameters { Page = 2, PageSize = 2 },
            direction: PriceListDirection.Output,
            status: null);

        // Assert: Page 1 should have 2 items, Page 2 should have 1 item
        Assert.Equal(3, page1.TotalCount); // Total 3 Output price lists
        Assert.Equal(2, page1.Items.Count()); // Page 1 has 2 items
        Assert.Equal(1, page1.Page);

        Assert.Equal(3, page2.TotalCount); // Same total count
        Assert.Equal(1, page2.Items.Count()); // Page 2 has remaining 1 item
        Assert.Equal(2, page2.Page);
    }

    [Fact]
    public async Task GetPriceListsAsync_NoFilters_ReturnsAllNonDeleted()
    {
        // Arrange
        await SeedPriceListsAsync();

        // Act: No filters
        var result = await _service.GetPriceListsAsync(
            new PaginationParameters { Page = 1, PageSize = 20 },
            direction: null,
            status: null);

        // Assert: Should return all 6 non-deleted price lists
        Assert.NotNull(result);
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(6, result.Items.Count());
    }

    [Fact]
    public async Task GetPriceListsAsync_WithFilters_TotalCountReflectsFilteredResults()
    {
        // Arrange
        await SeedPriceListsAsync();

        // Act: Filter that matches only 1 price list
        var result = await _service.GetPriceListsAsync(
            new PaginationParameters { Page = 1, PageSize = 10 },
            direction: PriceListDirection.Input,
            status: DtoPriceListStatus.Suspended);

        // Assert: TotalCount should be 1, not 6
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    /// <summary>
    /// Seeds test data: 6 price lists with various combinations of Direction and Status
    /// - PL-001: Output, Active
    /// - PL-002: Output, Active
    /// - PL-003: Input, Active
    /// - PL-004: Output, Suspended
    /// - PL-005: Input, Active
    /// - PL-006: Input, Suspended
    /// </summary>
    private async Task SeedPriceListsAsync()
    {
        var priceLists = new[]
        {
            CreatePriceList("PL-001", PriceListDirection.Output, EntityPriceListStatus.Active, 1),
            CreatePriceList("PL-002", PriceListDirection.Output, EntityPriceListStatus.Active, 2),
            CreatePriceList("PL-003", PriceListDirection.Input, EntityPriceListStatus.Active, 3),
            CreatePriceList("PL-004", PriceListDirection.Output, EntityPriceListStatus.Suspended, 4),
            CreatePriceList("PL-005", PriceListDirection.Input, EntityPriceListStatus.Active, 5),
            CreatePriceList("PL-006", PriceListDirection.Input, EntityPriceListStatus.Suspended, 6)
        };

        _context.PriceLists.AddRange(priceLists);
        await _context.SaveChangesAsync();
    }

    private PriceList CreatePriceList(string code, PriceListDirection direction, EntityPriceListStatus status, int priority)
    {
        return new PriceList
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = $"Price List {code}",
            Direction = direction,
            Status = status,
            Priority = priority,
            EventId = _eventId,
            TenantId = _tenantId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            ProductPrices = new List<PriceListEntry>()
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region Mock Services

    private class MockLogger : ILogger<PriceListService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // For debugging, you can uncomment this line:
            // Console.WriteLine(formatter(state, exception));
        }
    }

    private class MockAuditLogService : IAuditLogService
    {
        public Task<EntityChangeLog> LogEntityChangeAsync(string entityName, Guid entityId, string propertyName, string operationType, string? oldValue, string? newValue, string changedBy, string? entityDisplayName = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new EntityChangeLog());
        public Task<IEnumerable<EntityChangeLog>> GetEntityLogsAsync(Guid entityId, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        public Task<IEnumerable<EntityChangeLog>> GetEntityTypeLogsAsync(string entityName, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        public Task<IEnumerable<EntityChangeLog>> GetLogsAsync(System.Linq.Expressions.Expression<Func<EntityChangeLog, bool>>? filter = null, System.Linq.Expressions.Expression<Func<EntityChangeLog, object>>? orderBy = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        public Task<IEnumerable<EntityChangeLog>> TrackEntityChangesAsync<TEntity>(TEntity entity, string operationType, string changedBy, TEntity? originalValues, CancellationToken cancellationToken = default) where TEntity : AuditableEntity
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        public Task<IEnumerable<EntityChangeLog>> GetLogsInDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        public Task<IEnumerable<EntityChangeLog>> GetUserLogsAsync(string username, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<EntityChangeLog>());
        public Task<PagedResult<EntityChangeLog>> GetPagedLogsAsync(AuditLogQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<EntityChangeLog> { Items = Enumerable.Empty<EntityChangeLog>(), TotalCount = 0, Page = 1, PageSize = 10 });
        public Task<EntityChangeLog?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<EntityChangeLog?>(null);
        public Task<PagedResult<AuditTrailResponseDto>> SearchAuditTrailAsync(AuditTrailSearchDto searchDto, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<AuditTrailResponseDto> { Items = Enumerable.Empty<AuditTrailResponseDto>(), TotalCount = 0, Page = 1, PageSize = 10 });
        public Task<AuditTrailStatisticsDto> GetAuditTrailStatisticsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AuditTrailStatisticsDto());
        public Task<ExportResultDto> ExportAdvancedAsync(ExportRequestDto exportRequest, CancellationToken cancellationToken = default)
            => Task.FromResult(new ExportResultDto());
        public Task<ExportResultDto?> GetExportStatusAsync(Guid exportId, CancellationToken cancellationToken = default)
            => Task.FromResult<ExportResultDto?>(null);
    }

    private class MockUnitConversionService : IUnitConversionService
    {
        public decimal? ConvertQuantity(decimal quantity, Guid fromUnitId, Guid toUnitId) => null;
        public Task<decimal?> ConvertQuantityAsync(decimal quantity, Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default) => Task.FromResult<decimal?>(null);
        public decimal ConvertPrice(decimal price, decimal fromQuantity, decimal toQuantity, int roundingDecimals = 2) => price;
        public decimal ConvertQuantity(decimal quantity, decimal fromConversionFactor, decimal toConversionFactor, int decimalPlaces = 2) => quantity;
        public decimal ConvertToBaseUnit(decimal quantity, decimal conversionFactor, int decimalPlaces = 2) => quantity;
        public decimal ConvertFromBaseUnit(decimal quantity, decimal conversionFactor, int decimalPlaces = 2) => quantity;
        public bool IsValidConversionFactor(decimal conversionFactor) => true;
    }

    private class MockPriceListGenerationService : IPriceListGenerationService
    {
        public Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(GeneratePriceListFromPurchasesDto dto, CancellationToken cancellationToken = default)
            => Task.FromResult(new GeneratePriceListPreviewDto());
        public Task<Guid> GenerateFromPurchasesAsync(GeneratePriceListFromPurchasesDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.NewGuid());
        public Task<GeneratePriceListPreviewDto> PreviewUpdateFromPurchasesAsync(UpdatePriceListFromPurchasesDto dto, CancellationToken cancellationToken = default)
            => Task.FromResult(new GeneratePriceListPreviewDto());
        public Task<UpdatePriceListResultDto> UpdateFromPurchasesAsync(UpdatePriceListFromPurchasesDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(new UpdatePriceListResultDto());
        public Task<Guid> GenerateFromProductPricesAsync(GeneratePriceListFromProductsDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.NewGuid());
        public Task<GeneratePriceListPreviewDto> PreviewGenerateFromProductPricesAsync(GeneratePriceListFromProductsDto dto, CancellationToken cancellationToken = default)
            => Task.FromResult(new GeneratePriceListPreviewDto());
        public Task<ApplyPriceListResultDto> ApplyPriceListToProductsAsync(ApplyPriceListToProductsDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(new ApplyPriceListResultDto());
        public Task<DuplicatePriceListResultDto> DuplicatePriceListAsync(Guid sourcePriceListId, DuplicatePriceListDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(new DuplicatePriceListResultDto { NewPriceList = new PriceListDto() });
    }

    private class MockPriceCalculationService : IPriceCalculationService
    {
        public Task<ProductPriceResultDto> GetProductPriceAsync(GetProductPriceRequestDto request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProductPriceResultDto());
        public Task<List<PurchasePriceComparisonDto>> GetPurchasePriceComparisonAsync(Guid productId, int quantity = 1, DateTime? evaluationDate = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<PurchasePriceComparisonDto>());
        public Task<AppliedPriceDto?> GetAppliedPriceAsync(Guid productId, Guid eventId, Guid? businessPartyId = null, DateTime? evaluationDate = null, int quantity = 1, CancellationToken cancellationToken = default)
            => Task.FromResult<AppliedPriceDto?>(null);
        public Task<AppliedPriceDto?> GetAppliedPriceWithUnitConversionAsync(Guid productId, Guid eventId, Guid targetUnitId, DateTime? evaluationDate = null, int quantity = 1, Guid? businessPartyId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<AppliedPriceDto?>(null);
        public Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(Guid productId, Guid eventId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<PriceHistoryDto>());
    }

    private class MockPriceListBusinessPartyService : IPriceListBusinessPartyService
    {
        public Task<PriceListBusinessPartyDto> AssignBusinessPartyAsync(Guid priceListId, AssignBusinessPartyToPriceListDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(new PriceListBusinessPartyDto());
        public Task<bool> RemoveBusinessPartyAsync(Guid priceListId, Guid businessPartyId, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public Task<IEnumerable<PriceListBusinessPartyDto>> GetBusinessPartiesForPriceListAsync(Guid priceListId, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<PriceListBusinessPartyDto>());
        public Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(Guid businessPartyId, PriceListType? type, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<PriceListDto>());
    }

    private class MockPriceListBulkOperationsService : IPriceListBulkOperationsService
    {
        public Task<BulkUpdatePreviewDto> PreviewBulkUpdateAsync(Guid priceListId, BulkPriceUpdateDto dto, CancellationToken cancellationToken = default)
            => Task.FromResult(new BulkUpdatePreviewDto());
        public Task<BulkUpdateResultDto> BulkUpdatePricesAsync(Guid priceListId, BulkPriceUpdateDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(new BulkUpdateResultDto());
        public Task<DuplicatePriceListResultDto> DuplicatePriceListAsync(Guid sourcePriceListId, DuplicatePriceListDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(new DuplicatePriceListResultDto { NewPriceList = new PriceListDto() });
        public Task<BulkImportResultDto> BulkImportPriceListEntriesAsync(Guid priceListId, IEnumerable<CreatePriceListEntryDto> entries, string currentUser, bool replaceExisting = false, CancellationToken cancellationToken = default)
            => Task.FromResult(new BulkImportResultDto());
        public Task<IEnumerable<ExportablePriceListEntryDto>> ExportPriceListEntriesAsync(Guid priceListId, bool includeInactiveEntries = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<ExportablePriceListEntryDto>());
        public Task<PrecedenceValidationResultDto> ValidatePriceListPrecedenceAsync(Guid eventId, CancellationToken cancellationToken = default)
            => Task.FromResult(new PrecedenceValidationResultDto());
        public Task<ApplyPriceListResultDto> ApplyPriceListToProductsAsync(ApplyPriceListToProductsDto dto, string currentUser, CancellationToken cancellationToken = default)
            => Task.FromResult(new ApplyPriceListResultDto());
    }

    #endregion
}
