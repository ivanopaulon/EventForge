using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services;

/// <summary>
/// Unit tests for TransferOrderService to verify interface implementation and functionality.
/// These tests verify server-side pagination, search, and filtering logic.
/// </summary>
[Trait("Category", "Unit")]
public class TransferOrderServiceTests
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<ILogger<TransferOrderService>> _mockLogger;
    private readonly ITransferOrderService _service;

    public TransferOrderServiceTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockLogger = new Mock<ILogger<TransferOrderService>>();
        _service = new TransferOrderService(_mockHttpClient.Object, _mockLogger.Object);
    }

    #region GetTransferOrdersAsync Tests

    [Fact]
    public async Task GetTransferOrdersAsync_WithDefaultParameters_CallsCorrectUrl()
    {
        // Arrange
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            "api/v1/transferorder?page=1&pageSize=20",
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetTransferOrdersAsync();

        // Assert
        Assert.NotNull(result);
        _mockHttpClient.Verify(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            "api/v1/transferorder?page=1&pageSize=20",
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_WithSearchTerm_IncludesSearchInUrl()
    {
        // Arrange
        var searchTerm = "TO-123";
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>
            {
                new TransferOrderDto { Id = Guid.NewGuid(), Number = "TO-123" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"searchTerm={Uri.EscapeDataString(searchTerm)}")),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetTransferOrdersAsync(searchTerm: searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("TO-123", result.Items.First().Number);
        _mockHttpClient.Verify(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"searchTerm={Uri.EscapeDataString(searchTerm)}")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_WithSourceWarehouseFilter_IncludesSourceInUrl()
    {
        // Arrange
        var sourceWarehouseId = Guid.NewGuid();
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"sourceWarehouseId={sourceWarehouseId}")),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetTransferOrdersAsync(sourceWarehouseId: sourceWarehouseId);

        // Assert
        Assert.NotNull(result);
        _mockHttpClient.Verify(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"sourceWarehouseId={sourceWarehouseId}")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_WithDestinationWarehouseFilter_IncludesDestinationInUrl()
    {
        // Arrange
        var destinationWarehouseId = Guid.NewGuid();
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"destinationWarehouseId={destinationWarehouseId}")),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetTransferOrdersAsync(destinationWarehouseId: destinationWarehouseId);

        // Assert
        Assert.NotNull(result);
        _mockHttpClient.Verify(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"destinationWarehouseId={destinationWarehouseId}")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_WithStatusFilter_IncludesStatusInUrl()
    {
        // Arrange
        var status = "Pending";
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>
            {
                new TransferOrderDto { Id = Guid.NewGuid(), Status = "Pending" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"status={Uri.EscapeDataString(status)}")),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetTransferOrdersAsync(status: status);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Pending", result.Items.First().Status);
        _mockHttpClient.Verify(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"status={Uri.EscapeDataString(status)}")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_WithPaginationParameters_CallsCorrectPage()
    {
        // Arrange
        var page = 3;
        var pageSize = 50;
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>(),
            TotalCount = 150,
            Page = page,
            PageSize = pageSize
        };

        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url => url.Contains($"page={page}") && url.Contains($"pageSize={pageSize}")),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetTransferOrdersAsync(page: page, pageSize: pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(150, result.TotalCount);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_WithAllFilters_BuildsCompleteUrl()
    {
        // Arrange
        var page = 2;
        var pageSize = 25;
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var status = "Shipped";
        var searchTerm = "TEST";

        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };

        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url =>
                url.Contains($"page={page}") &&
                url.Contains($"pageSize={pageSize}") &&
                url.Contains($"sourceWarehouseId={sourceId}") &&
                url.Contains($"destinationWarehouseId={destId}") &&
                url.Contains($"status={Uri.EscapeDataString(status)}") &&
                url.Contains($"searchTerm={Uri.EscapeDataString(searchTerm)}")),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetTransferOrdersAsync(
            page: page,
            pageSize: pageSize,
            sourceWarehouseId: sourceId,
            destinationWarehouseId: destId,
            status: status,
            searchTerm: searchTerm);

        // Assert
        Assert.NotNull(result);
        _mockHttpClient.Verify(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.Is<string>(url =>
                url.Contains($"page={page}") &&
                url.Contains($"pageSize={pageSize}") &&
                url.Contains($"sourceWarehouseId={sourceId}") &&
                url.Contains($"destinationWarehouseId={destId}") &&
                url.Contains($"status={Uri.EscapeDataString(status)}") &&
                url.Contains($"searchTerm={Uri.EscapeDataString(searchTerm)}")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_WhenExceptionOccurs_ReturnsNull()
    {
        // Arrange
        _mockHttpClient.Setup(x => x.GetAsync<PagedResult<TransferOrderDto>>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _service.GetTransferOrdersAsync();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetTransferOrderAsync Tests

    [Fact]
    public async Task GetTransferOrderAsync_WithValidId_ReturnsTransferOrder()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new TransferOrderDto
        {
            Id = id,
            Number = "TO-001",
            Status = "Pending"
        };

        _mockHttpClient.Setup(x => x.GetAsync<TransferOrderDto>(
            $"api/v1/transferorder/{id}",
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.GetTransferOrderAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("TO-001", result.Number);
    }

    [Fact]
    public async Task GetTransferOrderAsync_WhenExceptionOccurs_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockHttpClient.Setup(x => x.GetAsync<TransferOrderDto>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ThrowsAsync(new Exception("Not found"));

        // Act
        var result = await _service.GetTransferOrderAsync(id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CancelTransferOrderAsync Tests

    [Fact]
    public async Task CancelTransferOrderAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockHttpClient.Setup(x => x.DeleteAsync(
            $"api/v1/transferorder/{id}/cancel",
            It.IsAny<CancellationToken>()
        )).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelTransferOrderAsync(id);

        // Assert
        Assert.True(result);
        _mockHttpClient.Verify(x => x.DeleteAsync(
            $"api/v1/transferorder/{id}/cancel",
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task CancelTransferOrderAsync_WhenExceptionOccurs_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockHttpClient.Setup(x => x.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ThrowsAsync(new Exception("Cancellation failed"));

        // Act
        var result = await _service.CancelTransferOrderAsync(id);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CreateTransferOrderAsync Tests

    [Fact]
    public async Task CreateTransferOrderAsync_WithValidDto_ReturnsCreatedOrder()
    {
        // Arrange
        var createDto = new CreateTransferOrderDto
        {
            SourceWarehouseId = Guid.NewGuid(),
            DestinationWarehouseId = Guid.NewGuid()
        };

        var expected = new TransferOrderDto
        {
            Id = Guid.NewGuid(),
            Number = "TO-NEW",
            Status = "Pending"
        };

        _mockHttpClient.Setup(x => x.PostAsync<CreateTransferOrderDto, TransferOrderDto>(
            "api/v1/transferorder",
            createDto,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.CreateTransferOrderAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TO-NEW", result.Number);
        Assert.Equal("Pending", result.Status);
    }

    #endregion

    #region ShipTransferOrderAsync Tests

    [Fact]
    public async Task ShipTransferOrderAsync_WithValidId_ReturnsShippedOrder()
    {
        // Arrange
        var id = Guid.NewGuid();
        var shipDto = new ShipTransferOrderDto();

        var expected = new TransferOrderDto
        {
            Id = id,
            Status = "Shipped"
        };

        _mockHttpClient.Setup(x => x.PostAsync<ShipTransferOrderDto, TransferOrderDto>(
            $"api/v1/transferorder/{id}/ship",
            shipDto,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.ShipTransferOrderAsync(id, shipDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Shipped", result.Status);
    }

    #endregion

    #region ReceiveTransferOrderAsync Tests

    [Fact]
    public async Task ReceiveTransferOrderAsync_WithValidId_ReturnsCompletedOrder()
    {
        // Arrange
        var id = Guid.NewGuid();
        var receiveDto = new ReceiveTransferOrderDto();

        var expected = new TransferOrderDto
        {
            Id = id,
            Status = "Completed"
        };

        _mockHttpClient.Setup(x => x.PostAsync<ReceiveTransferOrderDto, TransferOrderDto>(
            $"api/v1/transferorder/{id}/receive",
            receiveDto,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(expected);

        // Act
        var result = await _service.ReceiveTransferOrderAsync(id, receiveDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
    }

    #endregion
}
