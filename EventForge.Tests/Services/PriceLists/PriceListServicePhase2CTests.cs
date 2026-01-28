using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.Audit;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Tests.Services.PriceLists;

[Trait("Category", "Unit")]
public class PriceListServicePhase2CTests
{
    private readonly DbContextOptions<EventForgeDbContext> _dbOptions;

    public PriceListServicePhase2CTests()
    {
        _dbOptions = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private EventForgeDbContext CreateContext() => new EventForgeDbContext(_dbOptions);

    [Fact]
    public async Task GetProductPrice_AutomaticMode_SelectsHighestPriorityPriceList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        
        var priceList1 = CreatePriceList(tenant.Id, "List Priority 10", priority: 10);
        var priceList2 = CreatePriceList(tenant.Id, "List Priority 5", priority: 5);
        var priceList3 = CreatePriceList(tenant.Id, "List Priority 1", priority: 1);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.PriceLists.AddRange(priceList1, priceList2, priceList3);

        context.PriceListEntries.Add(CreatePriceListEntry(priceList1.Id, product.Id, 10m));
        context.PriceListEntries.Add(CreatePriceListEntry(priceList2.Id, product.Id, 20m));
        context.PriceListEntries.Add(CreatePriceListEntry(priceList3.Id, product.Id, 30m));

        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.Automatic
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10m, result.FinalPrice);
        Assert.Equal(priceList1.Id, result.AppliedPriceListId);
        Assert.Equal("List Priority 10", result.AppliedPriceListName);
        Assert.Equal(PriceApplicationMode.Automatic, result.AppliedMode);
    }

    [Fact]
    public async Task GetProductPrice_AutomaticMode_PrefersBusinessPartySpecificOverGeneric()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var businessParty = CreateBusinessParty(tenant.Id);
        
        var genericPriceList = CreatePriceList(tenant.Id, "Generic Priority 10", priority: 10);
        var specificPriceList = CreatePriceList(tenant.Id, "Specific Priority 5", priority: 5);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.BusinessParties.Add(businessParty);
        context.PriceLists.AddRange(genericPriceList, specificPriceList);

        context.PriceListEntries.Add(CreatePriceListEntry(genericPriceList.Id, product.Id, 100m));
        context.PriceListEntries.Add(CreatePriceListEntry(specificPriceList.Id, product.Id, 80m));

        // Assign specific price list to business party
        context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            PriceListId = specificPriceList.Id,
            BusinessPartyId = businessParty.Id,
            Status = PriceListBusinessPartyStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            BusinessPartyId = businessParty.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.Automatic
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(80m, result.FinalPrice);
        Assert.Equal(specificPriceList.Id, result.AppliedPriceListId);
    }

    [Fact]
    public async Task GetProductPrice_AutomaticMode_UsesGenericWhenNoBusinessParty()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var businessParty = CreateBusinessParty(tenant.Id);
        
        var genericPriceList = CreatePriceList(tenant.Id, "Generic", priority: 5);
        var specificPriceList = CreatePriceList(tenant.Id, "Specific", priority: 10);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.BusinessParties.Add(businessParty);
        context.PriceLists.AddRange(genericPriceList, specificPriceList);

        context.PriceListEntries.Add(CreatePriceListEntry(genericPriceList.Id, product.Id, 100m));
        context.PriceListEntries.Add(CreatePriceListEntry(specificPriceList.Id, product.Id, 80m));

        // Assign specific price list to business party
        context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            PriceListId = specificPriceList.Id,
            BusinessPartyId = businessParty.Id,
            Status = PriceListBusinessPartyStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            // No BusinessPartyId specified
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.Automatic
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100m, result.FinalPrice);
        Assert.Equal(genericPriceList.Id, result.AppliedPriceListId);
    }

    [Fact]
    public async Task GetProductPrice_AutomaticMode_AppliesBusinessPartyDiscount()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var businessParty = CreateBusinessParty(tenant.Id);
        var priceList = CreatePriceList(tenant.Id, "Test List", priority: 10);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.BusinessParties.Add(businessParty);
        context.PriceLists.Add(priceList);

        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product.Id, 100m));

        // Assign price list with 10% discount
        context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            PriceListId = priceList.Id,
            BusinessPartyId = businessParty.Id,
            GlobalDiscountPercentage = 10m,
            Status = PriceListBusinessPartyStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            BusinessPartyId = businessParty.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.Automatic
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(90m, result.FinalPrice); // 100 - 10%
        Assert.Equal(100m, result.BasePriceFromPriceList);
        Assert.Equal(10m, result.AppliedDiscountPercentage);
    }

    [Fact]
    public async Task GetProductPrice_AutomaticMode_FallsBackToProductBasePrice()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 50m);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.Automatic
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50m, result.FinalPrice);
        Assert.Null(result.AppliedPriceListId);
        Assert.Contains("base price", result.SearchPath.First(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProductPrice_ForcedMode_IgnoresPriority()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        
        var lowPriorityList = CreatePriceList(tenant.Id, "Low Priority", priority: 1);
        var highPriorityList = CreatePriceList(tenant.Id, "High Priority", priority: 10);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.PriceLists.AddRange(lowPriorityList, highPriorityList);

        context.PriceListEntries.Add(CreatePriceListEntry(lowPriorityList.Id, product.Id, 75m));
        context.PriceListEntries.Add(CreatePriceListEntry(highPriorityList.Id, product.Id, 90m));

        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.ForcedPriceList,
            ForcedPriceListId = lowPriorityList.Id
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(75m, result.FinalPrice);
        Assert.Equal(lowPriorityList.Id, result.AppliedPriceListId);
        Assert.True(result.IsPriceListForced);
    }

    [Fact]
    public async Task GetProductPrice_ForcedMode_ThrowsWhenProductNotInPriceList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var priceList = CreatePriceList(tenant.Id, "Empty List", priority: 10);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.PriceLists.Add(priceList);
        // Not adding any price list entry for this product
        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.ForcedPriceList,
            ForcedPriceListId = priceList.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetProductPriceAsync(request));
    }

    [Fact]
    public async Task GetProductPrice_ManualMode_IgnoresPriceLists()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var priceList = CreatePriceList(tenant.Id, "Test List", priority: 10);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.PriceLists.Add(priceList);
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product.Id, 50m));
        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.Manual,
            ManualPrice = 25m
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25m, result.FinalPrice);
        Assert.True(result.IsManual);
        Assert.Null(result.AppliedPriceListId);
    }

    [Fact]
    public async Task GetProductPrice_ManualMode_ThrowsWhenManualPriceNotProvided()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.Manual
            // ManualPrice not provided
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetProductPriceAsync(request));
    }

    [Fact]
    public async Task GetProductPrice_HybridMode_UsesManualWhenProvided()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var priceList = CreatePriceList(tenant.Id, "Forced List", priority: 10);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.PriceLists.Add(priceList);
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product.Id, 50m));
        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.HybridForcedWithOverrides,
            ForcedPriceListId = priceList.Id,
            ManualPrice = 30m
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(30m, result.FinalPrice);
        Assert.True(result.IsManual);
        Assert.Equal(PriceApplicationMode.HybridForcedWithOverrides, result.AppliedMode);
    }

    [Fact]
    public async Task GetProductPrice_HybridMode_UsesForcedPriceListWhenNoManual()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var priceList = CreatePriceList(tenant.Id, "Forced List", priority: 10);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.PriceLists.Add(priceList);
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product.Id, 60m));
        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.HybridForcedWithOverrides,
            ForcedPriceListId = priceList.Id
            // ManualPrice not provided
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60m, result.FinalPrice);
        Assert.False(result.IsManual);
        Assert.Equal(priceList.Id, result.AppliedPriceListId);
        Assert.Equal(PriceApplicationMode.HybridForcedWithOverrides, result.AppliedMode);
    }

    [Fact]
    public async Task GetProductPrice_DetermineMode_UsesRequestOverride()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var businessParty = CreateBusinessParty(tenant.Id);
        var priceList = CreatePriceList(tenant.Id, "Test List");
        
        // Set BusinessParty to Automatic mode
        businessParty.DefaultPriceApplicationMode = PriceApplicationMode.Automatic;

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.BusinessParties.Add(businessParty);
        context.PriceLists.Add(priceList);
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product.Id, 50m));
        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            BusinessPartyId = businessParty.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.ForcedPriceList,
            ForcedPriceListId = priceList.Id
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PriceApplicationMode.ForcedPriceList, result.AppliedMode);
    }

    [Fact]
    public async Task GetProductPrice_DetermineMode_UsesBusinessPartyDefault()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        var businessParty = CreateBusinessParty(tenant.Id);
        var priceList = CreatePriceList(tenant.Id, "Forced List");
        
        // Set BusinessParty to ForcedPriceList mode
        businessParty.DefaultPriceApplicationMode = PriceApplicationMode.ForcedPriceList;
        businessParty.ForcedPriceListId = priceList.Id;

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.BusinessParties.Add(businessParty);
        context.PriceLists.Add(priceList);
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product.Id, 75m));
        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            BusinessPartyId = businessParty.Id,
            Quantity = 1
            // No PriceApplicationMode specified - should use BusinessParty default
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PriceApplicationMode.ForcedPriceList, result.AppliedMode);
        Assert.Equal(75m, result.FinalPrice);
    }

    [Fact]
    public async Task GetProductPrice_ReturnsAvailablePriceLists()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var product = CreateProduct(tenant.Id, 100m);
        
        var priceList1 = CreatePriceList(tenant.Id, "List 1", priority: 10);
        var priceList2 = CreatePriceList(tenant.Id, "List 2", priority: 5);
        var priceList3 = CreatePriceList(tenant.Id, "List 3", priority: 1);

        context.Tenants.Add(tenant);
        context.Products.Add(product);
        context.PriceLists.AddRange(priceList1, priceList2, priceList3);

        context.PriceListEntries.Add(CreatePriceListEntry(priceList1.Id, product.Id, 10m));
        context.PriceListEntries.Add(CreatePriceListEntry(priceList2.Id, product.Id, 20m));
        context.PriceListEntries.Add(CreatePriceListEntry(priceList3.Id, product.Id, 30m));

        await context.SaveChangesAsync();

        var request = new GetProductPriceRequestDto
        {
            ProductId = product.Id,
            Quantity = 1,
            PriceApplicationMode = PriceApplicationMode.Automatic
        };

        // Act
        var result = await service.GetProductPriceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.AvailablePriceLists.Count);
        Assert.Contains(result.AvailablePriceLists, pl => pl.PriceListId == priceList1.Id && pl.Price == 10m);
        Assert.Contains(result.AvailablePriceLists, pl => pl.PriceListId == priceList2.Id && pl.Price == 20m);
        Assert.Contains(result.AvailablePriceLists, pl => pl.PriceListId == priceList3.Id && pl.Price == 30m);
    }

    #region Helper Methods

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

    private static PriceListService CreateService(EventForgeDbContext context)
    {
        var mockAudit = new MockAuditLogService();
        var mockUnitConversion = new Server.Services.UnitOfMeasures.UnitConversionService();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        return new PriceListService(context, mockAudit, NullLogger<PriceListService>.Instance, mockUnitConversion, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);
    }

    private static Tenant CreateTenant() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Tenant",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static Product CreateProduct(Guid tenantId, decimal basePrice, string code = "TEST001") => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Code = code,
        Name = "Test Product",
        DefaultPrice = basePrice,
        VatRateId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static PriceList CreatePriceList(Guid tenantId, string name, int priority = 0) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        Priority = priority,
        Status = Server.Data.Entities.PriceList.PriceListStatus.Active,
        Type = PriceListType.Sales,
        Direction = PriceListDirection.Output,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static PriceListEntry CreatePriceListEntry(Guid priceListId, Guid productId, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        PriceListId = priceListId,
        ProductId = productId,
        Price = price,
        Currency = "EUR",
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

    #endregion
}
