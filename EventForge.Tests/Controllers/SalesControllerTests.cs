using EventForge.DTOs.Common;
using EventForge.DTOs.Sales;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Sales;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for SalesController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class SalesControllerTests
{
    private readonly Mock<ISaleSessionService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<SalesController>> _mockLogger;
    private readonly SalesController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public SalesControllerTests()
    {
        _mockService = new Mock<ISaleSessionService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<SalesController>>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);

        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new SalesController(
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

    #region GetPOSSessions Tests

    [Fact]
    public async Task GetPOSSessions_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = new List<SaleSessionDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetPOSSessionsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPOSSessions(pagination, CancellationToken.None);

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
    public async Task GetPOSSessions_WithCappedPageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 100,
            WasCapped = true,
            AppliedMaxPageSize = 50
        };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = new List<SaleSessionDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetPOSSessionsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPOSSessions(pagination, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("50", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetPOSSessions_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var sessions = new List<SaleSessionDto>
        {
            new SaleSessionDto { Id = Guid.NewGuid() },
            new SaleSessionDto { Id = Guid.NewGuid() }
        };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = sessions,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetPOSSessionsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPOSSessions(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<SaleSessionDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
    }

    #endregion

    #region GetSessionsByOperator Tests

    [Fact]
    public async Task GetSessionsByOperator_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = new List<SaleSessionDto>(),
            TotalCount = 50,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetSessionsByOperatorAsync(It.IsAny<Guid>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetSessionsByOperator(operatorId, pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("50", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("3", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetSessionsByOperator_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = new List<SaleSessionDto>(),
            TotalCount = 100,
            Page = 2,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetSessionsByOperatorAsync(It.IsAny<Guid>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetSessionsByOperator(operatorId, pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetSessionsByOperatorAsync(
            operatorId,
            It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetSessionsByDate Tests

    [Fact]
    public async Task GetSessionsByDate_WithDateRange_ReturnsCorrectHeaders()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = new List<SaleSessionDto>(),
            TotalCount = 30,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetSessionsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime?>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetSessionsByDate(startDate, endDate, pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("30", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("2", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetSessionsByDate_WithNullEndDate_CallsServiceWithNull()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = new List<SaleSessionDto>(),
            TotalCount = 30,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetSessionsByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime?>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetSessionsByDate(startDate, null, pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetSessionsByDateAsync(
            startDate,
            null,
            It.IsAny<PaginationParameters>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetOpenSessions Tests

    [Fact]
    public async Task GetOpenSessions_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = new List<SaleSessionDto>(),
            TotalCount = 15,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetOpenSessionsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetOpenSessions(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("15", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Page"));
        Assert.Equal("1", _headers["X-Page"].ToString());
    }

    [Fact]
    public async Task GetOpenSessions_ReturnsOnlyOpenSessions()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var openSessions = new List<SaleSessionDto>
        {
            new SaleSessionDto { Id = Guid.NewGuid(), ClosedAt = null }, // Open
            new SaleSessionDto { Id = Guid.NewGuid(), ClosedAt = null }  // Open
        };
        var expectedResult = new PagedResult<SaleSessionDto>
        {
            Items = openSessions,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetOpenSessionsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetOpenSessions(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<SaleSessionDto>>(okResult.Value);
        Assert.All(pagedResult.Items, session => Assert.Null(session.ClosedAt));
    }

    #endregion
}
