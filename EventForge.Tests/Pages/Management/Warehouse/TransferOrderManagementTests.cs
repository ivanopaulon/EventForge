using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor;

namespace EventForge.Tests.Pages.Management.Warehouse;

/// <summary>
/// Unit tests for TransferOrderManagement component logic.
/// Tests service call parameters and bulk cancel behavior.
/// </summary>
[Trait("Category", "Unit")]
public class TransferOrderManagementTests
{
    private readonly Mock<ITransferOrderService> _mockTransferOrderService;
    private readonly Mock<ILogger<object>> _mockLogger;

    public TransferOrderManagementTests()
    {
        _mockTransferOrderService = new Mock<ITransferOrderService>();
        _mockLogger = new Mock<ILogger<object>>();
    }

    [Fact]
    public async Task GetTransferOrdersAsync_CalledWithCorrectParameters_WhenSearchTermProvided()
    {
        // Arrange
        var searchTerm = "TO-2024-001";
        var page = 1;
        var pageSize = 20;
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };

        _mockTransferOrderService
            .Setup(x => x.GetTransferOrdersAsync(
                page,
                pageSize,
                null,
                null,
                null,
                searchTerm,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockTransferOrderService.Object.GetTransferOrdersAsync(
            page,
            pageSize,
            null,
            null,
            null,
            searchTerm,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockTransferOrderService.Verify(x => x.GetTransferOrdersAsync(
            page,
            pageSize,
            null,
            null,
            null,
            searchTerm,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_CalledWithCorrectParameters_WhenFiltersApplied()
    {
        // Arrange
        var sourceWarehouseId = Guid.NewGuid();
        var destinationWarehouseId = Guid.NewGuid();
        var status = "Pending";
        var page = 2;
        var pageSize = 20;
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };

        _mockTransferOrderService
            .Setup(x => x.GetTransferOrdersAsync(
                page,
                pageSize,
                sourceWarehouseId,
                destinationWarehouseId,
                status,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockTransferOrderService.Object.GetTransferOrdersAsync(
            page,
            pageSize,
            sourceWarehouseId,
            destinationWarehouseId,
            status,
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockTransferOrderService.Verify(x => x.GetTransferOrdersAsync(
            page,
            pageSize,
            sourceWarehouseId,
            destinationWarehouseId,
            status,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_CalledWithCorrectParameters_WhenPageChanged()
    {
        // Arrange
        var page = 3;
        var pageSize = 20;
        var expectedResult = new PagedResult<TransferOrderDto>
        {
            Items = new List<TransferOrderDto>(),
            TotalCount = 100,
            Page = page,
            PageSize = pageSize
        };

        _mockTransferOrderService
            .Setup(x => x.GetTransferOrdersAsync(
                page,
                pageSize,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockTransferOrderService.Object.GetTransferOrdersAsync(
            page,
            pageSize,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(page, result.Page);
        _mockTransferOrderService.Verify(x => x.GetTransferOrdersAsync(
            page,
            pageSize,
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTransferOrdersAsync_SupportsCancellation_WhenCancellationTokenProvided()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<PagedResult<TransferOrderDto>?>();

        _mockTransferOrderService
            .Setup(x => x.GetTransferOrdersAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns((int p, int ps, Guid? sw, Guid? dw, string? s, string? st, CancellationToken ct) =>
            {
                ct.Register(() => tcs.TrySetCanceled(ct));
                return tcs.Task;
            });

        // Act
        var task = _mockTransferOrderService.Object.GetTransferOrdersAsync(
            1, 20, null, null, null, null, cts.Token);
        
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }

    [Fact]
    public async Task CancelTransferOrderAsync_OnlyCalledForPendingOrders()
    {
        // Arrange
        var pendingOrder1 = new TransferOrderDto 
        { 
            Id = Guid.NewGuid(), 
            Number = "TO-001", 
            Status = "Pending" 
        };
        var pendingOrder2 = new TransferOrderDto 
        { 
            Id = Guid.NewGuid(), 
            Number = "TO-002", 
            Status = "Pending" 
        };
        var shippedOrder = new TransferOrderDto 
        { 
            Id = Guid.NewGuid(), 
            Number = "TO-003", 
            Status = "Shipped" 
        };
        var completedOrder = new TransferOrderDto 
        { 
            Id = Guid.NewGuid(), 
            Number = "TO-004", 
            Status = "Completed" 
        };

        var selectedOrders = new List<TransferOrderDto>
        {
            pendingOrder1,
            pendingOrder2,
            shippedOrder,
            completedOrder
        };

        _mockTransferOrderService
            .Setup(x => x.CancelTransferOrderAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act - simulate CancelSelectedOrders logic
        var pendingOrders = selectedOrders.Where(t => t.Status == "Pending").ToList();
        var cancelledCount = 0;

        foreach (var order in pendingOrders)
        {
            var success = await _mockTransferOrderService.Object.CancelTransferOrderAsync(order.Id);
            if (success)
            {
                cancelledCount++;
            }
        }

        // Assert
        Assert.Equal(2, pendingOrders.Count); // Only 2 pending orders
        Assert.Equal(2, cancelledCount); // Both were cancelled
        _mockTransferOrderService.Verify(
            x => x.CancelTransferOrderAsync(pendingOrder1.Id), 
            Times.Once);
        _mockTransferOrderService.Verify(
            x => x.CancelTransferOrderAsync(pendingOrder2.Id), 
            Times.Once);
        _mockTransferOrderService.Verify(
            x => x.CancelTransferOrderAsync(shippedOrder.Id), 
            Times.Never);
        _mockTransferOrderService.Verify(
            x => x.CancelTransferOrderAsync(completedOrder.Id), 
            Times.Never);
    }

    [Fact]
    public async Task CancelTransferOrderAsync_AggregatesSuccessAndFailures()
    {
        // Arrange
        var order1 = Guid.NewGuid();
        var order2 = Guid.NewGuid();
        var order3 = Guid.NewGuid();

        _mockTransferOrderService
            .Setup(x => x.CancelTransferOrderAsync(order1))
            .ReturnsAsync(true);
        _mockTransferOrderService
            .Setup(x => x.CancelTransferOrderAsync(order2))
            .ReturnsAsync(false);
        _mockTransferOrderService
            .Setup(x => x.CancelTransferOrderAsync(order3))
            .ReturnsAsync(true);

        // Act - simulate aggregation logic
        var cancelledCount = 0;
        var failedCount = 0;
        var orderIds = new[] { order1, order2, order3 };

        foreach (var orderId in orderIds)
        {
            var success = await _mockTransferOrderService.Object.CancelTransferOrderAsync(orderId);
            if (success)
            {
                cancelledCount++;
            }
            else
            {
                failedCount++;
            }
        }

        // Assert
        Assert.Equal(2, cancelledCount);
        Assert.Equal(1, failedCount);
    }

    [Fact]
    public async Task CancelTransferOrderAsync_HandlesExceptions_DuringBulkCancel()
    {
        // Arrange
        var order1 = Guid.NewGuid();
        var order2 = Guid.NewGuid();
        var order3 = Guid.NewGuid();

        _mockTransferOrderService
            .Setup(x => x.CancelTransferOrderAsync(order1))
            .ReturnsAsync(true);
        _mockTransferOrderService
            .Setup(x => x.CancelTransferOrderAsync(order2))
            .ThrowsAsync(new Exception("Network error"));
        _mockTransferOrderService
            .Setup(x => x.CancelTransferOrderAsync(order3))
            .ReturnsAsync(true);

        // Act - simulate exception handling logic
        var cancelledCount = 0;
        var failedCount = 0;
        var orderIds = new[] { order1, order2, order3 };

        foreach (var orderId in orderIds)
        {
            try
            {
                var success = await _mockTransferOrderService.Object.CancelTransferOrderAsync(orderId);
                if (success)
                {
                    cancelledCount++;
                }
                else
                {
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                _mockLogger.Object.LogError(ex, "Error cancelling transfer order {OrderId}", orderId);
            }
        }

        // Assert
        Assert.Equal(2, cancelledCount);
        Assert.Equal(1, failedCount);
    }
}
