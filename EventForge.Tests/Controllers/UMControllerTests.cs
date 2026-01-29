using EventForge.DTOs.Common;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.Interfaces;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for ProductManagementController unit of measure methods.
/// </summary>
[Trait("Category", "Unit")]
public class ProductManagementControllerUMTests
{
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<IBrandService> _mockBrandService;
    private readonly Mock<IModelService> _mockModelService;
    private readonly Mock<IUMService> _mockUMService;
    private readonly Mock<IPriceListService> _mockPriceListService;
    private readonly Mock<IPriceListGenerationService> _mockPriceListGenerationService;
    private readonly Mock<IPriceCalculationService> _mockPriceCalculationService;
    private readonly Mock<IPriceListBusinessPartyService> _mockPriceListBusinessPartyService;
    private readonly Mock<IPriceListBulkOperationsService> _mockPriceListBulkOperationsService;
    private readonly Mock<IPromotionService> _mockPromotionService;
    private readonly Mock<IBarcodeService> _mockBarcodeService;
    private readonly Mock<IDocumentHeaderService> _mockDocumentHeaderService;
    private readonly Mock<IStockMovementService> _mockStockMovementService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<ProductManagementController>> _mockLogger;
    private readonly Mock<IExportService> _mockExportService;
    private readonly ProductManagementController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public ProductManagementControllerUMTests()
    {
        _mockProductService = new Mock<IProductService>();
        _mockBrandService = new Mock<IBrandService>();
        _mockModelService = new Mock<IModelService>();
        _mockUMService = new Mock<IUMService>();
        _mockPriceListService = new Mock<IPriceListService>();
        _mockPriceListGenerationService = new Mock<IPriceListGenerationService>();
        _mockPriceCalculationService = new Mock<IPriceCalculationService>();
        _mockPriceListBusinessPartyService = new Mock<IPriceListBusinessPartyService>();
        _mockPriceListBulkOperationsService = new Mock<IPriceListBulkOperationsService>();
        _mockPromotionService = new Mock<IPromotionService>();
        _mockBarcodeService = new Mock<IBarcodeService>();
        _mockDocumentHeaderService = new Mock<IDocumentHeaderService>();
        _mockStockMovementService = new Mock<IStockMovementService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<ProductManagementController>>();
        _mockExportService = new Mock<IExportService>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);
        
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new ProductManagementController(
            _mockProductService.Object,
            _mockBrandService.Object,
            _mockModelService.Object,
            _mockUMService.Object,
            _mockPriceListService.Object,
            _mockPriceListGenerationService.Object,
            _mockPriceCalculationService.Object,
            _mockPriceListBusinessPartyService.Object,
            _mockPriceListBulkOperationsService.Object,
            _mockPromotionService.Object,
            _mockBarcodeService.Object,
            _mockDocumentHeaderService.Object,
            _mockStockMovementService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object,
            _mockExportService.Object)
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
    public async Task GetUnitOfMeasures_WithPagination_ReturnsCorrectHeaders()
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
        _mockUMService.Setup(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitOfMeasures(pagination, CancellationToken.None);

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
    public async Task GetUnitOfMeasures_WithLargePageSize_ReturnsCappedHeader()
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
        _mockUMService.Setup(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitOfMeasures(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("1000", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetUnitOfMeasures_CallsServiceWithCorrectParameters()
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
        _mockUMService.Setup(s => s.GetUMsAsync(pagination, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitOfMeasures(pagination, CancellationToken.None);

        // Assert
        _mockUMService.Verify(s => s.GetUMsAsync(pagination, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUnitOfMeasures_ReturnsPagedResult()
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
        _mockUMService.Setup(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitOfMeasures(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<UMDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
    }

    [Fact]
    public async Task GetUnitOfMeasures_PassesCancellationToken()
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
        _mockUMService.Setup(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnitOfMeasures(pagination, cancellationToken);

        // Assert
        _mockUMService.Verify(s => s.GetUMsAsync(It.IsAny<PaginationParameters>(), cancellationToken), Times.Once);
    }
}
