using EventForge.Client.Services;
using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services.Warehouse;

public class InventoryFastServiceTests
{
    private readonly Mock<ILogger<InventoryFastService>> _loggerMock;
    private readonly InventoryFastService _service;

    public InventoryFastServiceTests()
    {
        _loggerMock = new Mock<ILogger<InventoryFastService>>();
        _service = new InventoryFastService(_loggerMock.Object);
    }

    #region HandleBarcodeScanned Tests

    [Fact]
    public void HandleBarcodeScanned_WithoutCurrentProduct_ReturnsLookupProduct()
    {
        // Arrange
        var scannedCode = "12345";
        ProductDto? currentProduct = null;
        Guid? selectedLocationId = null;
        decimal? currentQuantity = 1;
        bool fastConfirmEnabled = false;

        // Act
        var result = _service.HandleBarcodeScanned(
            scannedCode, 
            currentProduct, 
            selectedLocationId, 
            currentQuantity, 
            fastConfirmEnabled);

        // Assert
        Assert.Equal(BarcodeScanAction.LookupProduct, result.Action);
        Assert.Equal(1, result.NewQuantity);
    }

    [Fact]
    public void HandleBarcodeScanned_RepeatedScan_WithFastConfirm_ReturnsIncrementAndConfirm()
    {
        // Arrange
        var scannedCode = "12345";
        var currentProduct = new ProductDto { Id = Guid.NewGuid(), Name = "Test Product" };
        var selectedLocationId = Guid.NewGuid();
        decimal? currentQuantity = 5;
        bool fastConfirmEnabled = true;

        // Act
        var result = _service.HandleBarcodeScanned(
            scannedCode, 
            currentProduct, 
            selectedLocationId, 
            currentQuantity, 
            fastConfirmEnabled);

        // Assert
        Assert.Equal(BarcodeScanAction.IncrementAndConfirm, result.Action);
        Assert.Equal(6, result.NewQuantity);
        Assert.Contains("Repeated scan", result.LogMessage);
    }

    [Fact]
    public void HandleBarcodeScanned_RepeatedScan_WithoutFastConfirm_ReturnsIncrementAndFocusQuantity()
    {
        // Arrange
        var scannedCode = "12345";
        var currentProduct = new ProductDto { Id = Guid.NewGuid(), Name = "Test Product" };
        var selectedLocationId = Guid.NewGuid();
        decimal? currentQuantity = 3;
        bool fastConfirmEnabled = false;

        // Act
        var result = _service.HandleBarcodeScanned(
            scannedCode, 
            currentProduct, 
            selectedLocationId, 
            currentQuantity, 
            fastConfirmEnabled);

        // Assert
        Assert.Equal(BarcodeScanAction.IncrementAndFocusQuantity, result.Action);
        Assert.Equal(4, result.NewQuantity);
        Assert.Contains("Repeated scan", result.LogMessage);
    }

    [Fact]
    public void HandleBarcodeScanned_WithoutLocation_ReturnsLookupProduct()
    {
        // Arrange
        var scannedCode = "12345";
        var currentProduct = new ProductDto { Id = Guid.NewGuid(), Name = "Test Product" };
        Guid? selectedLocationId = null;
        decimal? currentQuantity = 5;
        bool fastConfirmEnabled = true;

        // Act
        var result = _service.HandleBarcodeScanned(
            scannedCode, 
            currentProduct, 
            selectedLocationId, 
            currentQuantity, 
            fastConfirmEnabled);

        // Assert
        Assert.Equal(BarcodeScanAction.LookupProduct, result.Action);
    }

    #endregion

    #region DetermineRowOperation Tests

    [Fact]
    public void DetermineRowOperation_NoExistingRow_ReturnsCreate()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var quantity = 10m;
        var notes = "Test notes";
        var documentRows = new List<InventoryDocumentRowDto>();

        // Act
        var result = _service.DetermineRowOperation(
            documentRows, 
            productId, 
            locationId, 
            quantity, 
            notes);

        // Assert
        Assert.Equal(RowOperationType.Create, result.OperationType);
        Assert.Null(result.ExistingRowId);
        Assert.Equal(10m, result.NewQuantity);
        Assert.Equal("Test notes", result.CombinedNotes);
    }

    [Fact]
    public void DetermineRowOperation_ExistingRow_ReturnsUpdate()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var existingRowId = Guid.NewGuid();
        var quantity = 5m;
        var notes = "New notes";

        var documentRows = new List<InventoryDocumentRowDto>
        {
            new InventoryDocumentRowDto 
            { 
                Id = existingRowId,
                ProductId = productId, 
                LocationId = locationId, 
                Quantity = 10m,
                Notes = "Existing notes"
            }
        };

        // Act
        var result = _service.DetermineRowOperation(
            documentRows, 
            productId, 
            locationId, 
            quantity, 
            notes);

        // Assert
        Assert.Equal(RowOperationType.Update, result.OperationType);
        Assert.Equal(existingRowId, result.ExistingRowId);
        Assert.Equal(15m, result.NewQuantity); // 10 + 5
        Assert.Equal("Existing notes; New notes", result.CombinedNotes);
    }

    [Fact]
    public void DetermineRowOperation_ExistingRowDifferentLocation_ReturnsCreate()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var locationId1 = Guid.NewGuid();
        var locationId2 = Guid.NewGuid();
        var quantity = 5m;
        var notes = "New notes";

        var documentRows = new List<InventoryDocumentRowDto>
        {
            new InventoryDocumentRowDto 
            { 
                Id = Guid.NewGuid(),
                ProductId = productId, 
                LocationId = locationId1, 
                Quantity = 10m,
                Notes = "Existing notes"
            }
        };

        // Act
        var result = _service.DetermineRowOperation(
            documentRows, 
            productId, 
            locationId2, // Different location
            quantity, 
            notes);

        // Assert
        Assert.Equal(RowOperationType.Create, result.OperationType);
        Assert.Null(result.ExistingRowId);
        Assert.Equal(5m, result.NewQuantity);
    }

    #endregion

    #region SearchProducts Tests

    [Fact]
    public void SearchProducts_EmptySearchTerm_ReturnsTopResults()
    {
        // Arrange
        var allProducts = CreateTestProducts(30);
        var searchTerm = "";

        // Act
        var results = _service.SearchProducts(searchTerm, allProducts, maxResults: 20);

        // Assert
        Assert.Equal(20, results.Count());
    }

    [Fact]
    public void SearchProducts_MatchesName_ReturnsProduct()
    {
        // Arrange
        var allProducts = CreateTestProducts(10);
        var searchTerm = "Product 5";

        // Act
        var results = _service.SearchProducts(searchTerm, allProducts);

        // Assert
        Assert.Single(results);
        Assert.Contains("Product 5", results.First().Name);
    }

    [Fact]
    public void SearchProducts_MatchesCode_ReturnsProduct()
    {
        // Arrange
        var allProducts = CreateTestProducts(10);
        var searchTerm = "CODE3";

        // Act
        var results = _service.SearchProducts(searchTerm, allProducts);

        // Assert
        Assert.Single(results);
        Assert.Contains("CODE3", results.First().Code);
    }

    [Fact]
    public void SearchProducts_MatchesDescription_ReturnsProduct()
    {
        // Arrange
        var allProducts = new List<ProductDto>
        {
            new ProductDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Product A", 
                Code = "CODEA",
                Description = "This product contains special features"
            },
            new ProductDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Product B", 
                Code = "CODEB",
                Description = "Standard product"
            }
        };
        var searchTerm = "special";

        // Act
        var results = _service.SearchProducts(searchTerm, allProducts);

        // Assert
        Assert.Single(results);
        Assert.Contains("special", results.First().Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SearchProducts_MatchesShortDescription_ReturnsProduct()
    {
        // Arrange
        var allProducts = new List<ProductDto>
        {
            new ProductDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Product A", 
                Code = "CODEA",
                ShortDescription = "Premium quality"
            },
            new ProductDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Product B", 
                Code = "CODEB",
                ShortDescription = "Standard quality"
            }
        };
        var searchTerm = "premium";

        // Act
        var results = _service.SearchProducts(searchTerm, allProducts);

        // Assert
        Assert.Single(results);
        Assert.Contains("Premium", results.First().ShortDescription);
    }

    [Fact]
    public void SearchProducts_CaseInsensitive_ReturnsProduct()
    {
        // Arrange
        var allProducts = CreateTestProducts(5);
        var searchTerm = "product 3"; // lowercase

        // Act
        var results = _service.SearchProducts(searchTerm, allProducts);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public void SearchProducts_MaxResults_LimitsResults()
    {
        // Arrange
        var allProducts = CreateTestProducts(50);
        var searchTerm = "Product"; // Matches all

        // Act
        var results = _service.SearchProducts(searchTerm, allProducts, maxResults: 10);

        // Assert
        Assert.Equal(10, results.Count());
    }

    #endregion

    #region ClearProductFormState Tests

    [Fact]
    public void ClearProductFormState_ReturnsDefaultState()
    {
        // Act
        var result = _service.ClearProductFormState();

        // Assert
        Assert.Equal(string.Empty, result.ScannedBarcode);
        Assert.Null(result.CurrentProduct);
        Assert.Null(result.SelectedLocationId);
        Assert.Null(result.SelectedLocation);
        Assert.Equal(1, result.Quantity);
        Assert.Equal(string.Empty, result.Notes);
    }

    #endregion

    #region CombineNotes Tests

    [Fact]
    public void CombineNotes_BothEmpty_ReturnsNull()
    {
        // Act
        var result = _service.CombineNotes(null, null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CombineNotes_ExistingOnly_ReturnsExisting()
    {
        // Act
        var result = _service.CombineNotes("Existing notes", null);

        // Assert
        Assert.Equal("Existing notes", result);
    }

    [Fact]
    public void CombineNotes_NewOnly_ReturnsNew()
    {
        // Act
        var result = _service.CombineNotes(null, "New notes");

        // Assert
        Assert.Equal("New notes", result);
    }

    [Fact]
    public void CombineNotes_Both_ReturnsCombined()
    {
        // Act
        var result = _service.CombineNotes("Existing notes", "New notes");

        // Assert
        Assert.Equal("Existing notes; New notes", result);
    }

    [Fact]
    public void CombineNotes_EmptyStrings_ReturnsEmptyString()
    {
        // Act
        var result = _service.CombineNotes(string.Empty, string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Helper Methods

    private List<ProductDto> CreateTestProducts(int count)
    {
        var products = new List<ProductDto>();
        for (int i = 0; i < count; i++)
        {
            products.Add(new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i}",
                Code = $"CODE{i}",
                ShortDescription = $"Short desc {i}",
                Description = $"Full description for product {i}"
            });
        }
        return products;
    }

    #endregion
}
