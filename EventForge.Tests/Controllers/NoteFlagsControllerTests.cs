using EventForge.DTOs.Common;
using EventForge.DTOs.Sales;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Sales;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for NoteFlagsController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class NoteFlagsControllerTests
{
    private readonly Mock<INoteFlagService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<NoteFlagsController>> _mockLogger;
    private readonly Mock<ICacheInvalidationService> _mockCacheInvalidation;
    private readonly NoteFlagsController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public NoteFlagsControllerTests()
    {
        _mockService = new Mock<INoteFlagService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<NoteFlagsController>>();
        _mockCacheInvalidation = new Mock<ICacheInvalidationService>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);
        
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new NoteFlagsController(
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

    [Fact]
    public async Task GetAll_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<NoteFlagDto>
        {
            Items = new List<NoteFlagDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetNoteFlagsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAll(pagination, CancellationToken.None);

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
    public async Task GetAll_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters 
        { 
            Page = 1, 
            PageSize = 5000,
            WasCapped = true,
            AppliedMaxPageSize = 1000
        };
        var expectedResult = new PagedResult<NoteFlagDto>
        {
            Items = new List<NoteFlagDto>(),
            TotalCount = 50,
            Page = 1,
            PageSize = 1000
        };
        _mockService.Setup(s => s.GetNoteFlagsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAll(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("1000", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetAll_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<NoteFlagDto>
        {
            Items = new List<NoteFlagDto>(),
            TotalCount = 150,
            Page = 2,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetNoteFlagsAsync(pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAll(pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetNoteFlagsAsync(pagination, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var noteFlagDtos = new List<NoteFlagDto>
        {
            new NoteFlagDto { Id = Guid.NewGuid(), Code = "URGENT", Name = "Urgent" },
            new NoteFlagDto { Id = Guid.NewGuid(), Code = "ALLERGY", Name = "Allergy" }
        };
        var expectedResult = new PagedResult<NoteFlagDto>
        {
            Items = noteFlagDtos,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetNoteFlagsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAll(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<NoteFlagDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
    }

    [Fact]
    public async Task GetAll_PassesCancellationToken()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var cancellationToken = new CancellationToken();
        var expectedResult = new PagedResult<NoteFlagDto>
        {
            Items = new List<NoteFlagDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetNoteFlagsAsync(It.IsAny<PaginationParameters>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAll(pagination, cancellationToken);

        // Assert
        _mockService.Verify(s => s.GetNoteFlagsAsync(It.IsAny<PaginationParameters>(), cancellationToken), Times.Once);
    }
}
