using EventForge.DTOs.Common;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for EntityManagementController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class EntityManagementControllerTests
{
    private readonly Mock<IAddressService> _mockAddressService;
    private readonly Mock<IContactService> _mockContactService;
    private readonly Mock<IClassificationNodeService> _mockClassificationNodeService;
    private readonly Mock<IReferenceService> _mockReferenceService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<EntityManagementController>> _mockLogger;
    private readonly Mock<ICacheInvalidationService> _mockCacheInvalidation;
    private readonly EntityManagementController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public EntityManagementControllerTests()
    {
        _mockAddressService = new Mock<IAddressService>();
        _mockContactService = new Mock<IContactService>();
        _mockClassificationNodeService = new Mock<IClassificationNodeService>();
        _mockReferenceService = new Mock<IReferenceService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<EntityManagementController>>();
        _mockCacheInvalidation = new Mock<ICacheInvalidationService>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);
        
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new EntityManagementController(
            _mockAddressService.Object,
            _mockContactService.Object,
            _mockReferenceService.Object,
            _mockClassificationNodeService.Object,
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

    #region GetAddresses Tests

    [Fact]
    public async Task GetAddresses_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<AddressDto>
        {
            Items = new List<AddressDto>(),
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockAddressService.Setup(s => s.GetAddressesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAddresses(pagination, CancellationToken.None);

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
    public async Task GetAddresses_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 1000,
            WasCapped = true,
            AppliedMaxPageSize = 5000
        };
        var expectedResult = new PagedResult<AddressDto>
        {
            Items = new List<AddressDto>(),
            TotalCount = 50,
            Page = 1,
            PageSize = 1000
        };
        _mockAddressService.Setup(s => s.GetAddressesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAddresses(pagination, CancellationToken.None);

        // Assert
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("5000", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetAddresses_WithValidPagination_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<AddressDto>
        {
            Items = new List<AddressDto>(),
            TotalCount = 200,
            Page = 2,
            PageSize = 50
        };
        _mockAddressService.Setup(s => s.GetAddressesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetAddresses(pagination, CancellationToken.None);

        // Assert
        _mockAddressService.Verify(s => s.GetAddressesAsync(
            It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAddresses_ReturnsPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<AddressDto>
        {
            Items = new List<AddressDto>
            {
                new AddressDto { Id = Guid.NewGuid(), City = "Rome" }
            },
            TotalCount = 100,
            Page = 1,
            PageSize = 20
        };
        _mockAddressService.Setup(s => s.GetAddressesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAddresses(new PaginationParameters(), CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<AddressDto>>(okResult.Value);
        Assert.Equal(100, pagedResult.TotalCount);
        Assert.Equal(1, pagedResult.Page);
        Assert.Equal(20, pagedResult.PageSize);
        Assert.Single(pagedResult.Items);
    }

    [Fact]
    public async Task GetAddresses_WithCancellationToken_PassesToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var expectedResult = new PagedResult<AddressDto>
        {
            Items = new List<AddressDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockAddressService.Setup(s => s.GetAddressesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetAddresses(new PaginationParameters(), cts.Token);

        // Assert
        _mockAddressService.Verify(s => s.GetAddressesAsync(
            It.IsAny<PaginationParameters>(),
            cts.Token), Times.Once);
    }

    #endregion

    #region GetContacts Tests

    [Fact]
    public async Task GetContacts_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<ContactDto>
        {
            Items = new List<ContactDto>(),
            TotalCount = 75,
            Page = 1,
            PageSize = 20
        };
        _mockContactService.Setup(s => s.GetContactsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetContacts(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("75", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("4", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetContacts_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 3, PageSize = 25 };
        var expectedResult = new PagedResult<ContactDto>
        {
            Items = new List<ContactDto>(),
            TotalCount = 100,
            Page = 3,
            PageSize = 25
        };
        _mockContactService.Setup(s => s.GetContactsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetContacts(pagination, CancellationToken.None);

        // Assert
        _mockContactService.Verify(s => s.GetContactsAsync(
            It.Is<PaginationParameters>(p => p.Page == 3 && p.PageSize == 25),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetClassificationNodes Tests

    [Fact]
    public async Task GetClassificationNodes_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<ClassificationNodeDto>
        {
            Items = new List<ClassificationNodeDto>(),
            TotalCount = 150,
            Page = 1,
            PageSize = 20
        };
        _mockClassificationNodeService.Setup(s => s.GetClassificationNodesAsync(
                It.IsAny<PaginationParameters>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetClassificationNodes(pagination, null, "false", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("150", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("8", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetClassificationNodes_WithParentId_PassesToService()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var parentId = Guid.NewGuid();
        var expectedResult = new PagedResult<ClassificationNodeDto>
        {
            Items = new List<ClassificationNodeDto>(),
            TotalCount = 10,
            Page = 1,
            PageSize = 20
        };
        _mockClassificationNodeService.Setup(s => s.GetClassificationNodesAsync(
                It.IsAny<PaginationParameters>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetClassificationNodes(pagination, parentId, "false", CancellationToken.None);

        // Assert
        _mockClassificationNodeService.Verify(s => s.GetClassificationNodesAsync(
            It.Is<PaginationParameters>(p => p.Page == 1 && p.PageSize == 20),
            parentId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
