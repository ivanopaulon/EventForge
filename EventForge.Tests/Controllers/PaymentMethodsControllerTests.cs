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
/// Unit tests for PaymentMethodsController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class PaymentMethodsControllerTests
{
    private readonly Mock<IPaymentMethodService> _mockService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<PaymentMethodsController>> _mockLogger;
    private readonly PaymentMethodsController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public PaymentMethodsControllerTests()
    {
        _mockService = new Mock<IPaymentMethodService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<PaymentMethodsController>>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);
        
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new PaymentMethodsController(
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

    #region GetPaymentMethods Tests

    [Fact]
    public async Task GetPaymentMethods_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = new List<PaymentMethodDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetPaymentMethodsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaymentMethods(pagination, CancellationToken.None);

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
    public async Task GetPaymentMethods_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters 
        { 
            Page = 1, 
            PageSize = 5000,
            WasCapped = true,
            AppliedMaxPageSize = 1000
        };
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = new List<PaymentMethodDto>(),
            TotalCount = 50,
            Page = 1,
            PageSize = 1000
        };
        _mockService.Setup(s => s.GetPaymentMethodsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaymentMethods(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("1000", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetPaymentMethods_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = new List<PaymentMethodDto>(),
            TotalCount = 150,
            Page = 2,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetPaymentMethodsAsync(pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaymentMethods(pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetPaymentMethodsAsync(pagination, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaymentMethods_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var paymentMethodDtos = new List<PaymentMethodDto>
        {
            new PaymentMethodDto { Id = Guid.NewGuid(), Code = "CASH", Name = "Cash" },
            new PaymentMethodDto { Id = Guid.NewGuid(), Code = "CARD", Name = "Credit Card" }
        };
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = paymentMethodDtos,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetPaymentMethodsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaymentMethods(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<PaymentMethodDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
    }

    [Fact]
    public async Task GetPaymentMethods_PassesCancellationToken()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var cancellationToken = new CancellationToken();
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = new List<PaymentMethodDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetPaymentMethodsAsync(It.IsAny<PaginationParameters>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaymentMethods(pagination, cancellationToken);

        // Assert
        _mockService.Verify(s => s.GetPaymentMethodsAsync(It.IsAny<PaginationParameters>(), cancellationToken), Times.Once);
    }

    #endregion

    #region GetActivePaymentMethods Tests

    [Fact]
    public async Task GetActivePaymentMethods_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = new List<PaymentMethodDto>(),
            TotalCount = 50,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetActivePaymentMethodsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetActivePaymentMethods(pagination, CancellationToken.None);

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
    public async Task GetActivePaymentMethods_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters 
        { 
            Page = 1, 
            PageSize = 5000,
            WasCapped = true,
            AppliedMaxPageSize = 1000
        };
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = new List<PaymentMethodDto>(),
            TotalCount = 30,
            Page = 1,
            PageSize = 1000
        };
        _mockService.Setup(s => s.GetActivePaymentMethodsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetActivePaymentMethods(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("1000", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetActivePaymentMethods_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 50 };
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = new List<PaymentMethodDto>(),
            TotalCount = 25,
            Page = 1,
            PageSize = 50
        };
        _mockService.Setup(s => s.GetActivePaymentMethodsAsync(pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetActivePaymentMethods(pagination, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetActivePaymentMethodsAsync(pagination, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActivePaymentMethods_ReturnsPagedResult()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var paymentMethodDtos = new List<PaymentMethodDto>
        {
            new PaymentMethodDto { Id = Guid.NewGuid(), Code = "CASH", Name = "Cash", IsActive = true },
            new PaymentMethodDto { Id = Guid.NewGuid(), Code = "CARD", Name = "Credit Card", IsActive = true }
        };
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = paymentMethodDtos,
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetActivePaymentMethodsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetActivePaymentMethods(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<PaymentMethodDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
    }

    [Fact]
    public async Task GetActivePaymentMethods_PassesCancellationToken()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var cancellationToken = new CancellationToken();
        var expectedResult = new PagedResult<PaymentMethodDto>
        {
            Items = new List<PaymentMethodDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockService.Setup(s => s.GetActivePaymentMethodsAsync(It.IsAny<PaginationParameters>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetActivePaymentMethods(pagination, cancellationToken);

        // Assert
        _mockService.Verify(s => s.GetActivePaymentMethodsAsync(It.IsAny<PaginationParameters>(), cancellationToken), Times.Once);
    }

    #endregion
}
