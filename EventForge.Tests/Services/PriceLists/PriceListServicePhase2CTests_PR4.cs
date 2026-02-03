using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Common;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.PriceLists;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Tests.Services.PriceLists;

[Trait("Category", "Unit")]
public class PriceListServicePhase2CTests_PR4
{
    private readonly DbContextOptions<EventForgeDbContext> _dbOptions;

    public PriceListServicePhase2CTests_PR4()
    {
        _dbOptions = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private EventForgeDbContext CreateContext() => new EventForgeDbContext(_dbOptions);

    #region Tests

    [Fact]
    public async Task GenerateFromPurchases_WithLastPurchasePrice_CreatesCorrectPriceList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        // Create 3 documents with different prices (different dates)
        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-10));
        var doc2 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-5));
        var doc3 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));

        context.DocumentHeaders.AddRange(doc1, doc2, doc3);

        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 10m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc2.Id, product.Id, 12m, 3m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc3.Id, product.Id, 15m, 2m, tenant.Id)); // Most recent

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            Description = "Test",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.True(priceList.IsGeneratedFromDocuments);
        Assert.Single(priceList.ProductPrices);
        Assert.Equal(15m, priceList.ProductPrices.First().Price); // Last purchase price
    }

    [Fact]
    public async Task GenerateFromPurchases_WithWeightedAverage_CalculatesCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-5));
        var doc2 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-3));

        context.DocumentHeaders.AddRange(doc1, doc2);

        // Weighted average: (10 * 100 + 20 * 50) / (100 + 50) = (1000 + 1000) / 150 = 13.33
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 10m, 100m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc2.Id, product.Id, 20m, 50m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.WeightedAveragePrice
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        var price = priceList.ProductPrices.First().Price;
        Assert.Equal(13.33m, Math.Round(price, 2));
    }

    [Fact]
    public async Task GenerateFromPurchases_WithSimpleAverage_CalculatesCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-5));
        var doc2 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-3));
        var doc3 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));

        context.DocumentHeaders.AddRange(doc1, doc2, doc3);

        // Simple average: (10 + 20 + 30) / 3 = 20
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 10m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc2.Id, product.Id, 20m, 3m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc3.Id, product.Id, 30m, 2m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.SimpleAveragePrice
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.Equal(20m, priceList.ProductPrices.First().Price);
    }

    [Fact]
    public async Task GenerateFromPurchases_WithLowestPrice_SelectsMinimum()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-5));
        var doc2 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-3));
        var doc3 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));

        context.DocumentHeaders.AddRange(doc1, doc2, doc3);

        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 15m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc2.Id, product.Id, 8m, 3m, tenant.Id)); // Lowest
        context.DocumentRows.Add(CreateDocumentRow(doc3.Id, product.Id, 12m, 2m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LowestPrice
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.Equal(8m, priceList.ProductPrices.First().Price);
    }

    [Fact]
    public async Task GenerateFromPurchases_WithHighestPrice_SelectsMaximum()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-5));
        var doc2 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-3));
        var doc3 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));

        context.DocumentHeaders.AddRange(doc1, doc2, doc3);

        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 15m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc2.Id, product.Id, 25m, 3m, tenant.Id)); // Highest
        context.DocumentRows.Add(CreateDocumentRow(doc3.Id, product.Id, 12m, 2m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.HighestPrice
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.Equal(25m, priceList.ProductPrices.First().Price);
    }

    [Fact]
    public async Task GenerateFromPurchases_WithMedianPrice_CalculatesMedianCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-6));
        var doc2 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-4));
        var doc3 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-2));

        context.DocumentHeaders.AddRange(doc1, doc2, doc3);

        // Median of 10, 20, 30 = 20
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 10m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc2.Id, product.Id, 20m, 3m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc3.Id, product.Id, 30m, 2m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.MedianPrice
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.Equal(20m, priceList.ProductPrices.First().Price);
    }

    [Fact]
    public async Task GenerateFromPurchases_WithMarkupAndRounding_AppliesCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);

        // Price 9.87, +10% = 10.857, rounded to .99 = 10.99
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 9.87m, 1m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice,
            MarkupPercentage = 10m,
            RoundingStrategy = RoundingStrategy.ToNearest99Cents
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.Equal(10.99m, priceList.ProductPrices.First().Price);
    }

    [Fact]
    public async Task GenerateFromPurchases_WithCategoryFilter_FiltersCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var category1 = CreateClassificationNode(tenant.Id, "Category 1");
        var category2 = CreateClassificationNode(tenant.Id, "Category 2");
        var product1 = CreateProduct(tenant.Id, 100m, "PROD001", category1.Id);
        var product2 = CreateProduct(tenant.Id, 200m, "PROD002", category2.Id);
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.ClassificationNodes.AddRange(category1, category2);
        context.Products.AddRange(product1, product2);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);

        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product1.Id, 10m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product2.Id, 20m, 3m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice,
            FilterByCategoryIds = new List<Guid> { category1.Id } // Only category 1
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.Single(priceList.ProductPrices); // Only product 1
        Assert.Equal(product1.Id, priceList.ProductPrices.First().ProductId);
    }

    [Fact]
    public async Task GenerateFromPurchases_WithMinimumQuantity_ExcludesLowVolume()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product1 = CreateProduct(tenant.Id, 100m, "PROD001");
        var product2 = CreateProduct(tenant.Id, 200m, "PROD002");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.AddRange(product1, product2);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);

        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product1.Id, 10m, 100m, tenant.Id)); // High volume
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product2.Id, 20m, 5m, tenant.Id)); // Low volume

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice,
            MinimumQuantity = 10m // Exclude product2
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.Single(priceList.ProductPrices); // Only product 1
        Assert.Equal(product1.Id, priceList.ProductPrices.First().ProductId);
    }

    [Fact]
    public async Task GenerateFromPurchases_OnlyActiveProducts_FiltersInactive()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var activeProduct = CreateProduct(tenant.Id, 100m, "PROD001", status: Server.Data.Entities.Products.ProductStatus.Active);
        var inactiveProduct = CreateProduct(tenant.Id, 200m, "PROD002", status: Server.Data.Entities.Products.ProductStatus.Deleted);
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.AddRange(activeProduct, inactiveProduct);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);

        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, activeProduct.Id, 10m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, inactiveProduct.Id, 20m, 3m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice,
            OnlyActiveProducts = true
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceListId);

        Assert.NotNull(priceList);
        Assert.Single(priceList.ProductPrices); // Only active product
        Assert.Equal(activeProduct.Id, priceList.ProductPrices.First().ProductId);
    }

    [Fact]
    public async Task UpdateFromPurchases_UpdatesExistingPrices()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);
        var priceList = CreatePurchasePriceList(tenant.Id, "Existing Price List");

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);
        context.PriceLists.Add(priceList);

        // Assign supplier to price list
        context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            PriceListId = priceList.Id,
            BusinessPartyId = supplier.Id,
            Status = PriceListBusinessPartyStatus.Active,
            TenantId = tenant.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        // Existing entry with old price
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product.Id, 10m));

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 15m, 5m, tenant.Id)); // New price

        await context.SaveChangesAsync();

        var dto = new UpdatePriceListFromPurchasesDto
        {
            PriceListId = priceList.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice
        };

        // Act
        var result = await service.UpdateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PricesUpdated);
        Assert.Equal(0, result.PricesAdded);
        Assert.Equal(0, result.PricesRemoved);

        var updatedList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceList.Id);

        Assert.NotNull(updatedList);
        Assert.Equal(15m, updatedList.ProductPrices.First().Price);
    }

    [Fact]
    public async Task UpdateFromPurchases_AddsNewProducts_WhenFlagTrue()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product1 = CreateProduct(tenant.Id, 100m, "PROD001");
        var product2 = CreateProduct(tenant.Id, 200m, "PROD002");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);
        var priceList = CreatePurchasePriceList(tenant.Id, "Existing Price List");

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.AddRange(product1, product2);
        context.DocumentTypes.Add(documentType);
        context.PriceLists.Add(priceList);

        context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            PriceListId = priceList.Id,
            BusinessPartyId = supplier.Id,
            Status = PriceListBusinessPartyStatus.Active,
            TenantId = tenant.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        // Only product1 exists in price list
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product1.Id, 10m));

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product1.Id, 15m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product2.Id, 25m, 3m, tenant.Id)); // New product

        await context.SaveChangesAsync();

        var dto = new UpdatePriceListFromPurchasesDto
        {
            PriceListId = priceList.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice,
            AddNewProducts = true
        };

        // Act
        var result = await service.UpdateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PricesUpdated);
        Assert.Equal(1, result.PricesAdded);

        var updatedList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceList.Id);

        Assert.NotNull(updatedList);
        Assert.Equal(2, updatedList.ProductPrices.Count);
    }

    [Fact]
    public async Task UpdateFromPurchases_RemovesObsoleteProducts_WhenFlagTrue()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product1 = CreateProduct(tenant.Id, 100m, "PROD001");
        var product2 = CreateProduct(tenant.Id, 200m, "PROD002");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);
        var priceList = CreatePurchasePriceList(tenant.Id, "Existing Price List");

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.AddRange(product1, product2);
        context.DocumentTypes.Add(documentType);
        context.PriceLists.Add(priceList);

        context.PriceListBusinessParties.Add(new PriceListBusinessParty
        {
            PriceListId = priceList.Id,
            BusinessPartyId = supplier.Id,
            Status = PriceListBusinessPartyStatus.Active,
            TenantId = tenant.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        // Both products in price list
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product1.Id, 10m));
        context.PriceListEntries.Add(CreatePriceListEntry(priceList.Id, product2.Id, 20m));

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);
        // Only product1 in recent documents
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product1.Id, 15m, 5m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new UpdatePriceListFromPurchasesDto
        {
            PriceListId = priceList.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice,
            RemoveObsoleteProducts = true
        };

        // Act
        var result = await service.UpdateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PricesUpdated);
        Assert.Equal(1, result.PricesRemoved);

        var updatedList = await context.PriceLists
            .Include(pl => pl.ProductPrices)
            .FirstOrDefaultAsync(pl => pl.Id == priceList.Id);

        Assert.NotNull(updatedList);
        var activeProducts = updatedList.ProductPrices.Where(p => !p.IsDeleted).ToList();
        Assert.Single(activeProducts);
        Assert.Equal(product1.Id, activeProducts.First().ProductId);
    }

    [Fact]
    public async Task PreviewGenerateFromPurchases_ReturnsCorrectStatistics()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product1 = CreateProduct(tenant.Id, 100m, "PROD001");
        var product2 = CreateProduct(tenant.Id, 200m, "PROD002");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.AddRange(product1, product2);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-5));
        var doc2 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.AddRange(doc1, doc2);

        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product1.Id, 10m, 5m, tenant.Id));
        context.DocumentRows.Add(CreateDocumentRow(doc2.Id, product1.Id, 15m, 3m, tenant.Id)); // product1 has 2 prices
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product2.Id, 20m, 2m, tenant.Id)); // product2 has 1 price

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice
        };

        // Act
        var preview = await service.PreviewGenerateFromPurchasesAsync(dto, CancellationToken.None);

        // Assert
        Assert.Equal(2, preview.TotalDocumentsAnalyzed);
        Assert.Equal(2, preview.TotalProductsFound);
        Assert.Equal(1, preview.ProductsWithMultiplePrices); // Only product1
        Assert.Equal(2, preview.ProductPreviews.Count);
    }

    [Fact]
    public async Task GenerateFromPurchases_ValidatesSupplierRequired()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = Guid.NewGuid(), // Non-existent supplier
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateFromPurchases_ValidatesDateRange()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(-30), // Invalid: ToDate before FromDate
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateFromPurchases_SavesMetadataCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 10m, 5m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.WeightedAveragePrice,
            RoundingStrategy = RoundingStrategy.ToNearest99Cents,
            MarkupPercentage = 10m
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "testuser", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists.FirstOrDefaultAsync(pl => pl.Id == priceListId);
        Assert.NotNull(priceList);
        Assert.NotNull(priceList.GenerationMetadata);
        Assert.Contains("\"Strategy\":1", priceList.GenerationMetadata); // WeightedAveragePrice enum value is 1
        Assert.Contains("\"Rounding\":5", priceList.GenerationMetadata); // ToNearest99Cents enum value is 5
        Assert.Equal("testuser", priceList.LastSyncedBy);
        Assert.NotNull(priceList.LastSyncedAt);
    }

    [Fact]
    public async Task GenerateFromPurchases_SetsIsGeneratedFromDocumentsFlag()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var tenant = CreateTenant();
        var supplier = CreateBusinessParty(tenant.Id, "Test Supplier");
        var product = CreateProduct(tenant.Id, 100m, "PROD001");
        var documentType = CreateDocumentType(tenant.Id, isStockIncrease: true);

        context.Tenants.Add(tenant);
        context.BusinessParties.Add(supplier);
        context.Products.Add(product);
        context.DocumentTypes.Add(documentType);

        var doc1 = CreateDocumentHeader(tenant.Id, supplier.Id, documentType.Id, DateTime.UtcNow.AddDays(-1));
        context.DocumentHeaders.Add(doc1);
        context.DocumentRows.Add(CreateDocumentRow(doc1.Id, product.Id, 10m, 5m, tenant.Id));

        await context.SaveChangesAsync();

        var dto = new GeneratePriceListFromPurchasesDto
        {
            Name = "Test Price List",
            SupplierId = supplier.Id,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CalculationStrategy = PriceCalculationStrategy.LastPurchasePrice
        };

        // Act
        var priceListId = await service.GenerateFromPurchasesAsync(dto, "test", CancellationToken.None);

        // Assert
        var priceList = await context.PriceLists.FirstOrDefaultAsync(pl => pl.Id == priceListId);
        Assert.NotNull(priceList);
        Assert.True(priceList.IsGeneratedFromDocuments);
        Assert.Equal(PriceListType.Purchase, priceList.Type);
        Assert.Equal(PriceListDirection.Input, priceList.Direction);
    }

    #endregion

    #region Helper Methods

    private static PriceListService CreateService(EventForgeDbContext context)
    {
        var mockAudit = new MockAuditLogService();
        var mockUnitConversion = new Server.Services.UnitOfMeasures.UnitConversionService();
        var mockGenerationService = new MockPriceListGenerationService();
        var mockCalculationService = new MockPriceCalculationService();
        var mockBusinessPartyService = new MockPriceListBusinessPartyService();
        var mockBulkOperationsService = new MockPriceListBulkOperationsService();
        return new PriceListService(context, mockAudit, Microsoft.Extensions.Logging.Abstractions.NullLogger<PriceListService>.Instance, mockUnitConversion, mockGenerationService, mockCalculationService, mockBusinessPartyService, mockBulkOperationsService);
    }

    private static Tenant CreateTenant() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Tenant",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static Product CreateProduct(
        Guid tenantId,
        decimal basePrice,
        string code = "TEST001",
        Guid? categoryNodeId = null,
        Server.Data.Entities.Products.ProductStatus status = Server.Data.Entities.Products.ProductStatus.Active) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = "Test Product " + code,
            DefaultPrice = basePrice,
            VatRateId = Guid.NewGuid(),
            CategoryNodeId = categoryNodeId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

    private static PriceList CreatePurchasePriceList(Guid tenantId, string name) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        Priority = 0,
        Status = Server.Data.Entities.PriceList.PriceListStatus.Active,
        Type = PriceListType.Purchase,
        Direction = PriceListDirection.Input,
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
        Status = Server.Data.Entities.PriceList.PriceListEntryStatus.Active,
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

    private static DocumentType CreateDocumentType(Guid tenantId, bool isStockIncrease) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Test Document Type",
        Code = "TEST",
        IsStockIncrease = isStockIncrease,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static DocumentHeader CreateDocumentHeader(Guid tenantId, Guid businessPartyId, Guid documentTypeId, DateTime date) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        BusinessPartyId = businessPartyId,
        DocumentTypeId = documentTypeId,
        Date = date,
        Number = "DOC-" + Guid.NewGuid().ToString().Substring(0, 8),
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static DocumentRow CreateDocumentRow(Guid documentHeaderId, Guid productId, decimal unitPrice, decimal quantity, Guid? tenantId = null)
    {
        return new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(), // Use provided or generate new
            DocumentHeaderId = documentHeaderId,
            ProductId = productId,
            UnitPrice = unitPrice,
            Quantity = quantity,
            Description = "Test Row",
            RowType = Server.Data.Entities.Documents.DocumentRowType.Product,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    private static ClassificationNode CreateClassificationNode(Guid tenantId, string name) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        Code = name,
        Type = ProductClassificationType.Category,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test"
    };

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
