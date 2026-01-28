using EventForge.DTOs.Common;
using EventForge.DTOs.Sales;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Sales;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for TableManagementController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class TableManagementControllerTests
{
    private readonly Mock<ITableManagementService> _mockService;
    private readonly Mock<ILogger<TableManagementController>> _mockLogger;
    private readonly TableManagementController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public TableManagementControllerTests()
    {
        _mockService = new Mock<ITableManagementService>();
        _mockLogger = new Mock<ILogger<TableManagementController>>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);

        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new TableManagementController(
            _mockService.Object,
            _mockLogger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            }
        };
    }

    #region GetTables Tests

    [Fact]
    public async Task GetTables_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = new List<TableSessionDto>(),
            TotalCount = 50,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetTablesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetTables(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("50", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Page"));
        Assert.Equal("1", _headers["X-Page"].ToString());
        Assert.True(_headers.ContainsKey("X-Page-Size"));
        Assert.Equal("20", _headers["X-Page-Size"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("3", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetTables_WithCappedPageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 100,
            WasCapped = true,
            AppliedMaxPageSize = 50
        };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = new List<TableSessionDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetTablesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetTables(pagination, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("50", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetTables_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var tables = new List<TableSessionDto>
        {
            new TableSessionDto { Id = Guid.NewGuid(), TableNumber = "T1" },
            new TableSessionDto { Id = Guid.NewGuid(), TableNumber = "T2" }
        };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = tables,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetTablesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetTables(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<TableSessionDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
    }

    [Fact]
    public async Task GetTables_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = new List<TableSessionDto>(),
            TotalCount = 100,
            Page = 2,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetTablesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetTables(pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetTablesAsync(
            It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetTablesByZone Tests

    [Fact]
    public async Task GetTablesByZone_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var zone = "Sala Principale";
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = new List<TableSessionDto>(),
            TotalCount = 25,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetTablesByZoneAsync(It.IsAny<string>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetTablesByZone(zone, pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("25", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("2", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetTablesByZone_CallsServiceWithCorrectZone()
    {
        // Arrange
        var zone = "Terrazza";
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = new List<TableSessionDto>(),
            TotalCount = 10,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetTablesByZoneAsync(It.IsAny<string>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetTablesByZone(zone, pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetTablesByZoneAsync(
            zone,
            It.IsAny<PaginationParameters>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTablesByZone_ReturnsOnlyTablesInZone()
    {
        // Arrange
        var zone = "Bar";
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var tables = new List<TableSessionDto>
        {
            new TableSessionDto { Id = Guid.NewGuid(), TableNumber = "B1", Area = "Bar" },
            new TableSessionDto { Id = Guid.NewGuid(), TableNumber = "B2", Area = "Bar" }
        };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = tables,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetTablesByZoneAsync(It.IsAny<string>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetTablesByZone(zone, pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<TableSessionDto>>(okResult.Value);
        Assert.All(pagedResult.Items, table => Assert.Equal("Bar", table.Area));
    }

    #endregion

    #region GetAvailableTablesPaginated Tests

    [Fact]
    public async Task GetAvailableTablesPaginated_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = new List<TableSessionDto>(),
            TotalCount = 15,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetAvailableTablesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAvailableTablesPaginated(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("15", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Page"));
        Assert.Equal("1", _headers["X-Page"].ToString());
    }

    [Fact]
    public async Task GetAvailableTablesPaginated_ReturnsOnlyAvailableTables()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var availableTables = new List<TableSessionDto>
        {
            new TableSessionDto { Id = Guid.NewGuid(), TableNumber = "T1", Status = "Available", IsActive = true },
            new TableSessionDto { Id = Guid.NewGuid(), TableNumber = "T2", Status = "Available", IsActive = true }
        };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = availableTables,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetAvailableTablesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAvailableTablesPaginated(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<TableSessionDto>>(okResult.Value);
        Assert.All(pagedResult.Items, table =>
        {
            Assert.Equal("Available", table.Status);
            Assert.True(table.IsActive);
        });
    }

    [Fact]
    public async Task GetAvailableTablesPaginated_WithNoAvailableTables_ReturnsEmptyResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<TableSessionDto>
        {
            Items = new List<TableSessionDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetAvailableTablesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAvailableTablesPaginated(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<TableSessionDto>>(okResult.Value);
        Assert.Empty(pagedResult.Items);
        Assert.Equal(0, pagedResult.TotalCount);
    }

    #endregion
}
