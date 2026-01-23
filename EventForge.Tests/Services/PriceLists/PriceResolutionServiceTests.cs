using EventForge.DTOs.Common;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.PriceLists;
using Microsoft.EntityFrameworkCore;

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
        _service = new PriceResolutionService(_context);

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
            PartyType = BusinessPartyType.Cliente,
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
            Status = PriceListStatus.Active,
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
            Status = PriceListStatus.Active,
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
            Status = PriceListStatus.Active,
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
            Status = PriceListStatus.Active,
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
            Status = PriceListStatus.Active,
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
            Status = PriceListStatus.Active,
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
            Status = PriceListStatus.Active,
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
            Status = PriceListStatus.Active,
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
}
