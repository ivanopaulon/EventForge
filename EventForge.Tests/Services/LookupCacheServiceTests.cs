using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services;

/// <summary>
/// Tests for LookupCacheService to verify structured error handling, retry logic, and caching behavior
/// </summary>
[Trait("Category", "Unit")]
public class LookupCacheServiceTests
{
    private readonly Mock<IBrandService> _brandServiceMock;
    private readonly Mock<IModelService> _modelServiceMock;
    private readonly Mock<IFinancialService> _financialServiceMock;
    private readonly Mock<IUMService> _umServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<LookupCacheService>> _loggerMock;
    private readonly LookupCacheService _service;

    public LookupCacheServiceTests()
    {
        _brandServiceMock = new Mock<IBrandService>();
        _modelServiceMock = new Mock<IModelService>();
        _financialServiceMock = new Mock<IFinancialService>();
        _umServiceMock = new Mock<IUMService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<LookupCacheService>>();

        _service = new LookupCacheService(
            _brandServiceMock.Object,
            _modelServiceMock.Object,
            _financialServiceMock.Object,
            _umServiceMock.Object,
            _cache,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetBrandsAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var brands = new List<BrandDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Brand1" },
            new() { Id = Guid.NewGuid(), Name = "Brand2" }
        };
        var pagedResult = new PagedResult<BrandDto>
        {
            Items = brands,
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100)).ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetBrandsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Items.Count);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
        Assert.False(result.IsTransient);
    }

    [Fact]
    public async Task GetBrandsAsync_WithNullApiResponse_ReturnsTransientFailure()
    {
        // Arrange
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100)).ReturnsAsync((PagedResult<BrandDto>?)null);

        // Act
        var result = await _service.GetBrandsAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Items);
        Assert.Equal("NULL_RESPONSE", result.ErrorCode);
        Assert.Equal("Null API response", result.ErrorMessage);
        Assert.True(result.IsTransient);
    }

    [Fact]
    public async Task GetBrandsAsync_WithException_ReturnsUnhandledExceptionFailure()
    {
        // Arrange
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _service.GetBrandsAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Items);
        Assert.Equal("UNHANDLED_EXCEPTION", result.ErrorCode);
        Assert.Contains("Test exception", result.ErrorMessage);
        Assert.False(result.IsTransient);
    }

    [Fact]
    public async Task GetBrandsAsync_OnSuccess_CachesResult()
    {
        // Arrange
        var brands = new List<BrandDto> { new() { Id = Guid.NewGuid(), Name = "Brand1" } };
        var pagedResult = new PagedResult<BrandDto>
        {
            Items = brands,
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100)).ReturnsAsync(pagedResult);

        // Act
        var result1 = await _service.GetBrandsAsync();
        var result2 = await _service.GetBrandsAsync();

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        _brandServiceMock.Verify(x => x.GetBrandsAsync(1, 100), Times.Once); // Called only once
    }

    [Fact]
    public async Task GetBrandsAsync_OnFailure_DoesNotCacheResult()
    {
        // Arrange
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100)).ReturnsAsync((PagedResult<BrandDto>?)null);

        // Act
        var result1 = await _service.GetBrandsAsync();
        var result2 = await _service.GetBrandsAsync();

        // Assert
        Assert.False(result1.Success);
        Assert.False(result2.Success);
        _brandServiceMock.Verify(x => x.GetBrandsAsync(1, 100), Times.Exactly(2)); // Called twice
    }

    [Fact]
    public async Task GetBrandsAsync_WithForceRefresh_InvalidatesCache()
    {
        // Arrange
        var brands = new List<BrandDto> { new() { Id = Guid.NewGuid(), Name = "Brand1" } };
        var pagedResult = new PagedResult<BrandDto>
        {
            Items = brands,
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100)).ReturnsAsync(pagedResult);

        // Act
        await _service.GetBrandsAsync(); // First call - caches result
        await _service.GetBrandsAsync(forceRefresh: true); // Force refresh

        // Assert
        _brandServiceMock.Verify(x => x.GetBrandsAsync(1, 100), Times.Exactly(2)); // Called twice
    }

    [Fact]
    public async Task GetBrandsRawAsync_UnwrapsItems()
    {
        // Arrange
        var brands = new List<BrandDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Brand1" },
            new() { Id = Guid.NewGuid(), Name = "Brand2" }
        };
        var pagedResult = new PagedResult<BrandDto>
        {
            Items = brands,
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100)).ReturnsAsync(pagedResult);

        // Act
        var items = await _service.GetBrandsRawAsync();

        // Assert
        Assert.NotNull(items);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetModelsAsync_WithBrandId_FetchesModelsByBrand()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var models = new List<ModelDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Model1", BrandId = brandId }
        };
        var pagedResult = new PagedResult<ModelDto>
        {
            Items = models,
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };
        _modelServiceMock.Setup(x => x.GetModelsByBrandIdAsync(brandId, 1, 100)).ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetModelsAsync(brandId);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Items);
        _modelServiceMock.Verify(x => x.GetModelsByBrandIdAsync(brandId, 1, 100), Times.Once);
    }

    [Fact]
    public async Task GetVatRatesAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var vatRates = new List<VatRateDto>
        {
            new() { Id = Guid.NewGuid(), Percentage = 22.0m, Name = "Standard" }
        };
        var pagedResult = new PagedResult<VatRateDto>
        {
            Items = vatRates,
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };
        _financialServiceMock.Setup(x => x.GetVatRatesAsync(1, 100)).ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetVatRatesAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetUnitsOfMeasureAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var units = new List<UMDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Kilogram", Symbol = "kg" }
        };
        var pagedResult = new PagedResult<UMDto>
        {
            Items = units,
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };
        _umServiceMock.Setup(x => x.GetUMsAsync(1, 100)).ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetUnitsOfMeasureAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Items);
    }

    [Fact]
    public void ClearCache_RemovesAllCachedEntries()
    {
        // Arrange
        var brands = new List<BrandDto> { new() { Id = Guid.NewGuid(), Name = "Brand1" } };
        var pagedResult = new PagedResult<BrandDto>
        {
            Items = brands,
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100)).ReturnsAsync(pagedResult);

        // Act
        _service.GetBrandsAsync().Wait(); // Cache the result
        _service.ClearCache();
        _service.GetBrandsAsync().Wait(); // Should call API again

        // Assert
        _brandServiceMock.Verify(x => x.GetBrandsAsync(1, 100), Times.Exactly(2));
    }

    [Fact]
    public async Task GetBrandByIdAsync_FallsBackToDirectFetch_WhenNotInCache()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var brand = new BrandDto { Id = brandId, Name = "Brand1" };

        // Empty cache
        _brandServiceMock.Setup(x => x.GetBrandsAsync(1, 100))
            .ReturnsAsync(new PagedResult<BrandDto> { Items = new List<BrandDto>(), TotalCount = 0, Page = 1, PageSize = 100 });

        // Direct fetch returns the brand
        _brandServiceMock.Setup(x => x.GetBrandByIdAsync(brandId)).ReturnsAsync(brand);

        // Act
        var result = await _service.GetBrandByIdAsync(brandId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(brandId, result.Id);
        _brandServiceMock.Verify(x => x.GetBrandByIdAsync(brandId), Times.Once);
    }

    [Fact]
    public async Task LookupResult_Ok_CreatesSuccessResult()
    {
        // Arrange
        var items = new List<string> { "Item1", "Item2" };

        // Act
        var result = LookupResult<string>.Ok(items);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Items.Count);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
        Assert.False(result.IsTransient);
    }

    [Fact]
    public void LookupResult_Fail_CreatesFailureResult()
    {
        // Act
        var result = LookupResult<string>.Fail("Test error", "TEST_CODE", true);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Items);
        Assert.Equal("TEST_CODE", result.ErrorCode);
        Assert.Equal("Test error", result.ErrorMessage);
        Assert.True(result.IsTransient);
    }
}
