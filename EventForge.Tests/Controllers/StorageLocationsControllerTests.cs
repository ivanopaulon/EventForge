using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for StorageLocationsController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class StorageLocationsControllerTests
{
    private readonly Mock<IStorageLocationService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<StorageLocationsController>> _mockLogger;
    private readonly StorageLocationsController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public StorageLocationsControllerTests()
    {
        _mockService = new Mock<IStorageLocationService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<StorageLocationsController>>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);

        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new StorageLocationsController(
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

    #region GetStorageLocations Tests

    [Fact]
    public async Task GetStorageLocations_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetStorageLocationsAsync(It.IsAny<PaginationParameters>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetStorageLocations(pagination, CancellationToken.None);

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
    public async Task GetStorageLocations_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 100,
            WasCapped = true,
            AppliedMaxPageSize = 50
        };
        var expectedResult = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetStorageLocationsAsync(It.IsAny<PaginationParameters>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetStorageLocations(pagination, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("50", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetStorageLocations_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>(),
            TotalCount = 100,
            Page = 2,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetStorageLocationsAsync(It.IsAny<PaginationParameters>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetStorageLocations(pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetStorageLocationsAsync(
            It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 50),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStorageLocations_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto> { new StorageLocationDto { Id = Guid.NewGuid() } },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetStorageLocationsAsync(It.IsAny<PaginationParameters>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetStorageLocations(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<StorageLocationDto>>(okResult.Value);
        Assert.Single(pagedResult.Items);
    }

    [Fact]
    public async Task GetStorageLocations_PassesCancellationToken()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var cts = new CancellationTokenSource();
        var expectedResult = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetStorageLocationsAsync(It.IsAny<PaginationParameters>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetStorageLocations(pagination, cts.Token);

        // Assert
        _mockService.Verify(s => s.GetStorageLocationsAsync(
            It.IsAny<PaginationParameters>(),
            null,
            cts.Token), Times.Once);
    }

    #endregion

    #region GetLocationsByZone Tests

    [Fact]
    public async Task GetLocationsByZone_WithValidZone_ReturnsLocations()
    {
        // Arrange
        var zone = "A";
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<StorageLocationDto>
        {
            Items = new List<StorageLocationDto>(),
            TotalCount = 10,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetLocationsByZoneAsync(zone, It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetLocationsByZone(zone, pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        _mockService.Verify(s => s.GetLocationsByZoneAsync(zone, It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
