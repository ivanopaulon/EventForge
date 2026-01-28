using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Events;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventForge.Tests.Services.PriceLists;

[Trait("Category", "Unit")]
public class PriceListServicePhase2ATests
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

        public Task<PagedResult<EntityChangeLogDto>> GetAuditLogsAsync(
            PaginationParameters pagination,
            CancellationToken ct = default)
        {
            return Task.FromResult(new PagedResult<EntityChangeLogDto>
            {
                Items = new List<EntityChangeLogDto>(),
                TotalCount = 0,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
            });
        }

        public Task<PagedResult<EntityChangeLogDto>> GetLogsByEntityAsync(
            string entityType,
            PaginationParameters pagination,
            CancellationToken ct = default)
        {
            return Task.FromResult(new PagedResult<EntityChangeLogDto>
            {
                Items = new List<EntityChangeLogDto>(),
                TotalCount = 0,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
            });
        }

        public Task<PagedResult<EntityChangeLogDto>> GetLogsByUserAsync(
            Guid userId,
            PaginationParameters pagination,
            CancellationToken ct = default)
        {
            return Task.FromResult(new PagedResult<EntityChangeLogDto>
            {
                Items = new List<EntityChangeLogDto>(),
                TotalCount = 0,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
            });
        }

        public Task<PagedResult<EntityChangeLogDto>> GetLogsByDateRangeAsync(
            DateTime startDate,
            DateTime? endDate,
            PaginationParameters pagination,
            CancellationToken ct = default)
        {
            return Task.FromResult(new PagedResult<EntityChangeLogDto>
            {
                Items = new List<EntityChangeLogDto>(),
                TotalCount = 0,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
            });
        }
    }

    private readonly DbContextOptions<EventForgeDbContext> _dbOptions;

    public PriceListServicePhase2ATests()
    {
        _dbOptions = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private EventForgeDbContext CreateContext() => new EventForgeDbContext(_dbOptions);

    [Fact]
    public async Task AssignBusinessParty_ShouldCreateRelation()
    {
        // Arrange
        await using var context = CreateContext();
        var mockAudit = new MockAuditLogService();
        var mockUnitConversion = new Server.Services.UnitOfMeasures.UnitConversionService();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, mockAudit, NullLogger<PriceListService>.Instance, mockUnitConversion, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var tenant = CreateTenant();
        var priceList = CreatePriceList(tenant.Id);
        var businessParty = CreateBusinessParty(tenant.Id);

        context.Tenants.Add(tenant);
        context.PriceLists.Add(priceList);
        context.BusinessParties.Add(businessParty);
        await context.SaveChangesAsync();

        var dto = new AssignBusinessPartyToPriceListDto
        {
            BusinessPartyId = businessParty.Id,
            IsPrimary = true,
            GlobalDiscountPercentage = 15.5m,
            Notes = "Test assignment"
        };

        // Act
        var result = await service.AssignBusinessPartyAsync(priceList.Id, dto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(businessParty.Id, result.BusinessPartyId);
        Assert.Equal(businessParty.Name, result.BusinessPartyName);
        Assert.True(result.IsPrimary);
        Assert.Equal(15.5m, result.GlobalDiscountPercentage);
        Assert.Equal("Test assignment", result.Notes);

        var relation = await context.PriceListBusinessParties
            .FirstOrDefaultAsync(plbp => plbp.PriceListId == priceList.Id && plbp.BusinessPartyId == businessParty.Id);
        Assert.NotNull(relation);
        Assert.False(relation.IsDeleted);
        Assert.Equal(PriceListBusinessPartyStatus.Active, relation.Status);
    }

    [Fact]
    public async Task RemoveBusinessParty_ShouldSoftDelete()
    {
        // Arrange
        await using var context = CreateContext();
        var mockAudit = new MockAuditLogService();
        var mockUnitConversion = new Server.Services.UnitOfMeasures.UnitConversionService();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, mockAudit, NullLogger<PriceListService>.Instance, mockUnitConversion, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var tenant = CreateTenant();
        var priceList = CreatePriceList(tenant.Id);
        var businessParty = CreateBusinessParty(tenant.Id);

        context.Tenants.Add(tenant);
        context.PriceLists.Add(priceList);
        context.BusinessParties.Add(businessParty);
        
        var relation = new PriceListBusinessParty
        {
            PriceListId = priceList.Id,
            BusinessPartyId = businessParty.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        context.PriceListBusinessParties.Add(relation);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RemoveBusinessPartyAsync(priceList.Id, businessParty.Id, "test-user");

        // Assert
        Assert.True(result);

        var deletedRelation = await context.PriceListBusinessParties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(plbp => plbp.PriceListId == priceList.Id && plbp.BusinessPartyId == businessParty.Id);
        
        Assert.NotNull(deletedRelation);
        Assert.True(deletedRelation.IsDeleted);
        Assert.NotNull(deletedRelation.DeletedAt);
        Assert.Equal("test-user", deletedRelation.DeletedBy);
        Assert.Equal(PriceListBusinessPartyStatus.Deleted, deletedRelation.Status);
    }

    [Fact]
    public async Task GetPriceListsByType_ShouldFilterCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var mockAudit = new MockAuditLogService();
        var mockUnitConversion = new Server.Services.UnitOfMeasures.UnitConversionService();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, mockAudit, NullLogger<PriceListService>.Instance, mockUnitConversion, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var tenant = CreateTenant();
        var salesList = CreatePriceList(tenant.Id, "Sales List", PriceListType.Sales);
        var purchaseList = CreatePriceList(tenant.Id, "Purchase List", PriceListType.Purchase);

        context.Tenants.Add(tenant);
        context.PriceLists.AddRange(salesList, purchaseList);
        await context.SaveChangesAsync();

        // Act
        var salesResults = (await service.GetPriceListsByTypeAsync(PriceListType.Sales)).ToList();
        var purchaseResults = (await service.GetPriceListsByTypeAsync(PriceListType.Purchase)).ToList();

        // Assert
        Assert.Single(salesResults);
        Assert.Equal("Sales List", salesResults[0].Name);
        Assert.Equal(PriceListType.Sales, salesResults[0].Type);

        Assert.Single(purchaseResults);
        Assert.Equal("Purchase List", purchaseResults[0].Name);
        Assert.Equal(PriceListType.Purchase, purchaseResults[0].Type);
    }

    // Helper methods
    private static Tenant CreateTenant() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Tenant",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static PriceList CreatePriceList(Guid tenantId, string name = "Test List", PriceListType type = PriceListType.Sales) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        Type = type,
        Direction = type == PriceListType.Sales ? PriceListDirection.Output : PriceListDirection.Input,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static BusinessParty CreateBusinessParty(Guid tenantId, string name = "Test Business") => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        PartyType = Server.Data.Entities.Business.BusinessPartyType.Cliente,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

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
        public Task<BulkImportResultDto> BulkImportPriceListEntriesAsync(Guid priceListId, IEnumerable<CreatePriceListEntryDto> entries, string currentUser, bool replaceExisting = false, CancellationToken cancellationToken = default)
            => Task.FromResult(new BulkImportResultDto());
        public Task<IEnumerable<ExportablePriceListEntryDto>> ExportPriceListEntriesAsync(Guid priceListId, bool includeInactiveEntries = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Enumerable.Empty<ExportablePriceListEntryDto>());
        public Task<PrecedenceValidationResultDto> ValidatePriceListPrecedenceAsync(Guid eventId, CancellationToken cancellationToken = default)
            => Task.FromResult(new PrecedenceValidationResultDto());
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
}
