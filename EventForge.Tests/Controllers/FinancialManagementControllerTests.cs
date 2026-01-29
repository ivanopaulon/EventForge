using EventForge.DTOs.Banks;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.VatRates;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Banks;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Services.VatRates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for FinancialManagementController pagination methods.
/// </summary>
[Trait("Category", "Unit")]
public class FinancialManagementControllerTests
{
    private readonly Mock<IBankService> _mockBankService;
    private readonly Mock<IPaymentTermService> _mockPaymentTermService;
    private readonly Mock<IVatRateService> _mockVatRateService;
    private readonly Mock<IVatNatureService> _mockVatNatureService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<FinancialManagementController>> _mockLogger;
    private readonly Mock<ICacheInvalidationService> _mockCacheInvalidation;
    private readonly FinancialManagementController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly HeaderDictionary _headers;

    public FinancialManagementControllerTests()
    {
        _mockBankService = new Mock<IBankService>();
        _mockPaymentTermService = new Mock<IPaymentTermService>();
        _mockVatRateService = new Mock<IVatRateService>();
        _mockVatNatureService = new Mock<IVatNatureService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<FinancialManagementController>>();
        _mockCacheInvalidation = new Mock<ICacheInvalidationService>();

        // Setup HttpContext mock for header testing
        _headers = new HeaderDictionary();
        _mockResponse = new Mock<HttpResponse>();
        _mockResponse.Setup(r => r.Headers).Returns(_headers);
        
        _mockHttpContext = new Mock<HttpContext>();
        _mockHttpContext.Setup(c => c.Response).Returns(_mockResponse.Object);

        _controller = new FinancialManagementController(
            _mockBankService.Object,
            _mockPaymentTermService.Object,
            _mockVatRateService.Object,
            _mockVatNatureService.Object,
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

    #region GetBanks Tests

    [Fact]
    public async Task GetBanks_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<BankDto>
        {
            Items = new List<BankDto>(),
            TotalCount = 80,
            Page = 1,
            PageSize = 20
        };
        _mockBankService.Setup(s => s.GetBanksAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetBanks(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("80", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Page"));
        Assert.Equal("1", _headers["X-Page"].ToString());
        Assert.True(_headers.ContainsKey("X-Page-Size"));
        Assert.Equal("20", _headers["X-Page-Size"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("4", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetBanks_WithLargePageSize_ReturnsCappedHeader()
    {
        // Arrange
        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 5000,
            WasCapped = true,
            AppliedMaxPageSize = 10000
        };
        var expectedResult = new PagedResult<BankDto>
        {
            Items = new List<BankDto>(),
            TotalCount = 25,
            Page = 1,
            PageSize = 5000
        };
        _mockBankService.Setup(s => s.GetBanksAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetBanks(pagination, CancellationToken.None);

        // Assert
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
        Assert.True(_headers.ContainsKey("X-Pagination-Applied-Max"));
        Assert.Equal("10000", _headers["X-Pagination-Applied-Max"].ToString());
    }

    [Fact]
    public async Task GetBanks_WithValidPagination_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 2, PageSize = 50 };
        var expectedResult = new PagedResult<BankDto>
        {
            Items = new List<BankDto>(),
            TotalCount = 150,
            Page = 2,
            PageSize = 50
        };
        _mockBankService.Setup(s => s.GetBanksAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetBanks(pagination, CancellationToken.None);

        // Assert
        _mockBankService.Verify(s => s.GetBanksAsync(
            It.Is<PaginationParameters>(p => p.Page == 2 && p.PageSize == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBanks_ReturnsPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<BankDto>
        {
            Items = new List<BankDto>
            {
                new BankDto { Id = Guid.NewGuid(), Name = "Test Bank" }
            },
            TotalCount = 50,
            Page = 1,
            PageSize = 20
        };
        _mockBankService.Setup(s => s.GetBanksAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetBanks(new PaginationParameters(), CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResult = Assert.IsType<PagedResult<BankDto>>(okResult.Value);
        Assert.Equal(50, pagedResult.TotalCount);
        Assert.Equal(1, pagedResult.Page);
        Assert.Equal(20, pagedResult.PageSize);
        Assert.Single(pagedResult.Items);
    }

    [Fact]
    public async Task GetBanks_WithCancellationToken_PassesToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var expectedResult = new PagedResult<BankDto>
        {
            Items = new List<BankDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _mockBankService.Setup(s => s.GetBanksAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetBanks(new PaginationParameters(), cts.Token);

        // Assert
        _mockBankService.Verify(s => s.GetBanksAsync(
            It.IsAny<PaginationParameters>(),
            cts.Token), Times.Once);
    }

    #endregion

    #region GetPaymentTerms Tests

    [Fact]
    public async Task GetPaymentTerms_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<PaymentTermDto>
        {
            Items = new List<PaymentTermDto>(),
            TotalCount = 35,
            Page = 1,
            PageSize = 20
        };
        _mockPaymentTermService.Setup(s => s.GetPaymentTermsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPaymentTerms(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("35", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("2", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetPaymentTerms_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 3, PageSize = 10 };
        var expectedResult = new PagedResult<PaymentTermDto>
        {
            Items = new List<PaymentTermDto>(),
            TotalCount = 100,
            Page = 3,
            PageSize = 10
        };
        _mockPaymentTermService.Setup(s => s.GetPaymentTermsAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetPaymentTerms(pagination, CancellationToken.None);

        // Assert
        _mockPaymentTermService.Verify(s => s.GetPaymentTermsAsync(
            It.Is<PaginationParameters>(p => p.Page == 3 && p.PageSize == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetVatRates Tests

    [Fact]
    public async Task GetVatRates_WithPagination_ReturnsCorrectHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 20 };
        var expectedResult = new PagedResult<VatRateDto>
        {
            Items = new List<VatRateDto>(),
            TotalCount = 12,
            Page = 1,
            PageSize = 20
        };
        _mockVatRateService.Setup(s => s.GetVatRatesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetVatRates(pagination, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(_headers.ContainsKey("X-Total-Count"));
        Assert.Equal("12", _headers["X-Total-Count"].ToString());
        Assert.True(_headers.ContainsKey("X-Total-Pages"));
        Assert.Equal("1", _headers["X-Total-Pages"].ToString());
    }

    [Fact]
    public async Task GetVatRates_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var pagination = new PaginationParameters { Page = 1, PageSize = 10 };
        var expectedResult = new PagedResult<VatRateDto>
        {
            Items = new List<VatRateDto>(),
            TotalCount = 12,
            Page = 1,
            PageSize = 10
        };
        _mockVatRateService.Setup(s => s.GetVatRatesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetVatRates(pagination, CancellationToken.None);

        // Assert
        _mockVatRateService.Verify(s => s.GetVatRatesAsync(
            It.Is<PaginationParameters>(p => p.Page == 1 && p.PageSize == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVatRates_WithCappedPagination_AddsHeaders()
    {
        // Arrange
        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 1000,
            WasCapped = true,
            AppliedMaxPageSize = 1000
        };
        var expectedResult = new PagedResult<VatRateDto>
        {
            Items = new List<VatRateDto>(),
            TotalCount = 12,
            Page = 1,
            PageSize = 1000
        };
        _mockVatRateService.Setup(s => s.GetVatRatesAsync(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetVatRates(pagination, CancellationToken.None);

        // Assert
        Assert.True(_headers.ContainsKey("X-Pagination-Capped"));
        Assert.Equal("true", _headers["X-Pagination-Capped"].ToString());
    }

    #endregion
}
