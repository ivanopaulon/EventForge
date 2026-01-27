using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services.PriceLists;

public class PriceListValidationServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly PriceListValidationService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    
    public PriceListValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new EventForgeDbContext(options);
        
        var logger = new Mock<ILogger<PriceListValidationService>>();
        _service = new PriceListValidationService(_context, logger.Object);
    }
    
    [Fact]
    public async Task ValidatePriceListDateRange_ExpiredPriceList_ReturnsInvalid()
    {
        // Arrange
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Expired List",
            ValidFrom = DateTime.UtcNow.AddMonths(-2),
            ValidTo = DateTime.UtcNow.AddMonths(-1),
            Status = PriceListStatus.Active,
            Direction = EventForge.DTOs.Common.PriceListDirection.Output,
            Type = EventForge.DTOs.Common.PriceListType.Sales
        };
        
        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.ValidatePriceListDateRangeAsync(
            priceList.Id, 
            DateTime.UtcNow);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("PRICE_LIST_EXPIRED", result.ErrorCode);
        Assert.Contains("scaduto", result.ErrorMessage);
    }
    
    [Fact]
    public async Task ValidatePriceListDateRange_FuturePriceList_ReturnsInvalid()
    {
        // Arrange
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Future List",
            ValidFrom = DateTime.UtcNow.AddMonths(1),
            ValidTo = DateTime.UtcNow.AddMonths(3),
            Status = PriceListStatus.Active,
            Direction = EventForge.DTOs.Common.PriceListDirection.Output,
            Type = EventForge.DTOs.Common.PriceListType.Sales
        };
        
        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.ValidatePriceListDateRangeAsync(
            priceList.Id, 
            DateTime.UtcNow);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("PRICE_LIST_NOT_YET_VALID", result.ErrorCode);
        Assert.Contains("non ancora valido", result.ErrorMessage);
    }
    
    [Fact]
    public async Task ValidateNoDuplicateProduct_DuplicateExists_ReturnsInvalid()
    {
        // Arrange
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test List",
            Status = PriceListStatus.Active,
            Direction = EventForge.DTOs.Common.PriceListDirection.Output,
            Type = EventForge.DTOs.Common.PriceListType.Sales
        };
        
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Product",
            Code = "PROD-001",
            Status = ProductStatus.Active
        };
        
        var entry = new PriceListEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PriceListId = priceList.Id,
            ProductId = product.Id,
            Price = 100m,
            Currency = "EUR"
        };
        
        _context.PriceLists.Add(priceList);
        _context.Products.Add(product);
        _context.PriceListEntries.Add(entry);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.ValidateNoDuplicateProductAsync(
            priceList.Id, 
            product.Id);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("DUPLICATE_PRODUCT_IN_PRICE_LIST", result.ErrorCode);
        Assert.Contains("già presente", result.ErrorMessage);
    }
    
    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(1_500_000)]
    public void ValidatePriceValue_InvalidPrice_ReturnsInvalid(decimal price)
    {
        // Act
        var result = _service.ValidatePriceValue(price);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_PRICE_VALUE", result.ErrorCode);
    }
    
    [Fact]
    public void ValidatePriceValue_ValidPrice_ReturnsSuccess()
    {
        // Act
        var result = _service.ValidatePriceValue(49.99m);
        
        // Assert
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void ValidateQuantityRange_InvalidRange_ReturnsInvalid()
    {
        // Act
        var result = _service.ValidateQuantityRange(minQuantity: 10, maxQuantity: 5);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_QUANTITY_RANGE", result.ErrorCode);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("XYZ")]
    [InlineData("INVALID")]
    public void ValidateCurrency_InvalidCurrency_ReturnsInvalid(string currency)
    {
        // Act
        var result = _service.ValidateCurrency(currency);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.ErrorCode == "MISSING_CURRENCY" || result.ErrorCode == "UNSUPPORTED_CURRENCY");
    }
    
    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("CHF")]
    public void ValidateCurrency_ValidCurrency_ReturnsSuccess(string currency)
    {
        // Act
        var result = _service.ValidateCurrency(currency);
        
        // Assert
        Assert.True(result.IsValid);
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}
