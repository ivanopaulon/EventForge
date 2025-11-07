using EventForge.Client.Services;
using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services.Warehouse;

public class SfInventoryStateManagerTests
{
    private readonly Mock<ILogger<SfInventoryStateManager>> _loggerMock;
    private readonly SfInventoryStateManager _stateManager;

    public SfInventoryStateManagerTests()
    {
        _loggerMock = new Mock<ILogger<SfInventoryStateManager>>();
        _stateManager = new SfInventoryStateManager(_loggerMock.Object);
    }

    #region ResetInputState Tests

    [Fact]
    public void ResetInputState_ShouldReturnClearedState()
    {
        // Act
        var result = _stateManager.ResetInputState();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.ScannedBarcode);
        Assert.Null(result.CurrentProduct);
        Assert.Null(result.SelectedLocationId);
        Assert.Null(result.SelectedLocation);
        Assert.Equal(1, result.Quantity);
        Assert.Equal(string.Empty, result.Notes);
        Assert.Null(result.LastAddedRow);
    }

    [Fact]
    public void ResetInputState_ShouldReturnNewInstance()
    {
        // Act
        var result1 = _stateManager.ResetInputState();
        var result2 = _stateManager.ResetInputState();

        // Assert
        Assert.NotSame(result1, result2);
    }

    #endregion

    #region ResetUIState Tests

    [Fact]
    public void ResetUIState_ShouldReturnClearedState()
    {
        // Act
        var result = _stateManager.ResetUIState();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsLoading);
        Assert.False(result.ShowAssignPanel);
        Assert.Equal(string.Empty, result.AssignPanelBarcode);
        Assert.Null(result.AssignSelectedProduct);
        Assert.Equal("Barcode", result.AssignCodeType);
        Assert.Null(result.EditingRowId);
        Assert.False(result.ShowFinalizeConfirmation);
        Assert.False(result.ShowCancelConfirmation);
    }

    [Fact]
    public void ResetUIState_ShouldReturnNewInstance()
    {
        // Act
        var result1 = _stateManager.ResetUIState();
        var result2 = _stateManager.ResetUIState();

        // Assert
        Assert.NotSame(result1, result2);
    }

    #endregion

    #region CloneInputState Tests

    [Fact]
    public void CloneInputState_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new InventoryInputState
        {
            ScannedBarcode = "12345",
            CurrentProduct = new ProductDto { Id = Guid.NewGuid(), Name = "Test Product" },
            SelectedLocationId = Guid.NewGuid(),
            Quantity = 5,
            Notes = "Test notes"
        };

        // Act
        var cloned = _stateManager.CloneInputState(original);

        // Assert
        Assert.NotSame(original, cloned);
        Assert.Equal(original.ScannedBarcode, cloned.ScannedBarcode);
        Assert.Same(original.CurrentProduct, cloned.CurrentProduct); // Reference type, shallow copy expected
        Assert.Equal(original.SelectedLocationId, cloned.SelectedLocationId);
        Assert.Equal(original.Quantity, cloned.Quantity);
        Assert.Equal(original.Notes, cloned.Notes);
    }

    [Fact]
    public void CloneInputState_WithNullProduct_ShouldWork()
    {
        // Arrange
        var original = new InventoryInputState
        {
            ScannedBarcode = "12345",
            CurrentProduct = null,
            Quantity = 3
        };

        // Act
        var cloned = _stateManager.CloneInputState(original);

        // Assert
        Assert.NotSame(original, cloned);
        Assert.Null(cloned.CurrentProduct);
        Assert.Equal(original.Quantity, cloned.Quantity);
    }

    #endregion

    #region CloneUIState Tests

    [Fact]
    public void CloneUIState_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new InventoryUIState
        {
            IsLoading = true,
            ShowAssignPanel = true,
            AssignPanelBarcode = "ABC123",
            EditingRowId = Guid.NewGuid(),
            EditQuantity = 10,
            ShowFinalizeConfirmation = true
        };

        // Act
        var cloned = _stateManager.CloneUIState(original);

        // Assert
        Assert.NotSame(original, cloned);
        Assert.Equal(original.IsLoading, cloned.IsLoading);
        Assert.Equal(original.ShowAssignPanel, cloned.ShowAssignPanel);
        Assert.Equal(original.AssignPanelBarcode, cloned.AssignPanelBarcode);
        Assert.Equal(original.EditingRowId, cloned.EditingRowId);
        Assert.Equal(original.EditQuantity, cloned.EditQuantity);
        Assert.Equal(original.ShowFinalizeConfirmation, cloned.ShowFinalizeConfirmation);
    }

    #endregion

    #region ValidateSessionState Tests

    [Fact]
    public void ValidateSessionState_WithNullState_ShouldReturnFalse()
    {
        // Act
        var result = _stateManager.ValidateSessionState(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSessionState_WithValidState_ShouldReturnTrue()
    {
        // Arrange
        var state = new InventorySessionState
        {
            DocumentId = Guid.NewGuid(),
            DocumentNumber = "INV-001",
            WarehouseId = Guid.NewGuid(),
            WarehouseName = "Main Warehouse"
        };

        // Act
        var result = _stateManager.ValidateSessionState(state);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSessionState_WithoutDocumentId_ShouldReturnFalse()
    {
        // Arrange
        var state = new InventorySessionState
        {
            DocumentId = Guid.Empty,
            DocumentNumber = "INV-001",
            WarehouseId = Guid.NewGuid()
        };

        // Act
        var result = _stateManager.ValidateSessionState(state);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSessionState_WithoutWarehouseId_ShouldReturnFalse()
    {
        // Arrange
        var state = new InventorySessionState
        {
            DocumentId = Guid.NewGuid(),
            DocumentNumber = "INV-001",
            WarehouseId = null
        };

        // Act
        var result = _stateManager.ValidateSessionState(state);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSessionState_WithoutDocumentNumber_ShouldReturnFalse()
    {
        // Arrange
        var state = new InventorySessionState
        {
            DocumentId = Guid.NewGuid(),
            DocumentNumber = null,
            WarehouseId = Guid.NewGuid()
        };

        // Act
        var result = _stateManager.ValidateSessionState(state);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ValidateInputState Tests

    [Fact]
    public void ValidateInputState_WithCompleteState_ShouldReturnTrue()
    {
        // Arrange
        var state = new InventoryInputState
        {
            CurrentProduct = new ProductDto { Id = Guid.NewGuid(), Name = "Test" },
            SelectedLocationId = Guid.NewGuid(),
            Quantity = 5
        };

        // Act
        var result = _stateManager.ValidateInputState(state);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateInputState_WithoutProduct_ShouldReturnFalse()
    {
        // Arrange
        var state = new InventoryInputState
        {
            CurrentProduct = null,
            SelectedLocationId = Guid.NewGuid(),
            Quantity = 5
        };

        // Act
        var result = _stateManager.ValidateInputState(state);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateInputState_WithoutLocation_ShouldReturnFalse()
    {
        // Arrange
        var state = new InventoryInputState
        {
            CurrentProduct = new ProductDto { Id = Guid.NewGuid(), Name = "Test" },
            SelectedLocationId = null,
            Quantity = 5
        };

        // Act
        var result = _stateManager.ValidateInputState(state);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateInputState_WithZeroQuantity_ShouldReturnFalse()
    {
        // Arrange
        var state = new InventoryInputState
        {
            CurrentProduct = new ProductDto { Id = Guid.NewGuid(), Name = "Test" },
            SelectedLocationId = Guid.NewGuid(),
            Quantity = 0
        };

        // Act
        var result = _stateManager.ValidateInputState(state);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateInputState_WithNegativeQuantity_ShouldReturnFalse()
    {
        // Arrange
        var state = new InventoryInputState
        {
            CurrentProduct = new ProductDto { Id = Guid.NewGuid(), Name = "Test" },
            SelectedLocationId = Guid.NewGuid(),
            Quantity = -1
        };

        // Act
        var result = _stateManager.ValidateInputState(state);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsSessionExpired Tests

    [Fact]
    public void IsSessionExpired_WithRecentActivity_ShouldReturnFalse()
    {
        // Arrange
        var state = new InventorySessionState
        {
            LastActivityTime = DateTime.UtcNow.AddMinutes(-5)
        };
        var maxInactivity = TimeSpan.FromMinutes(30);

        // Act
        var result = _stateManager.IsSessionExpired(state, maxInactivity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSessionExpired_WithOldActivity_ShouldReturnTrue()
    {
        // Arrange
        var state = new InventorySessionState
        {
            LastActivityTime = DateTime.UtcNow.AddHours(-2)
        };
        var maxInactivity = TimeSpan.FromMinutes(30);

        // Act
        var result = _stateManager.IsSessionExpired(state, maxInactivity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSessionExpired_JustUnderLimit_ShouldReturnFalse()
    {
        // Arrange
        var maxInactivity = TimeSpan.FromMinutes(30);
        var state = new InventorySessionState
        {
            LastActivityTime = DateTime.UtcNow.AddMinutes(-29)
        };

        // Act
        var result = _stateManager.IsSessionExpired(state, maxInactivity);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CreateLogEntry Tests

    [Fact]
    public void CreateLogEntry_WithInfoType_ShouldCreateInfoEntry()
    {
        // Act
        var result = _stateManager.CreateLogEntry("info", "Test message", "Test details");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test message", result.Message);
        Assert.Equal("Test details", result.Details);
        Assert.Equal("Info", result.Type);
    }

    [Fact]
    public void CreateLogEntry_WithSuccessType_ShouldCreateSuccessEntry()
    {
        // Act
        var result = _stateManager.CreateLogEntry("success", "Success message");

        // Assert
        Assert.Equal("Success message", result.Message);
        Assert.Equal("Success", result.Type);
    }

    [Fact]
    public void CreateLogEntry_WithWarningType_ShouldCreateWarningEntry()
    {
        // Act
        var result = _stateManager.CreateLogEntry("warning", "Warning message");

        // Assert
        Assert.Equal("Warning message", result.Message);
        Assert.Equal("Warning", result.Type);
    }

    [Fact]
    public void CreateLogEntry_WithErrorType_ShouldCreateErrorEntry()
    {
        // Act
        var result = _stateManager.CreateLogEntry("error", "Error message");

        // Assert
        Assert.Equal("Error message", result.Message);
        Assert.Equal("Error", result.Type);
    }

    [Fact]
    public void CreateLogEntry_WithUnknownType_ShouldCreateInfoEntry()
    {
        // Act
        var result = _stateManager.CreateLogEntry("unknown", "Unknown message");

        // Assert
        Assert.Equal("Unknown message", result.Message);
        Assert.Equal("Info", result.Type);
    }

    [Fact]
    public void CreateLogEntry_ShouldSetTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = _stateManager.CreateLogEntry("info", "Test");
        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.InRange(result.Timestamp, beforeCall, afterCall);
    }

    #endregion
}
