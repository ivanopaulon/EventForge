using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventForge.Tests.Services.PriceLists;

[Trait("Category", "Unit")]
public class PriceListBusinessPartyServiceTests
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

    private EventForgeDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new EventForgeDbContext(options);
    }

    [Fact]
    public async Task GetBusinessPartiesForPriceListAsync_ReturnsEmptyList_WhenNoAssignments()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new PriceListBusinessPartyService(context, new MockAuditLogService(), NullLogger<PriceListBusinessPartyService>.Instance);
        var priceListId = Guid.NewGuid();

        // Act
        var result = await service.GetBusinessPartiesForPriceListAsync(priceListId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBusinessPartiesForPriceListAsync_ReturnsAssignments_WhenExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new PriceListBusinessPartyService(context, new MockAuditLogService(), NullLogger<PriceListBusinessPartyService>.Instance);

        var priceListId = Guid.NewGuid();
        var businessPartyId = Guid.NewGuid();

        var businessParty = new BusinessParty
        {
            Id = businessPartyId,
            Name = "Test Customer",
            PartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType.Cliente,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        var assignment = new PriceListBusinessParty
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            BusinessPartyId = businessPartyId,
            BusinessParty = businessParty,
            Status = PriceListBusinessPartyStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            IsDeleted = false
        };

        context.BusinessParties.Add(businessParty);
        context.PriceListBusinessParties.Add(assignment);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetBusinessPartiesForPriceListAsync(priceListId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var dto = result.First();
        Assert.Equal(businessPartyId, dto.BusinessPartyId);
        Assert.Equal("Test Customer", dto.BusinessPartyName);
    }

    [Fact]
    public async Task AssignBusinessPartyAsync_ThrowsException_WhenPriceListNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new PriceListBusinessPartyService(context, new MockAuditLogService(), NullLogger<PriceListBusinessPartyService>.Instance);

        var dto = new AssignBusinessPartyToPriceListDto
        {
            BusinessPartyId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignBusinessPartyAsync(Guid.NewGuid(), dto, "test"));
    }

    [Fact]
    public async Task RemoveBusinessPartyAsync_ReturnsFalse_WhenAssignmentNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new PriceListBusinessPartyService(context, new MockAuditLogService(), NullLogger<PriceListBusinessPartyService>.Instance);

        // Act
        var result = await service.RemoveBusinessPartyAsync(Guid.NewGuid(), Guid.NewGuid(), "test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetPriceListsByBusinessPartyAsync_ReturnsEmptyList_WhenNoAssignments()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new PriceListBusinessPartyService(context, new MockAuditLogService(), NullLogger<PriceListBusinessPartyService>.Instance);

        // Act
        var result = await service.GetPriceListsByBusinessPartyAsync(Guid.NewGuid(), null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
