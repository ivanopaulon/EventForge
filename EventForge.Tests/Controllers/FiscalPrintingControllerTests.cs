using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Controllers;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.FiscalPrinting;
using EventForge.Server.Services.Station;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="FiscalPrintingController"/> verifying
/// status code returns, cache interactions, and error handling.
/// </summary>
[Trait("Category", "Unit")]
public class FiscalPrintingControllerTests
{
    private readonly Mock<IFiscalPrinterService> _mockService;
    private readonly Mock<ILogger<FiscalPrintingController>> _mockLogger;
    private readonly Mock<IStationService> _mockStationService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly FiscalPrinterStatusCache _statusCache;
    private readonly FiscalPrintingController _controller;

    public FiscalPrintingControllerTests()
    {
        _mockService = new Mock<IFiscalPrinterService>();
        _mockLogger = new Mock<ILogger<FiscalPrintingController>>();
        _mockStationService = new Mock<IStationService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _statusCache = new FiscalPrinterStatusCache();

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Items).Returns(new Dictionary<object, object?>());
        mockHttpContext.Setup(c => c.Request.Path)
                       .Returns(new PathString("/api/v1/fiscal-printing/test"));

        var mockUser = new Mock<System.Security.Claims.ClaimsPrincipal>();
        mockUser.Setup(u => u.Identity!.Name).Returns("testuser");
        mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

        var tenantId = Guid.NewGuid();
        _mockTenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);
        _mockTenantContext.Setup(t => t.CanAccessTenantAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        _controller = new FiscalPrintingController(
            _mockService.Object,
            _statusCache,
            _mockStationService.Object,
            _mockTenantContext.Object,
            _mockAuditLogService.Object,
            _mockLogger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            }
        };
    }

    // -------------------------------------------------------------------------
    //  PrintReceiptAsync – happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PrintReceiptAsync_ValidReceipt_Returns200WithSuccessResult()
    {
        // Arrange
        var printerId = Guid.NewGuid();
        var receipt = BuildTestReceipt();
        var expected = new FiscalPrintResult { Success = true, ReceiptNumber = "0001", PrintDate = DateTime.UtcNow };

        _mockService.Setup(s => s.PrintReceiptAsync(printerId, receipt, default))
                    .ReturnsAsync(expected);

        // Act
        var actionResult = await _controller.PrintReceiptAsync(printerId, receipt);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<FiscalPrintResult>(ok.Value);
        Assert.True(result.Success);
        Assert.Equal("0001", result.ReceiptNumber);
    }

    // -------------------------------------------------------------------------
    //  PrintReceiptAsync – printer offline (service returns failure result)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PrintReceiptAsync_PrinterOffline_Returns200WithFailureResult()
    {
        // Service returns a FiscalPrintResult with Success=false (not an exception)
        var printerId = Guid.NewGuid();
        var receipt = BuildTestReceipt();
        var expected = new FiscalPrintResult
        {
            Success = false,
            ErrorMessage = "Communication error: Cannot connect to fiscal printer at 192.168.1.100:9100",
            PrintDate = DateTime.UtcNow
        };

        _mockService.Setup(s => s.PrintReceiptAsync(printerId, receipt, default))
                    .ReturnsAsync(expected);

        var actionResult = await _controller.PrintReceiptAsync(printerId, receipt);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<FiscalPrintResult>(ok.Value);
        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    // -------------------------------------------------------------------------
    //  GetStatus – cache hit
    // -------------------------------------------------------------------------

    [Fact]
    public void GetStatus_CachedEntry_Returns200WithStatus()
    {
        var printerId = Guid.NewGuid();
        var status = new FiscalPrinterStatus
        {
            IsOnline = true,
            PaperStatus = "OK",
            LastCheck = DateTime.UtcNow
        };
        _statusCache.UpdateStatus(printerId, status);

        var actionResult = _controller.GetStatus(printerId);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<FiscalPrinterStatus>(ok.Value);
        Assert.True(returned.IsOnline);
    }

    // -------------------------------------------------------------------------
    //  GetStatus – cache miss
    // -------------------------------------------------------------------------

    [Fact]
    public void GetStatus_NoCachedEntry_Returns404()
    {
        var actionResult = _controller.GetStatus(Guid.NewGuid());

        var problem = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    // -------------------------------------------------------------------------
    //  GetHealthAsync – printer online
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHealthAsync_PrinterOnline_Returns200WithSummaryOK()
    {
        var printerId = Guid.NewGuid();
        var testResult = new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        var status = new FiscalPrinterStatus
        {
            IsOnline = true,
            PaperStatus = "OK",
            LastCheck = DateTime.UtcNow
        };

        _mockService.Setup(s => s.TestConnectionAsync(printerId, default)).ReturnsAsync(testResult);
        _statusCache.UpdateStatus(printerId, status);

        var actionResult = await _controller.GetHealthAsync(printerId);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var health = Assert.IsType<FiscalPrinterHealthDto>(ok.Value);
        Assert.True(health.IsOnline);
        Assert.Equal("OK", health.Summary);
        Assert.False(health.HasCriticalIssue);
    }

    // -------------------------------------------------------------------------
    //  GetHealthAsync – printer offline
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHealthAsync_PrinterOffline_Returns200WithOfflineSummary()
    {
        var printerId = Guid.NewGuid();
        var testResult = new FiscalPrintResult
        {
            Success = false,
            ErrorMessage = "Cannot connect to printer",
            PrintDate = DateTime.UtcNow
        };

        _mockService.Setup(s => s.TestConnectionAsync(printerId, default)).ReturnsAsync(testResult);

        var actionResult = await _controller.GetHealthAsync(printerId);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var health = Assert.IsType<FiscalPrinterHealthDto>(ok.Value);
        Assert.False(health.IsOnline);
        Assert.Equal("Offline", health.Summary);
    }

    // -------------------------------------------------------------------------
    //  GetHealthAsync – critical issue
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHealthAsync_FiscalMemoryFull_Returns200WithCriticalSummary()
    {
        var printerId = Guid.NewGuid();
        _mockService.Setup(s => s.TestConnectionAsync(printerId, default))
                    .ReturnsAsync(new FiscalPrintResult { Success = true });

        _statusCache.UpdateStatus(printerId, new FiscalPrinterStatus
        {
            IsOnline = true,
            IsFiscalMemoryFull = true,
            LastCheck = DateTime.UtcNow
        });

        var actionResult = await _controller.GetHealthAsync(printerId);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var health = Assert.IsType<FiscalPrinterHealthDto>(ok.Value);
        Assert.True(health.HasCriticalIssue);
        Assert.Equal("Critical", health.Summary);
    }

    // -------------------------------------------------------------------------
    //  DailyClosureAsync – happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DailyClosureAsync_Success_Returns200()
    {
        var printerId = Guid.NewGuid();
        _mockService.Setup(s => s.DailyClosureAsync(printerId, default))
                    .ReturnsAsync(new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow });

        var actionResult = await _controller.DailyClosureAsync(printerId);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<FiscalPrintResult>(ok.Value);
        Assert.True(result.Success);
    }

    // -------------------------------------------------------------------------
    //  TestConnectionAsync – happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TestConnectionAsync_Success_Returns200()
    {
        var printerId = Guid.NewGuid();
        _mockService.Setup(s => s.TestConnectionAsync(printerId, default))
                    .ReturnsAsync(new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow });

        var actionResult = await _controller.TestConnectionAsync(printerId);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<FiscalPrintResult>(ok.Value);
        Assert.True(result.Success);
    }

    // -------------------------------------------------------------------------
    //  OpenDrawerAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OpenDrawerAsync_Success_Returns200()
    {
        var printerId = Guid.NewGuid();
        _mockService.Setup(s => s.OpenDrawerAsync(printerId, default))
                    .ReturnsAsync(new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow });

        var actionResult = await _controller.OpenDrawerAsync(printerId);

        Assert.IsType<OkObjectResult>(actionResult.Result);
    }

    // -------------------------------------------------------------------------
    //  FiscalPrinterHealthDto computed helpers
    // -------------------------------------------------------------------------

    [Fact]
    public void FiscalPrinterHealthDto_DailyClosureRequired_SummaryIsClosureRequired()
    {
        var health = new FiscalPrinterHealthDto
        {
            PrinterId = Guid.NewGuid(),
            IsOnline = true,
            CachedStatus = new FiscalPrinterStatus { IsDailyClosureRequired = true },
            CheckedAt = DateTime.UtcNow
        };

        Assert.Equal("Closure Required", health.Summary);
        Assert.True(health.IsDailyClosureRequired);
        Assert.False(health.HasCriticalIssue);
    }

    [Fact]
    public void FiscalPrinterHealthDto_PaperLow_SummaryIsWarning()
    {
        var health = new FiscalPrinterHealthDto
        {
            PrinterId = Guid.NewGuid(),
            IsOnline = true,
            CachedStatus = new FiscalPrinterStatus { IsPaperLow = true },
            CheckedAt = DateTime.UtcNow
        };

        Assert.Equal("Warning", health.Summary);
        Assert.False(health.HasCriticalIssue);
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static FiscalReceiptData BuildTestReceipt()
        => new()
        {
            Items =
            [
                new FiscalReceiptItem
                {
                    Description = "Prodotto Test",
                    Quantity = 1,
                    UnitPrice = 10.00m,
                    VatCode = 1,
                    Department = 1
                }
            ],
            Payments =
            [
                new FiscalPayment { Amount = 10.00m, MethodCode = 1, Description = "Contanti" }
            ]
        };
}
