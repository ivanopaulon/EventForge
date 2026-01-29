using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for BrandsController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class BrandsControllerTests
{
    private readonly Mock<IBrandService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<BrandsController>> _mockLogger;
    private readonly Mock<ICacheInvalidationService> _mockCacheInvalidation;
    private readonly BrandsController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public BrandsControllerTests()
    {
        _mockService = new Mock<IBrandService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<BrandsController>>();
        _mockCacheInvalidation = new Mock<ICacheInvalidationService>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);

        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new BrandsController(
            _mockService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object,
            _mockCacheInvalidation.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            }
        };

        // Setup tenant context to return a valid tenant ID
        var tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);
        _mockTenantContext.Setup(t => t.CanAccessTenantAsync(It.IsAny<Guid>())).ReturnsAsync(true);
    }

    #region GetBrands Tests

    [Fact]
    public async Task GetBrands_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<BrandDto>
        {
            Items = new List<BrandDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetBrandsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetBrands(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("100", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Page"));
        Assert.Equal("1", _headers["X-Page"].ToString());
        Assert.True(_headers.ContainsKey("X-Page-Size"));
        Assert.Equal("20", _headers["X-Page-Size"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("5", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetBrands_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 100,
            WasCapped = true,
            AppliedMaxPageSize = 50
        };
        var expectedResult = new PagedResult<BrandDto>
        {
            Items = new List<BrandDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetBrandsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetBrands(pagination, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("50", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetBrands_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<BrandDto>
        {
            Items = new List<BrandDto>(),
            TotalCount = 100,
            Page = 2,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetBrandsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetBrands(pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetBrandsAsync(
            It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBrands_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<BrandDto>
        {
            Items = new List<BrandDto> { new BrandDto { Id = Guid.NewGuid() } },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetBrandsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetBrands(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<BrandDto>>(okResult.Value);
        Assert.Single(pagedResult.Items);
    }

    [Fact]
    public async Task GetBrands_PassesCancellationToken()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var cts = new CancellationTokenSource();
        var expectedResult = new PagedResult<BrandDto>
        {
            Items = new List<BrandDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetBrandsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetBrands(pagination, cts.Token);

        // Assert
        _mockService.Verify(s => s.GetBrandsAsync(
            It.IsAny<PaginationParameters>(),
            cts.Token), Times.Once);
    }

    #endregion

    #region GetActiveBrands Tests

    [Fact]
    public async Task GetActiveBrands_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<BrandDto>
        {
            Items = new List<BrandDto>(),
            TotalCount = 50,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetActiveBrandsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetActiveBrands(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("50", _headers["X-Total-Count"].ToString());
    }

    #endregion
}
