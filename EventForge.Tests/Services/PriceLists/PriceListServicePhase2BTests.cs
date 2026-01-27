using EventForge.DTOs.PriceLists;
using EventForge.DTOs.Common;
using EventForge.DTOs.Audit;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Events;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ServerBusinessPartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType;

namespace EventForge.Tests.Services.PriceLists;

[Trait("Category", "Unit")]
public class PriceListServicePhase2BTests
{
    private readonly DbContextOptions<EventForgeDbContext> _dbOptions;

    public PriceListServicePhase2BTests()
    {
        _dbOptions = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private EventForgeDbContext CreateContext() => new EventForgeDbContext(_dbOptions);

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

    [Fact]
    public async Task GetAppliedPrice_WithBusinessParty_ShouldApplyDiscount()
    {
        // Arrange
        await using var context = CreateContext();
        var mockAudit = new MockAuditLogService();
        var unitConversionService = new UnitConversionService();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, mockAudit, NullLogger<PriceListService>.Instance, unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var tenant = CreateTenant();
        var eventEntity = CreateEvent(tenant.Id);
        var product = CreateProduct(tenant.Id);
        var businessParty = CreateBusinessParty(tenant.Id, "VIP Customer");
        var priceList = CreateSalesPriceList(tenant.Id, eventEntity.Id);

        context.Tenants.Add(tenant);
        context.Events.Add(eventEntity);
        context.Products.Add(product);
        context.BusinessParties.Add(businessParty);
        context.PriceLists.Add(priceList);
        await context.SaveChangesAsync();

        // Assegna BusinessParty con sconto 20%
        var relation = new PriceListBusinessParty
        {
            PriceListId = priceList.Id,
            BusinessPartyId = businessParty.Id,
            GlobalDiscountPercentage = 20m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        context.PriceListBusinessParties.Add(relation);

        var entry = new PriceListEntry
        {
            PriceListId = priceList.Id,
            ProductId = product.Id,
            Price = 100m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        context.PriceListEntries.Add(entry);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAppliedPriceAsync(product.Id, eventEntity.Id, businessParty.Id, null, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(80m, result.Price); // 100 - 20% = 80
        Assert.Equal(100m, result.OriginalPrice);
        Assert.Equal(20m, result.AppliedDiscountPercentage);
        Assert.Equal(businessParty.Id, result.BusinessPartyId);
        Assert.Equal("VIP Customer", result.BusinessPartyName);
        Assert.Contains("20", result.CalculationNotes);
    }

    [Fact]
    public async Task GetPurchasePriceComparison_ShouldReturnOrderedByPrice()
    {
        // Arrange
        await using var context = CreateContext();
        var mockAudit = new MockAuditLogService();
        var unitConversionService = new UnitConversionService();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, mockAudit, NullLogger<PriceListService>.Instance, unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id);
        var supplier1 = CreateBusinessParty(tenant.Id, "Supplier A");
        var supplier2 = CreateBusinessParty(tenant.Id, "Supplier B");

        var priceList1 = CreatePurchasePriceList(tenant.Id, "List A");
        var priceList2 = CreatePurchasePriceList(tenant.Id, "List B");

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.BusinessParties.AddRange(supplier1, supplier2);
        context.PriceLists.AddRange(priceList1, priceList2);
        await context.SaveChangesAsync();

        // Supplier A: Prezzo 50€
        context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            PriceListId = priceList1.Id,
            BusinessPartyId = supplier1.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        context.PriceListEntries.Add(new PriceListEntry
        {
            PriceListId = priceList1.Id,
            ProductId = product.Id,
            Price = 50m,
            Currency = "EUR",
            LeadTimeDays = 7,
            MinimumOrderQuantity = 10,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        // Supplier B: Prezzo 45€
        context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            PriceListId = priceList2.Id,
            BusinessPartyId = supplier2.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        context.PriceListEntries.Add(new PriceListEntry
        {
            PriceListId = priceList2.Id,
            ProductId = product.Id,
            Price = 45m,
            Currency = "EUR",
            LeadTimeDays = 10,
            MinimumOrderQuantity = 5,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetPurchasePriceComparisonAsync(product.Id, 1);

        // Assert
        Assert.Equal(2, result.Count);
        
        // Primo elemento = prezzo migliore (Supplier B)
        Assert.Equal(45m, result[0].Price);
        Assert.Equal("Supplier B", result[0].SupplierName);
        Assert.Equal(10, result[0].LeadTimeDays);
        Assert.Equal(5, result[0].MinimumOrderQuantity);
        
        // Secondo elemento = prezzo più alto (Supplier A)
        Assert.Equal(50m, result[1].Price);
        Assert.Equal("Supplier A", result[1].SupplierName);
        Assert.Equal(7, result[1].LeadTimeDays);
        Assert.Equal(10, result[1].MinimumOrderQuantity);
    }

    private static Server.Data.Entities.Auth.Tenant CreateTenant() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Tenant",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static Server.Data.Entities.Events.Event CreateEvent(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Test Event",
        ShortDescription = "Test",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static Server.Data.Entities.Products.Product CreateProduct(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Test Product",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static Server.Data.Entities.Business.BusinessParty CreateBusinessParty(Guid tenantId, string name) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        PartyType = ServerBusinessPartyType.Fornitore,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static Server.Data.Entities.PriceList.PriceList CreateSalesPriceList(Guid tenantId, Guid eventId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        EventId = eventId,
        Name = "Sales List",
        Type = PriceListType.Sales,
        Direction = PriceListDirection.Output,
        Status = Server.Data.Entities.PriceList.PriceListStatus.Active,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static Server.Data.Entities.PriceList.PriceList CreatePurchasePriceList(Guid tenantId, string name) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        Type = PriceListType.Purchase,
        Direction = PriceListDirection.Input,
        Status = Server.Data.Entities.PriceList.PriceListStatus.Active,
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
