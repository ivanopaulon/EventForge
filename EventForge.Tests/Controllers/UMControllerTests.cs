using EventForge.DTOs.Common;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for UMController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class UMControllerTests
{
    private readonly Mock<IUMService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<UMController>> _mockLogger;
    private readonly UMController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public UMControllerTests()
    {
        _mockService = new Mock<IUMService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<UMController>>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);
        
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new UMController(
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

    [Fact]
    public async Task GetUnitsOfMeasure_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<UMDto>
        {
            Items = new List<UMDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitsOfMeasure(pagination, CancellationToken.None);

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
    public async Task GetUnitsOfMeasure_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters 
        { 
            Page = 1, 
            PageSize = 5000,
            WasCapped = true,
            AppliedMaxPageSize = 1000
        };
        var expectedResult = new PagedResult<UMDto>
        {
            Items = new List<UMDto>(),
            TotalCount = 50,
            Page = 1,
            PageSize = 1000
        };
        _mockService.Setup(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitsOfMeasure(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("1000", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetUnitsOfMeasure_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<UMDto>
        {
            Items = new List<UMDto>(),
            TotalCount = 150,
            Page = 2,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetUMsAsync(pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitsOfMeasure(pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetUMsAsync(pagination, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUnitsOfMeasure_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var umDtos = new List<UMDto>
        {
            new UMDto { Id = Guid.NewGuid(), Name = "Kilogram", Symbol = "kg" },
            new UMDto { Id = Guid.NewGuid(), Name = "Liter", Symbol = "l" }
        };
        var expectedResult = new PagedResult<UMDto>
        {
            Items = umDtos,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitsOfMeasure(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<UMDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
    }

    [Fact]
    public async Task GetUnitsOfMeasure_PassesCancellationToken()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var cancellationToken = new CancellationToken();
        var expectedResult = new PagedResult<UMDto>
        {
            Items = new List<UMDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitsOfMeasure(pagination, cancellationToken);

        // Assert
        _mockService.Verify(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), cancellationToken), Times.Once);
    }
}
