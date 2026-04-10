using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.PriceLists;
using Microsoft.EntityFrameworkCore;
using EntityBusinessPartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType;
using EntityPriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;

namespace EventForge.Tests.Services.PriceLists;

/// <summary>
/// Unit tests for PriceResolutionService.
/// Tests cascading price resolution logic for document row pricing.
/// </summary>
[Trait("Category", "Unit")]
public class PriceResolutionServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly PriceResolutionService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _businessPartyId = Guid.NewGuid();
    private readonly Guid _documentTypeId = Guid.NewGuid();

    public PriceResolutionServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: $"PriceResolutionTest_{Guid.NewGuid()}")
            .Options;

        _context = new EventForgeDbContext(options);
        _service = new PriceResolutionService(_context, Microsoft.Extensions.Logging.Abstractions.NullLogger<PriceResolutionService>.Instance);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed a product with default price
        var product = new Product
        {
            Id = _productId,
            Name = "Test Product",
            Code = "TEST001",
            DefaultPrice = 100m,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Products.Add(product);

        // Seed a business party
        var businessParty = new BusinessParty
        {
            Id = _businessPartyId,
            Name = "Test Customer",
            PartyType = EntityBusinessPartyType.Cliente,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.BusinessParties.Add(businessParty);

        // Seed document type for sales (stock decrease)
        var documentType = new DocumentType
        {
            Id = _documentTypeId,
            Name = "Sales Invoice",
            Code = "INV",
            IsStockIncrease = false, // Sales = stock decrease
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(documentType);

        _context.SaveChanges();
    }

    [Fact]
    public async Task ResolvePriceAsync_NoListsAvailable_ReturnsProductDefaultPrice()
    {
        // Act
        var result = await _service.ResolvePriceAsync(_productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100m, result.Price);
        Assert.Equal("DefaultPrice", result.Source);
        Assert.False(result.IsPriceFromList);
        Assert.Null(result.AppliedPriceListId);
    }

    [Fact]
    public async Task ResolvePriceAsync_ForcedPriceListId_UsesForcedList()
    {
        // Arrange
        var priceListId = Guid.NewGuid();
        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Forced Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var priceListEntry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 150m,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(priceListEntry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150m, result.Price);
        Assert.Equal("ParameterList", result.Source);
        Assert.True(result.IsPriceFromList);
        Assert.Equal(priceListId, result.AppliedPriceListId);
        Assert.Equal("Forced Price List", result.PriceListName);
    }

    [Fact]
    public async Task ResolvePriceAsync_DocumentHeaderPriceList_UsesDocumentList()
    {
        // Arrange
        var priceListId = Guid.NewGuid();
        var documentHeaderId = Guid.NewGuid();

        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Document Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var priceListEntry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 120m,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(priceListEntry);

        var documentHeader = new DocumentHeader
        {
            Id = documentHeaderId,
            DocumentTypeId = _documentTypeId,
            BusinessPartyId = _businessPartyId,
            Number = "DOC001",
            Date = DateTime.UtcNow,
            PriceListId = priceListId,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(documentHeader);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ResolvePriceAsync(_productId, documentHeaderId: documentHeaderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(120m, result.Price);
        Assert.Equal("DocumentList", result.Source);
        Assert.True(result.IsPriceFromList);
        Assert.Equal(priceListId, result.AppliedPriceListId);
    }

    [Fact]
    public async Task ResolvePriceAsync_BusinessPartyDefaultSalesList_UsesPartyList()
    {
        // Arrange
        var priceListId = Guid.NewGuid();

        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Customer Default Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var priceListEntry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 110m,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(priceListEntry);

        // Update business party with default sales price list
        var businessParty = await _context.BusinessParties.FindAsync(_businessPartyId);
        businessParty!.DefaultSalesPriceListId = priceListId;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ResolvePriceAsync(
            _productId,
            businessPartyId: _businessPartyId,
            direction: PriceListDirection.Output);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(110m, result.Price);
        Assert.Equal("PartyList", result.Source);
        Assert.True(result.IsPriceFromList);
        Assert.Equal(priceListId, result.AppliedPriceListId);
    }

    [Fact]
    public async Task ResolvePriceAsync_BusinessPartyDefaultPurchaseList_UsesPartyList()
    {
        // Arrange
        var priceListId = Guid.NewGuid();

        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Supplier Default Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Input,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var priceListEntry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 80m,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(priceListEntry);

        // Update business party with default purchase price list
        var businessParty = await _context.BusinessParties.FindAsync(_businessPartyId);
        businessParty!.DefaultPurchasePriceListId = priceListId;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ResolvePriceAsync(
            _productId,
            businessPartyId: _businessPartyId,
            direction: PriceListDirection.Input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(80m, result.Price);
        Assert.Equal("PartyList", result.Source);
        Assert.True(result.IsPriceFromList);
        Assert.Equal(priceListId, result.AppliedPriceListId);
    }

    [Fact]
    public async Task ResolvePriceAsync_GeneralActivePriceList_UsesGeneralList()
    {
        // Arrange
        var priceListId = Guid.NewGuid();

        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "General Sales Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            Priority = 1,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var priceListEntry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 105m,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(priceListEntry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ResolvePriceAsync(
            _productId,
            direction: PriceListDirection.Output);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(105m, result.Price);
        Assert.Equal("GeneralList", result.Source);
        Assert.True(result.IsPriceFromList);
        Assert.Equal(priceListId, result.AppliedPriceListId);
    }

    [Fact]
    public async Task ResolvePriceAsync_PriorityCascade_UsesForcedOverParty()
    {
        // Arrange - Create both forced and party price lists
        var forcedPriceListId = Guid.NewGuid();
        var partyPriceListId = Guid.NewGuid();

        // Forced price list
        var forcedPriceList = new PriceList
        {
            Id = forcedPriceListId,
            Name = "Forced Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(forcedPriceList);

        var forcedEntry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = forcedPriceListId,
            ProductId = _productId,
            Price = 150m,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(forcedEntry);

        // Party price list
        var partyPriceList = new PriceList
        {
            Id = partyPriceListId,
            Name = "Party Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(partyPriceList);

        var partyEntry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = partyPriceListId,
            ProductId = _productId,
            Price = 110m,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(partyEntry);

        // Set party default price list
        var businessParty = await _context.BusinessParties.FindAsync(_businessPartyId);
        businessParty!.DefaultSalesPriceListId = partyPriceListId;
        await _context.SaveChangesAsync();

        // Act - Use forced price list
        var result = await _service.ResolvePriceAsync(
            _productId,
            businessPartyId: _businessPartyId,
            forcedPriceListId: forcedPriceListId,
            direction: PriceListDirection.Output);

        // Assert - Should use parameter list (higher priority)
        Assert.NotNull(result);
        Assert.Equal(150m, result.Price);
        Assert.Equal("ParameterList", result.Source);
        Assert.Equal(forcedPriceListId, result.AppliedPriceListId);
    }

    [Fact]
    public async Task ResolvePriceAsync_ProductNotInList_FallsBackToDefault()
    {
        // Arrange - Create price list without the product
        var priceListId = Guid.NewGuid();
        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Empty Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ResolvePriceAsync(
            _productId,
            forcedPriceListId: priceListId);

        // Assert - Should fall back to default price
        Assert.NotNull(result);
        Assert.Equal(100m, result.Price);
        Assert.Equal("DefaultPrice", result.Source);
        Assert.False(result.IsPriceFromList);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task ResolvePriceAsync_WithQuantityInBracket_ReturnsMatchingEntry()
    {
        // Arrange: price list with two quantity brackets
        var priceListId = Guid.NewGuid();
        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Quantity Bracket List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        // Bracket 1: qty 1-9 → price 100
        var entry1 = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 100m,
            MinQuantity = 1,
            MaxQuantity = 9,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        // Bracket 2: qty 10+ → price 80
        var entry2 = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 80m,
            MinQuantity = 10,
            MaxQuantity = 0, // no upper limit
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.AddRange(entry1, entry2);
        await _context.SaveChangesAsync();

        // Act: qty=5 should hit bracket 1 (price 100)
        var result5 = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, quantity: 5m);

        // Act: qty=10 should hit bracket 2 (price 80)
        var result10 = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, quantity: 10m);

        // Assert
        Assert.NotNull(result5);
        Assert.Equal(100m, result5.Price);
        Assert.True(result5.IsPriceFromList);

        Assert.NotNull(result10);
        Assert.Equal(80m, result10.Price);
        Assert.True(result10.IsPriceFromList);
    }

    [Fact]
    public async Task ResolvePriceAsync_QuantityOutsideAllBrackets_FallsBackToDefaultPrice()
    {
        // Arrange: price list with a bracket that requires qty >= 5
        var priceListId = Guid.NewGuid();
        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "High Qty List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var entry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 70m,
            MinQuantity = 5,
            MaxQuantity = 0,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Act: qty=2 doesn't match any bracket → fall back to DefaultPrice
        var result = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, quantity: 2m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100m, result.Price); // product DefaultPrice
        Assert.Equal("DefaultPrice", result.Source);
        Assert.False(result.IsPriceFromList);
    }

    [Fact]
    public async Task ResolvePriceAsync_MultipleMatchingBrackets_SelectsMostSpecific()
    {
        // Arrange: overlapping entries — only possible when one bracket is contained in another.
        // The rule says: pick highest MinQuantity (most specific).
        var priceListId = Guid.NewGuid();
        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Overlapping Brackets List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        // Generic: qty 1+ → price 100
        var entryGeneric = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 100m,
            MinQuantity = 1,
            MaxQuantity = 0,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        // Specific: qty 10+ → price 75
        var entrySpecific = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 75m,
            MinQuantity = 10,
            MaxQuantity = 0,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.AddRange(entryGeneric, entrySpecific);
        await _context.SaveChangesAsync();

        // Act: qty=15 matches both; most specific (MinQuantity=10) should win
        var result = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, quantity: 15m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(75m, result.Price);
        Assert.True(result.IsPriceFromList);
    }

    #region ResolvePricesBatchAsync Tests

    [Fact]
    public async Task ResolvePricesBatchAsync_WithSingleItem_ReturnsResult()
    {
        // Arrange
        var request = new BatchPriceResolutionRequest
        {
            Items = new List<BatchPriceResolutionItem>
            {
                new BatchPriceResolutionItem
                {
                    Key = "item-1",
                    ProductId = _productId,
                    Quantity = 1m
                }
            }
        };

        // Act
        var response = await _service.ResolvePricesBatchAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, response.TotalProcessed);
        Assert.Equal(1, response.TotalSucceeded);
        Assert.Equal(0, response.TotalFailed);
        Assert.True(response.Results.ContainsKey("item-1"));
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task ResolvePricesBatchAsync_WithMultipleItems_ReturnsAllResults()
    {
        // Arrange: seed a price list with an entry for the test product
        var priceListId = Guid.NewGuid();
        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Batch Test List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);
        var entry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 88m,
            MinQuantity = 1,
            MaxQuantity = 0,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(entry);
        await _context.SaveChangesAsync();

        var request = new BatchPriceResolutionRequest
        {
            Items = new List<BatchPriceResolutionItem>
            {
                new BatchPriceResolutionItem { Key = "k1", ProductId = _productId, ForcedPriceListId = priceListId, Quantity = 1m },
                new BatchPriceResolutionItem { Key = "k2", ProductId = _productId, ForcedPriceListId = priceListId, Quantity = 5m },
                new BatchPriceResolutionItem { Key = "k3", ProductId = Guid.NewGuid(), Quantity = 1m }  // Unknown product → default 0
            }
        };

        // Act
        var response = await _service.ResolvePricesBatchAsync(request);

        // Assert
        Assert.Equal(3, response.TotalProcessed);
        Assert.Equal(3, response.TotalSucceeded);
        Assert.Equal(0, response.TotalFailed);
        Assert.Equal(88m, response.Results["k1"].Price);
        Assert.Equal(88m, response.Results["k2"].Price);
        Assert.Equal(0m, response.Results["k3"].Price);   // Unknown product → 0 fallback
    }

    [Fact]
    public async Task ResolvePricesBatchAsync_WithDuplicateKeys_BothKeysPresent()
    {
        // Arrange: two items with the same key but different quantities
        var request = new BatchPriceResolutionRequest
        {
            Items = new List<BatchPriceResolutionItem>
            {
                new BatchPriceResolutionItem { Key = "dup", ProductId = _productId, Quantity = 1m },
                new BatchPriceResolutionItem { Key = "dup", ProductId = _productId, Quantity = 2m }
            }
        };

        // Act: the batch runs both; the second result overwrites the first for the same key
        var response = await _service.ResolvePricesBatchAsync(request);

        // Assert: totals reflect both items were processed
        Assert.Equal(2, response.TotalProcessed);
        Assert.Equal(0, response.TotalFailed);
    }

    [Fact]
    public async Task ResolvePricesBatchAsync_EmptyItems_ReturnsEmptyResponse()
    {
        // Arrange
        var request = new BatchPriceResolutionRequest
        {
            Items = new List<BatchPriceResolutionItem>()
        };

        // Act
        var response = await _service.ResolvePricesBatchAsync(request);

        // Assert
        Assert.Equal(0, response.TotalProcessed);
        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal(0, response.TotalFailed);
        Assert.Empty(response.Results);
        Assert.Empty(response.Errors);
    }

    #endregion

    #region UoM Filter Tests (A3)

    [Fact]
    public async Task ResolvePriceAsync_WithMatchingUoM_ReturnsUoMSpecificEntry()
    {
        // Arrange: two entries for the same product/list — one with UoM, one without
        var priceListId = Guid.NewGuid();
        var uomId = Guid.NewGuid();

        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "UoM Price List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        // Entry without UoM → generic price
        var entryNoUom = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 100m,
            UnitOfMeasureId = null,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        // Entry with UoM → UoM-specific price
        var entryWithUom = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 90m,
            UnitOfMeasureId = uomId,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.AddRange(entryNoUom, entryWithUom);
        await _context.SaveChangesAsync();

        // Act: resolve with UoM specified
        var result = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, unitOfMeasureId: uomId);

        // Assert: UoM-specific entry is preferred
        Assert.NotNull(result);
        Assert.Equal(90m, result.Price);
        Assert.True(result.IsPriceFromList);
        Assert.Equal(uomId, result.AppliedUnitOfMeasureId);
    }

    [Fact]
    public async Task ResolvePriceAsync_WithUoMButNoMatchingEntry_FallsBackToEntryWithoutUoM()
    {
        // Arrange: only one entry without UoM
        var priceListId = Guid.NewGuid();
        var requestedUomId = Guid.NewGuid();

        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "UoM Fallback List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var entryNoUom = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 95m,
            UnitOfMeasureId = null,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(entryNoUom);
        await _context.SaveChangesAsync();

        // Act: request with UoM that has no matching entry
        var result = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, unitOfMeasureId: requestedUomId);

        // Assert: falls back to the entry without UoM constraint
        Assert.NotNull(result);
        Assert.Equal(95m, result.Price);
        Assert.True(result.IsPriceFromList);
        Assert.Null(result.AppliedUnitOfMeasureId);
    }

    [Fact]
    public async Task ResolvePriceAsync_WithoutUoM_IgnoresUoMFilterAndReturnsFirstMatch()
    {
        // Arrange: entry without UoM constraint
        var priceListId = Guid.NewGuid();

        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "No-UoM List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var entry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 120m,
            UnitOfMeasureId = null,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Act: no UoM specified
        var result = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, unitOfMeasureId: null);

        // Assert: price resolved normally, no UoM applied
        Assert.NotNull(result);
        Assert.Equal(120m, result.Price);
        Assert.True(result.IsPriceFromList);
        Assert.Null(result.AppliedUnitOfMeasureId);
    }

    [Fact]
    public async Task ResolvePricesBatchAsync_WithUoMInItem_PassesUoMToResolution()
    {
        // Arrange: price list with UoM-specific entry
        var priceListId = Guid.NewGuid();
        var uomId = Guid.NewGuid();

        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Batch UoM List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        var entryNoUom = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 100m,
            UnitOfMeasureId = null,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        var entryWithUom = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 85m,
            UnitOfMeasureId = uomId,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.AddRange(entryNoUom, entryWithUom);
        await _context.SaveChangesAsync();

        var request = new BatchPriceResolutionRequest
        {
            Items = new List<BatchPriceResolutionItem>
            {
                new BatchPriceResolutionItem { Key = "no-uom", ProductId = _productId, ForcedPriceListId = priceListId, Quantity = 1m, UnitOfMeasureId = null },
                new BatchPriceResolutionItem { Key = "with-uom", ProductId = _productId, ForcedPriceListId = priceListId, Quantity = 1m, UnitOfMeasureId = uomId }
            }
        };

        // Act
        var response = await _service.ResolvePricesBatchAsync(request);

        // Assert
        Assert.Equal(100m, response.Results["no-uom"].Price);
        Assert.Equal(85m, response.Results["with-uom"].Price);
        Assert.Equal(uomId, response.Results["with-uom"].AppliedUnitOfMeasureId);
        Assert.Null(response.Results["no-uom"].AppliedUnitOfMeasureId);
    }

    #endregion

    #region Applied Quantity Range in Result Tests (A4)

    [Fact]
    public async Task ResolvePriceAsync_WithQuantityBracket_ResultContainsAppliedMinMaxQuantity()
    {
        // Arrange: price list with two quantity brackets
        var priceListId = Guid.NewGuid();
        var priceList = new PriceList
        {
            Id = priceListId,
            Name = "Qty Range Result List",
            Status = EntityPriceListStatus.Active,
            Direction = PriceListDirection.Output,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceLists.Add(priceList);

        // Bracket: qty 1-9 → price 100
        var entry1 = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 100m,
            MinQuantity = 1,
            MaxQuantity = 9,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        // Bracket: qty 10+ → price 80
        var entry2 = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = _productId,
            Price = 80m,
            MinQuantity = 10,
            MaxQuantity = 0,
            TenantId = _tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.PriceListEntries.AddRange(entry1, entry2);
        await _context.SaveChangesAsync();

        // Act
        var result5 = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, quantity: 5m);
        var result10 = await _service.ResolvePriceAsync(_productId, forcedPriceListId: priceListId, quantity: 10m);

        // Assert: bracket 1 applied for qty=5
        Assert.Equal(100m, result5.Price);
        Assert.Equal(1, result5.AppliedMinQuantity);
        Assert.Equal(9, result5.AppliedMaxQuantity);

        // Assert: bracket 2 applied for qty=10
        Assert.Equal(80m, result10.Price);
        Assert.Equal(10, result10.AppliedMinQuantity);
        Assert.Equal(0, result10.AppliedMaxQuantity);
    }

    [Fact]
    public async Task ResolvePriceAsync_DefaultPriceFallback_AppliedQuantityRangeIsNull()
    {
        // Act: no price list available → falls back to product DefaultPrice
        var result = await _service.ResolvePriceAsync(_productId);

        // Assert: no quantity range was applied
        Assert.Equal(100m, result.Price);
        Assert.Equal("DefaultPrice", result.Source);
        Assert.Null(result.AppliedMinQuantity);
        Assert.Null(result.AppliedMaxQuantity);
    }

    #endregion
}
