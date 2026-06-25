using Microsoft.Extensions.Logging;
using Moq;
using Prym.DTOs.Common;
using Prym.DTOs.Products;
using Prym.DTOs.Warehouse;
using Prym.Web.Services;
using Prym.Web.ViewModels;

namespace EventForge.Tests.ViewModels;

/// <summary>
/// Unit tests for SerialDetailViewModel to verify implementation and business logic.
/// </summary>
[Trait("Category", "Unit")]
public class SerialDetailViewModelTests : IDisposable
{
    private readonly Mock<ISerialService> _mockSerialService;
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ILotService> _mockLotService;
    private readonly Mock<ILogger<SerialDetailViewModel>> _mockLogger;
    private readonly SerialDetailViewModel _viewModel;

    public SerialDetailViewModelTests()
    {
        _mockSerialService = new Mock<ISerialService>();
        _mockProductService = new Mock<IProductService>();
        _mockLotService = new Mock<ILotService>();
        _mockLogger = new Mock<ILogger<SerialDetailViewModel>>();
        _viewModel = new SerialDetailViewModel(
            _mockSerialService.Object,
            _mockProductService.Object,
            _mockLotService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoadAsync_WithValidId_LoadsEntity()
    {
        // Arrange
        var serialId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var expectedSerial = new SerialDto
        {
            Id = serialId,
            TenantId = Guid.NewGuid(),
            SerialNumber = "SN-001",
            ProductId = productId,
            ProductName = "Product A",
            ProductCode = "PROD-A",
            Status = "Available",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var productA = new ProductDto { Id = productId, Name = "Product A", Code = "PROD-A" };
        var lotsResult = new PagedResult<LotDto> { Items = new List<LotDto>(), TotalCount = 0, Page = 1, PageSize = 200 };

        _mockSerialService.Setup(s => s.GetSerialByIdAsync(serialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSerial);
        _mockProductService.Setup(s => s.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(productA);
        _mockLotService.Setup(s => s.GetLotsAsync(It.IsAny<int>(), It.IsAny<int>(), productId, It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lotsResult);

        // Act
        await _viewModel.LoadEntityAsync(serialId);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(serialId, _viewModel.Entity.Id);
        Assert.Equal("SN-001", _viewModel.Entity.SerialNumber);
        Assert.Equal(productId, _viewModel.Entity.ProductId);
        Assert.False(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.SelectedProduct);
        Assert.Equal(productId, _viewModel.SelectedProduct!.Id);
        Assert.Single(_viewModel.Products);
    }

    [Fact]
    public async Task CreateNewEntity_ReturnsDefaultSerial()
    {
        // Act
        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Assert
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(Guid.Empty, _viewModel.Entity.Id);
        Assert.Equal(string.Empty, _viewModel.Entity.SerialNumber);
        Assert.Equal("Available", _viewModel.Entity.Status);
        Assert.True(_viewModel.Entity.IsActive);
        Assert.True(_viewModel.IsNewEntity);
        // No lots preloaded when no product is selected
        _mockLotService.Verify(
            s => s.GetLotsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InitialProductId_NewEntity_SetsSelectedProductAndLoadsLots()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productA = new ProductDto { Id = productId, Name = "Product A", Code = "PROD-A" };
        var lot1 = new LotDto { Id = Guid.NewGuid(), Code = "LOT-001", ProductId = productId };
        var lotsResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto> { lot1 },
            TotalCount = 1,
            Page = 1,
            PageSize = 200
        };

        _viewModel.InitialProductId = productId;

        _mockProductService.Setup(s => s.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(productA);
        _mockLotService.Setup(s => s.GetLotsAsync(It.IsAny<int>(), It.IsAny<int>(), productId, It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lotsResult);

        // Act
        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Assert
        Assert.True(_viewModel.IsNewEntity);
        Assert.NotNull(_viewModel.SelectedProduct);
        Assert.Equal(productId, _viewModel.SelectedProduct!.Id);
        Assert.Equal(productId, _viewModel.Entity!.ProductId);
        Assert.NotNull(_viewModel.Lots);
        Assert.Single(_viewModel.Lots!);
    }

    [Fact]
    public async Task NewEntity_WithoutInitialProductId_DoesNotPreloadLots()
    {
        // Act — new entity with no pre-set product
        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Assert
        Assert.True(_viewModel.IsNewEntity);
        Assert.Null(_viewModel.SelectedProduct);
        // Lots must NOT be preloaded (avoids expensive unbounded query)
        _mockLotService.Verify(
            s => s.GetLotsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveAsync_NewEntity_CallsCreate()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        var productId = Guid.NewGuid();
        _viewModel.Entity!.SerialNumber = "SN-NEW";
        _viewModel.Entity.ProductId = productId;

        var createdSerial = new SerialDto
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            SerialNumber = "SN-NEW",
            ProductId = productId,
            Status = "Available",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockSerialService.Setup(s => s.CreateSerialAsync(It.IsAny<CreateSerialDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSerial);

        // Act
        var result = await _viewModel.SaveEntityAsync();

        // Assert
        Assert.True(result);
        Assert.NotNull(_viewModel.Entity);
        Assert.Equal(createdSerial.Id, _viewModel.Entity.Id);
        Assert.Equal("SN-NEW", _viewModel.Entity.SerialNumber);
        Assert.False(_viewModel.IsNewEntity);
        _mockSerialService.Verify(s => s.CreateSerialAsync(It.IsAny<CreateSerialDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchProductsAsync_PassesTermToServer()
    {
        // Arrange
        const string searchTerm = "sensor";
        var expectedProducts = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>
            {
                new ProductDto { Id = Guid.NewGuid(), Name = "Sensor A", Code = "S-A" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50
        };

        _mockProductService
            .Setup(s => s.GetProductsAsync(1, 50, searchTerm, It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProducts);

        await _viewModel.LoadEntityAsync(Guid.Empty);

        // Act
        var results = (await _viewModel.SearchProductsAsync(searchTerm)).ToList();

        // Assert
        Assert.Single(results);
        _mockProductService.Verify(
            s => s.GetProductsAsync(1, 50, searchTerm, It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetSelectedProductAsync_UpdatesProductsAndLoadsLots()
    {
        // Arrange
        await _viewModel.LoadEntityAsync(Guid.Empty);

        var productId = Guid.NewGuid();
        var product = new ProductDto { Id = productId, Name = "Product X", Code = "X" };
        var lot = new LotDto { Id = Guid.NewGuid(), Code = "LOT-X", ProductId = productId };
        var lotsResult = new PagedResult<LotDto>
        {
            Items = new List<LotDto> { lot },
            TotalCount = 1,
            Page = 1,
            PageSize = 200
        };

        _mockLotService.Setup(s => s.GetLotsAsync(It.IsAny<int>(), It.IsAny<int>(), productId, It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lotsResult);

        // Act
        await _viewModel.SetSelectedProductAsync(product);

        // Assert
        Assert.Equal(product, _viewModel.SelectedProduct);
        Assert.Single(_viewModel.Products);
        Assert.Equal(productId, _viewModel.Entity!.ProductId);
        Assert.Equal("Product X", _viewModel.Entity.ProductName);
        Assert.NotNull(_viewModel.Lots);
        Assert.Single(_viewModel.Lots!);
    }

    [Fact]
    public async Task SetSelectedProductAsync_Null_ClearsProductAndLots()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var serialId = Guid.NewGuid();
        var existingSerial = new SerialDto
        {
            Id = serialId,
            TenantId = Guid.NewGuid(),
            SerialNumber = "SN-001",
            ProductId = productId,
            ProductName = "Product A",
            ProductCode = "PROD-A",
            Status = "Available",
            IsActive = true
        };
        var productA = new ProductDto { Id = productId, Name = "Product A", Code = "PROD-A" };
        var lotsResult = new PagedResult<LotDto> { Items = new List<LotDto>(), TotalCount = 0, Page = 1, PageSize = 200 };

        _mockSerialService.Setup(s => s.GetSerialByIdAsync(serialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSerial);
        _mockProductService.Setup(s => s.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(productA);
        _mockLotService.Setup(s => s.GetLotsAsync(It.IsAny<int>(), It.IsAny<int>(), productId, It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lotsResult);

        await _viewModel.LoadEntityAsync(serialId);

        // Act
        await _viewModel.SetSelectedProductAsync(null);

        // Assert
        Assert.Null(_viewModel.SelectedProduct);
        Assert.Empty(_viewModel.Products);
        Assert.Equal(Guid.Empty, _viewModel.Entity!.ProductId);
        Assert.Null(_viewModel.Entity.LotId);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }
}
