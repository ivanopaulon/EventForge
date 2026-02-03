using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventForge.Tests.Services.PriceLists;

/// <summary>
/// Unit tests for bulk price update functionality in PriceListService.
/// </summary>
[Trait("Category", "Unit")]
public class PriceListServiceBulkUpdateTests
{
    private readonly ILogger<PriceListService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitConversionService _unitConversionService;

    public PriceListServiceBulkUpdateTests()
    {
        _logger = new LoggerFactory().CreateLogger<PriceListService>();
        _auditLogService = new MockAuditLogService();
        _unitConversionService = new MockUnitConversionService();
    }

    private EventForgeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new EventForgeDbContext(options);
    }

    private async Task<(EventForgeDbContext context, PriceList priceList, List<PriceListEntry> entries)> SeedTestDataAsync()
    {
        var context = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Create sample products
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Product 1",
            Code = "P001",
            DefaultPrice = 100m,
            CategoryNodeId = Guid.NewGuid(),
            BrandId = Guid.NewGuid()
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Product 2",
            Code = "P002",
            DefaultPrice = 50m,
            CategoryNodeId = Guid.NewGuid(),
            BrandId = Guid.NewGuid()
        };

        var product3 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Product 3",
            Code = "P003",
            DefaultPrice = 200m,
            CategoryNodeId = product1.CategoryNodeId, // Same category as product1
            BrandId = product2.BrandId // Same brand as product2
        };

        context.Products.AddRange(product1, product2, product3);

        // Create price list
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Test Price List",
            Code = "TEST001",
            EventId = eventId,
            Priority = 1,
            IsActive = true
        };
        context.PriceLists.Add(priceList);

        // Create price list entries
        var entries = new List<PriceListEntry>
        {
            new PriceListEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PriceListId = priceList.Id,
                ProductId = product1.Id,
                Price = 100m,
                Product = product1
            },
            new PriceListEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PriceListId = priceList.Id,
                ProductId = product2.Id,
                Price = 50m,
                Product = product2
            },
            new PriceListEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PriceListId = priceList.Id,
                ProductId = product3.Id,
                Price = 200m,
                Product = product3
            }
        };

        context.PriceListEntries.AddRange(entries);
        await context.SaveChangesAsync();

        return (context, priceList, entries);
    }

    #region Bulk Update Operation Tests

    [Fact]
    public async Task BulkUpdate_IncreaseByPercentage_UpdatesCorrectly()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByPercentage,
            Value = 10m, // 10% increase
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal(0, result.FailedCount);

        var updatedEntries = await context.PriceListEntries
            .Where(e => e.PriceListId == priceList.Id)
            .ToListAsync();

        Assert.Equal(110m, updatedEntries.First(e => e.ProductId == entries[0].ProductId).Price);
        Assert.Equal(55m, updatedEntries.First(e => e.ProductId == entries[1].ProductId).Price);
        Assert.Equal(220m, updatedEntries.First(e => e.ProductId == entries[2].ProductId).Price);
    }

    [Fact]
    public async Task BulkUpdate_DecreaseByPercentage_UpdatesCorrectly()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.DecreaseByPercentage,
            Value = 20m, // 20% decrease
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal(80m, (await context.PriceListEntries.FindAsync(entries[0].Id))!.Price);
        Assert.Equal(40m, (await context.PriceListEntries.FindAsync(entries[1].Id))!.Price);
        Assert.Equal(160m, (await context.PriceListEntries.FindAsync(entries[2].Id))!.Price);
    }

    [Fact]
    public async Task BulkUpdate_IncreaseByAmount_UpdatesCorrectly()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByAmount,
            Value = 10m, // +10 euros
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal(110m, (await context.PriceListEntries.FindAsync(entries[0].Id))!.Price);
        Assert.Equal(60m, (await context.PriceListEntries.FindAsync(entries[1].Id))!.Price);
        Assert.Equal(210m, (await context.PriceListEntries.FindAsync(entries[2].Id))!.Price);
    }

    [Fact]
    public async Task BulkUpdate_DecreaseByAmount_UpdatesCorrectly()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.DecreaseByAmount,
            Value = 10m, // -10 euros
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal(90m, (await context.PriceListEntries.FindAsync(entries[0].Id))!.Price);
        Assert.Equal(40m, (await context.PriceListEntries.FindAsync(entries[1].Id))!.Price);
        Assert.Equal(190m, (await context.PriceListEntries.FindAsync(entries[2].Id))!.Price);
    }

    [Fact]
    public async Task BulkUpdate_SetFixedPrice_UpdatesCorrectly()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.SetFixedPrice,
            Value = 99.99m,
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal(99.99m, (await context.PriceListEntries.FindAsync(entries[0].Id))!.Price);
        Assert.Equal(99.99m, (await context.PriceListEntries.FindAsync(entries[1].Id))!.Price);
        Assert.Equal(99.99m, (await context.PriceListEntries.FindAsync(entries[2].Id))!.Price);
    }

    [Fact]
    public async Task BulkUpdate_MultiplyBy_UpdatesCorrectly()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.MultiplyBy,
            Value = 1.5m,
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal(150m, (await context.PriceListEntries.FindAsync(entries[0].Id))!.Price);
        Assert.Equal(75m, (await context.PriceListEntries.FindAsync(entries[1].Id))!.Price);
        Assert.Equal(300m, (await context.PriceListEntries.FindAsync(entries[2].Id))!.Price);
    }

    #endregion

    #region Rounding Strategy Tests

    [Fact]
    public async Task ApplyRounding_ToNearest5Cents_RoundsCorrectly()
    {
        // Arrange
        var (context, priceList, _) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        // Add entry with odd price
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = priceList.TenantId,
            Name = "Test Product",
            Code = "TEST",
            DefaultPrice = 10.37m
        };
        context.Products.Add(product);

        var entry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            TenantId = priceList.TenantId,
            PriceListId = priceList.Id,
            ProductId = product.Id,
            Price = 10.37m,
            Product = product
        };
        context.PriceListEntries.Add(entry);
        await context.SaveChangesAsync();

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.SetFixedPrice,
            Value = 10.37m,
            RoundingStrategy = RoundingStrategy.ToNearest5Cents
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        var updatedEntry = await context.PriceListEntries.FindAsync(entry.Id);
        Assert.Equal(10.35m, updatedEntry!.Price); // Should round to nearest 0.05
    }

    [Fact]
    public async Task ApplyRounding_ToNearest10Cents_RoundsCorrectly()
    {
        // Arrange
        var (context, priceList, _) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = priceList.TenantId,
            Name = "Test Product",
            Code = "TEST",
            DefaultPrice = 10.37m
        };
        context.Products.Add(product);

        var entry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            TenantId = priceList.TenantId,
            PriceListId = priceList.Id,
            ProductId = product.Id,
            Price = 10.37m,
            Product = product
        };
        context.PriceListEntries.Add(entry);
        await context.SaveChangesAsync();

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.SetFixedPrice,
            Value = 10.37m,
            RoundingStrategy = RoundingStrategy.ToNearest10Cents
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        var updatedEntry = await context.PriceListEntries.FindAsync(entry.Id);
        Assert.Equal(10.40m, updatedEntry!.Price); // Should round to nearest 0.10
    }

    [Fact]
    public async Task ApplyRounding_ToNearest99Cents_RoundsCorrectly()
    {
        // Arrange
        var (context, priceList, _) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = priceList.TenantId,
            Name = "Test Product",
            Code = "TEST",
            DefaultPrice = 10.37m
        };
        context.Products.Add(product);

        var entry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            TenantId = priceList.TenantId,
            PriceListId = priceList.Id,
            ProductId = product.Id,
            Price = 10.37m,
            Product = product
        };
        context.PriceListEntries.Add(entry);
        await context.SaveChangesAsync();

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.SetFixedPrice,
            Value = 10.37m,
            RoundingStrategy = RoundingStrategy.ToNearest99Cents
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        var updatedEntry = await context.PriceListEntries.FindAsync(entry.Id);
        Assert.Equal(10.99m, updatedEntry!.Price); // Should be 10.99
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task BulkUpdate_WithCategoryFilter_UpdatesOnlyFilteredProducts()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var targetCategoryId = entries[0].Product!.CategoryNodeId!.Value;

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByPercentage,
            Value = 10m,
            CategoryIds = new List<Guid> { targetCategoryId },
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert - Should update 2 products (product1 and product3 share the same category)
        Assert.Equal(2, result.UpdatedCount);
    }

    [Fact]
    public async Task BulkUpdate_WithBrandFilter_UpdatesOnlyFilteredProducts()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var targetBrandId = entries[1].Product!.BrandId!.Value;

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByPercentage,
            Value = 10m,
            BrandIds = new List<Guid> { targetBrandId },
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert - Should update 2 products (product2 and product3 share the same brand)
        Assert.Equal(2, result.UpdatedCount);
    }

    [Fact]
    public async Task BulkUpdate_WithPriceRange_UpdatesOnlyInRange()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByPercentage,
            Value = 10m,
            MinPrice = 60m,
            MaxPrice = 150m,
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert - Should update only product1 (price 100)
        Assert.Equal(1, result.UpdatedCount);

        var updatedEntries = await context.PriceListEntries
            .Where(e => e.PriceListId == priceList.Id)
            .ToListAsync();

        Assert.Equal(110m, updatedEntries.First(e => e.ProductId == entries[0].ProductId).Price);
        Assert.Equal(50m, updatedEntries.First(e => e.ProductId == entries[1].ProductId).Price); // Unchanged
        Assert.Equal(200m, updatedEntries.First(e => e.ProductId == entries[2].ProductId).Price); // Unchanged
    }

    [Fact]
    public async Task BulkUpdate_WithCombinedFilters_UpdatesCorrectly()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var targetCategoryId = entries[0].Product!.CategoryNodeId!.Value;

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByPercentage,
            Value = 10m,
            CategoryIds = new List<Guid> { targetCategoryId },
            MinPrice = 150m, // This will exclude product1 (100) but include product3 (200)
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert - Should update only product3 (matches category AND price range)
        Assert.Equal(1, result.UpdatedCount);
    }

    #endregion

    #region Preview Tests

    [Fact]
    public async Task PreviewBulkUpdate_ReturnsCorrectPreview_WithoutSaving()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByPercentage,
            Value = 10m,
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var preview = await service.PreviewBulkUpdateAsync(priceList.Id, dto);

        // Assert
        Assert.Equal(3, preview.AffectedCount);
        Assert.Equal(3, preview.Changes.Count);

        // Verify preview calculations
        var change1 = preview.Changes.First(c => c.ProductId == entries[0].ProductId);
        Assert.Equal(100m, change1.CurrentPrice);
        Assert.Equal(110m, change1.NewPrice);
        Assert.Equal(10m, change1.ChangeAmount);
        Assert.Equal(10m, change1.ChangePercentage);

        // Verify database not modified
        var dbEntries = await context.PriceListEntries
            .Where(e => e.PriceListId == priceList.Id)
            .ToListAsync();

        Assert.Equal(100m, dbEntries.First(e => e.ProductId == entries[0].ProductId).Price);
        Assert.Equal(50m, dbEntries.First(e => e.ProductId == entries[1].ProductId).Price);
        Assert.Equal(200m, dbEntries.First(e => e.ProductId == entries[2].ProductId).Price);
    }

    [Fact]
    public async Task PreviewBulkUpdate_CalculatesCorrectTotals()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByPercentage,
            Value = 10m,
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var preview = await service.PreviewBulkUpdateAsync(priceList.Id, dto);

        // Assert
        Assert.Equal(350m, preview.TotalCurrentValue); // 100 + 50 + 200
        Assert.Equal(385m, preview.TotalNewValue); // 110 + 55 + 220
        Assert.Equal(10m, preview.AverageIncreasePercentage);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task BulkUpdate_NegativeResult_SkipsItem()
    {
        // Arrange
        var (context, priceList, entries) = await SeedTestDataAsync();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.DecreaseByAmount,
            Value = 60m, // This would make product2 (50) negative
            RoundingStrategy = RoundingStrategy.None
        };

        // Act
        var result = await service.BulkUpdatePricesAsync(priceList.Id, dto, "testuser");

        // Assert
        Assert.Equal(2, result.UpdatedCount); // product1 and product3
        Assert.Equal(1, result.FailedCount); // product2
        Assert.NotEmpty(result.Errors);

        // Verify product2 price unchanged
        var product2Entry = await context.PriceListEntries.FindAsync(entries[1].Id);
        Assert.Equal(50m, product2Entry!.Price); // Unchanged
    }

    [Fact]
    public async Task BulkUpdate_InvalidPriceList_ThrowsException()
    {
        // Arrange
        var context = CreateDbContext();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        var service = new PriceListService(context, _auditLogService, _logger, _unitConversionService, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);

        var dto = new BulkPriceUpdateDto
        {
            Operation = BulkUpdateOperation.IncreaseByPercentage,
            Value = 10m,
            RoundingStrategy = RoundingStrategy.None
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BulkUpdatePricesAsync(Guid.NewGuid(), dto, "testuser"));
    }

    #endregion

    #region Mock Services

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

        public Task<PagedResult<EventForge.DTOs.Audit.AuditTrailResponseDto>> SearchAuditTrailAsync(EventForge.DTOs.Audit.AuditTrailSearchDto searchDto, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResult<EventForge.DTOs.Audit.AuditTrailResponseDto>
            {
                Items = Enumerable.Empty<EventForge.DTOs.Audit.AuditTrailResponseDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            });
        }

        public Task<EventForge.DTOs.Audit.AuditTrailStatisticsDto> GetAuditTrailStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EventForge.DTOs.Audit.AuditTrailStatisticsDto());
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

    private class MockUnitConversionService : IUnitConversionService
    {
        public decimal ConvertQuantity(decimal quantity, decimal fromConversionFactor, decimal toConversionFactor, int decimalPlaces = 2)
        {
            return quantity;
        }

        public decimal ConvertToBaseUnit(decimal quantity, decimal conversionFactor, int decimalPlaces = 2)
        {
            return quantity;
        }

        public decimal ConvertFromBaseUnit(decimal baseQuantity, decimal conversionFactor, int decimalPlaces = 2)
        {
            return baseQuantity;
        }

        public bool IsValidConversionFactor(decimal conversionFactor)
        {
            return true;
        }

        public decimal ConvertPrice(decimal price, decimal fromConversionFactor, decimal toConversionFactor, int decimalPlaces = 2)
        {
            return price;
        }
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
