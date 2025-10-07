using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Logs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Logs;

/// <summary>
/// Tests for the unified LogManagementService
/// </summary>
public class LogManagementServiceTests
{
    private readonly Mock<IApplicationLogService> _mockApplicationLogService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<LogManagementService>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly LogManagementService _service;

    public LogManagementServiceTests()
    {
        _mockApplicationLogService = new Mock<IApplicationLogService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<LogManagementService>>();
        _mockCache = new Mock<IMemoryCache>();

        // Create a simple configuration with the required connection string
        var configBuilder = new ConfigurationBuilder();
        _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:LogDB"] = "Data Source=test;Initial Catalog=test;Integrated Security=true;"
        });
        var configuration = configBuilder.Build();

        _service = new LogManagementService(
            _mockApplicationLogService.Object,
            _mockAuditLogService.Object,
            _mockLogger.Object,
            _mockCache.Object,
            configuration);
    }

    [Fact]
    public async Task GetApplicationLogsAsync_ShouldCallApplicationLogService()
    {
        // Arrange
        var queryParameters = new ApplicationLogQueryParameters { Page = 1, PageSize = 10 };
        var expectedResult = new PagedResult<SystemLogDto>
        {
            Items = new List<SystemLogDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _ = _mockApplicationLogService.Setup(s => s.GetPagedLogsAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetApplicationLogsAsync(queryParameters);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockApplicationLogService.Verify(s => s.GetPagedLogsAsync(queryParameters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessClientLogAsync_ShouldNotThrow_WithValidLog()
    {
        // Arrange
        var clientLog = new ClientLogDto
        {
            Level = "Information",
            Message = "Test message",
            Page = "/test",
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert
        await _service.ProcessClientLogAsync(clientLog, "TestUser");

        // Should not throw exception
    }

    [Fact]
    public async Task ProcessClientLogBatchAsync_ShouldProcessMultipleLogs()
    {
        // Arrange
        var clientLogs = new[]
        {
            new ClientLogDto { Level = "Information", Message = "Log 1", Timestamp = DateTime.UtcNow },
            new ClientLogDto { Level = "Warning", Message = "Log 2", Timestamp = DateTime.UtcNow },
            new ClientLogDto { Level = "Error", Message = "Log 3", Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = await _service.ProcessClientLogBatchAsync(clientLogs, "TestUser");

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(3, result.Results.Count);
        Assert.All(result.Results, r => Assert.Equal("success", r.Status));
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldCallAuditLogService()
    {
        // Arrange
        var searchDto = new AuditTrailSearchDto { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<AuditTrailResponseDto>
        {
            Items = new List<AuditTrailResponseDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _ = _mockAuditLogService.Setup(s => s.SearchAuditTrailAsync(searchDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetAuditLogsAsync(searchDto);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockAuditLogService.Verify(s => s.SearchAuditTrailAsync(searchDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldReturnResult()
    {
        // Act
        var result = await _service.GetSystemHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Status);
        Assert.NotNull(result.Details);
        // Health status will depend on database connectivity and environment
    }

    [Fact]
    public async Task ClearCacheAsync_ShouldNotThrow()
    {
        // Act & Assert
        await _service.ClearCacheAsync();

        // Should not throw exception
    }

    [Fact]
    public async Task ProcessClientLogAsync_ShouldThrow_WithNullLog()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ProcessClientLogAsync(null!, "TestUser"));
    }

    [Fact]
    public async Task GetAvailableLogLevelsAsync_ShouldReturnDefaultLevels_OnDatabaseError()
    {
        // This test checks that default levels are returned when database access fails
        // Act
        var result = await _service.GetAvailableLogLevelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Information", result);
        Assert.Contains("Warning", result);
        Assert.Contains("Error", result);
    }
}