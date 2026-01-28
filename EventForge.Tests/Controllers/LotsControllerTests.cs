using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Warehouse;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for LotsController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class LotsControllerTests
{
    private readonly Mock<ILotService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<LotsController>> _mockLogger;
    private readonly LotsController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public LotsControllerTests()
    {
        _mockService = new Mock<ILotService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<LotsController>>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);

        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new LotsController(
            _mockService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object)
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

    #region GetLots Tests

    [Fact]
    public async Task GetLots_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetLotsAsync(It.IsAny<PaginationParameters>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetLots(pagination, CancellationToken.None);

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
    public async Task GetLots_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters 
        { 
            Page = 1, 
            PageSize = 100,
            WasCapped = true,
            AppliedMaxPageSize = 50
        };
        var expectedResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetLotsAsync(It.IsAny<PaginationParameters>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetLots(pagination, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("50", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetLots_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto>(),
            TotalCount = 100,
            Page = 2,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetLotsAsync(It.IsAny<PaginationParameters>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetLots(pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetLotsAsync(
            It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 50),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLots_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto> { new LotDto { Id = Guid.NewGuid() } },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetLotsAsync(It.IsAny<PaginationParameters>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetLots(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<LotDto>>(okResult.Value);
        Assert.Single(pagedResult.Items);
    }

    [Fact]
    public async Task GetLots_PassesCancellationToken()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var cts = new CancellationTokenSource();
        var expectedResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetLotsAsync(It.IsAny<PaginationParameters>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetLots(pagination, cts.Token);

        // Assert
        _mockService.Verify(s => s.GetLotsAsync(
            It.IsAny<PaginationParameters>(),
            null,
            null,
            null,
            cts.Token), Times.Once);
    }

    #endregion

    #region GetLotsByProduct Tests

    [Fact]
    public async Task GetLotsByProduct_WithValidProductId_ReturnsLots()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto>(),
            TotalCount = 10,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetLotsByProductAsync(productId, It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetLotsByProduct(productId, pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        _mockService.Verify(s => s.GetLotsByProductAsync(productId, It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetLotsByWarehouse Tests

    [Fact]
    public async Task GetLotsByWarehouse_WithValidWarehouseId_ReturnsLots()
    {
        // Arrange
        var warehouseId = Guid.NewGuid();
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto>(),
            TotalCount = 15,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetLotsByWarehouseAsync(warehouseId, It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetLotsByWarehouse(warehouseId, pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        _mockService.Verify(s => s.GetLotsByWarehouseAsync(warehouseId, It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetExpiredLots Tests

    [Fact]
    public async Task GetExpiredLots_WithThreshold_FiltersCorrectly()
    {
        // Arrange
        var threshold = DateTime.UtcNow.AddDays(7);
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto>(),
            TotalCount = 5,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetExpiredLotsAsync(It.IsAny<DateTime?>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetExpiredLots(threshold, pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockService.Verify(s => s.GetExpiredLotsAsync(
            threshold,
            It.IsAny<PaginationParameters>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
