using Microsoft.Extensions.Logging;

namespace EventForge.Client.Services;

/// <summary>
/// Represents the current input/form state for product entry.
/// This is ephemeral state that doesn't need to be persisted.
/// </summary>
public class InventoryInputState
{
    /// <summary>
    /// Currently scanned or entered barcode
    /// </summary>
    public string ScannedBarcode { get; set; } = string.Empty;

    /// <summary>
    /// Currently selected product (if any)
    /// </summary>
    public EventForge.DTOs.Products.ProductDto? CurrentProduct { get; set; }

    /// <summary>
    /// Selected storage location ID
    /// </summary>
    public Guid? SelectedLocationId { get; set; }

    /// <summary>
    /// Selected storage location object
    /// </summary>
    public EventForge.DTOs.Warehouse.StorageLocationDto? SelectedLocation { get; set; }

    /// <summary>
    /// Quantity to add
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Notes/remarks for the inventory entry
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// The last row that was added (for undo functionality)
    /// </summary>
    public EventForge.DTOs.Warehouse.InventoryDocumentRowDto? LastAddedRow { get; set; }

    /// <summary>
    /// Creates a cleared state (reset form)
    /// </summary>
    public static InventoryInputState CreateCleared()
    {
        return new InventoryInputState
        {
            ScannedBarcode = string.Empty,
            CurrentProduct = null,
            SelectedLocationId = null,
            SelectedLocation = null,
            Quantity = 1,
            Notes = string.Empty,
            LastAddedRow = null
        };
    }

    /// <summary>
    /// Clears the form state
    /// </summary>
    public void Clear()
    {
        ScannedBarcode = string.Empty;
        CurrentProduct = null;
        SelectedLocationId = null;
        SelectedLocation = null;
        Quantity = 1;
        Notes = string.Empty;
        // LastAddedRow is NOT cleared to support undo
    }

    /// <summary>
    /// Checks if the form is ready for confirmation
    /// </summary>
    public bool IsReadyForConfirm()
    {
        return CurrentProduct != null && 
               SelectedLocationId.HasValue && 
               Quantity > 0;
    }

    /// <summary>
    /// Creates a clone of this state
    /// </summary>
    public InventoryInputState Clone()
    {
        return new InventoryInputState
        {
            ScannedBarcode = ScannedBarcode,
            CurrentProduct = CurrentProduct,
            SelectedLocationId = SelectedLocationId,
            SelectedLocation = SelectedLocation,
            Quantity = Quantity,
            Notes = Notes,
            LastAddedRow = LastAddedRow
        };
    }
}

/// <summary>
/// Represents the UI state for the inventory procedure (dialogs, panels, editing state).
/// This is ephemeral UI state that doesn't need to be persisted.
/// </summary>
public class InventoryUIState
{
    /// <summary>
    /// Whether the application is currently loading data
    /// </summary>
    public bool IsLoading { get; set; } = false;

    /// <summary>
    /// Whether the product assignment panel should be shown
    /// </summary>
    public bool ShowAssignPanel { get; set; } = false;

    /// <summary>
    /// Barcode that triggered the assignment panel
    /// </summary>
    public string AssignPanelBarcode { get; set; } = string.Empty;

    /// <summary>
    /// Selected product for assignment
    /// </summary>
    public EventForge.DTOs.Products.ProductDto? AssignSelectedProduct { get; set; }

    /// <summary>
    /// Code type for assignment (Barcode, QRCode, etc.)
    /// </summary>
    public string AssignCodeType { get; set; } = "Barcode";

    /// <summary>
    /// Code value for assignment
    /// </summary>
    public string AssignCode { get; set; } = string.Empty;

    /// <summary>
    /// Alternative description for the code
    /// </summary>
    public string? AssignAlternativeDescription { get; set; }

    /// <summary>
    /// ID of the row currently being edited (inline edit)
    /// </summary>
    public Guid? EditingRowId { get; set; }

    /// <summary>
    /// Quantity value during edit
    /// </summary>
    public decimal? EditQuantity { get; set; }

    /// <summary>
    /// Notes value during edit
    /// </summary>
    public string? EditNotes { get; set; }

    /// <summary>
    /// ID of the row pending delete confirmation
    /// </summary>
    public Guid? ConfirmDeleteRowId { get; set; }

    /// <summary>
    /// Whether the finalize confirmation is shown
    /// </summary>
    public bool ShowFinalizeConfirmation { get; set; } = false;

    /// <summary>
    /// Whether the cancel confirmation is shown
    /// </summary>
    public bool ShowCancelConfirmation { get; set; } = false;

    /// <summary>
    /// Creates a cleared UI state
    /// </summary>
    public static InventoryUIState CreateCleared()
    {
        return new InventoryUIState();
    }

    /// <summary>
    /// Clears all assignment panel state
    /// </summary>
    public void ClearAssignPanel()
    {
        ShowAssignPanel = false;
        AssignPanelBarcode = string.Empty;
        AssignSelectedProduct = null;
        AssignCodeType = "Barcode";
        AssignCode = string.Empty;
        AssignAlternativeDescription = null;
    }

    /// <summary>
    /// Clears all edit state
    /// </summary>
    public void ClearEditState()
    {
        EditingRowId = null;
        EditQuantity = null;
        EditNotes = null;
    }

    /// <summary>
    /// Clears all confirmation dialogs
    /// </summary>
    public void ClearConfirmations()
    {
        ConfirmDeleteRowId = null;
        ShowFinalizeConfirmation = false;
        ShowCancelConfirmation = false;
    }

    /// <summary>
    /// Opens the assignment panel with the given barcode
    /// </summary>
    public void OpenAssignPanel(string barcode)
    {
        ShowAssignPanel = true;
        AssignPanelBarcode = barcode;
        AssignCode = barcode;
        AssignSelectedProduct = null;
        AssignCodeType = "Barcode";
        AssignAlternativeDescription = null;
    }

    /// <summary>
    /// Begins editing a row
    /// </summary>
    public void BeginEdit(Guid rowId, decimal quantity, string? notes)
    {
        EditingRowId = rowId;
        EditQuantity = quantity;
        EditNotes = notes;
    }

    /// <summary>
    /// Creates a clone of this state
    /// </summary>
    public InventoryUIState Clone()
    {
        return new InventoryUIState
        {
            IsLoading = IsLoading,
            ShowAssignPanel = ShowAssignPanel,
            AssignPanelBarcode = AssignPanelBarcode,
            AssignSelectedProduct = AssignSelectedProduct,
            AssignCodeType = AssignCodeType,
            AssignCode = AssignCode,
            AssignAlternativeDescription = AssignAlternativeDescription,
            EditingRowId = EditingRowId,
            EditQuantity = EditQuantity,
            EditNotes = EditNotes,
            ConfirmDeleteRowId = ConfirmDeleteRowId,
            ShowFinalizeConfirmation = ShowFinalizeConfirmation,
            ShowCancelConfirmation = ShowCancelConfirmation
        };
    }
}

/// <summary>
/// Represents a single entry in the operation log for the inventory procedure.
/// Used for user-visible audit trail and troubleshooting.
/// </summary>
public class OperationLogEntry
{
    /// <summary>
    /// Timestamp of the operation
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Short message describing the operation
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional detailed information
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Type of operation (Info, Success, Warning, Error)
    /// </summary>
    public string Type { get; set; } = "Info";

    /// <summary>
    /// Creates an info log entry
    /// </summary>
    public static OperationLogEntry Info(string message, string details = "")
    {
        return new OperationLogEntry
        {
            Message = message,
            Details = details,
            Type = "Info"
        };
    }

    /// <summary>
    /// Creates a success log entry
    /// </summary>
    public static OperationLogEntry Success(string message, string details = "")
    {
        return new OperationLogEntry
        {
            Message = message,
            Details = details,
            Type = "Success"
        };
    }

    /// <summary>
    /// Creates a warning log entry
    /// </summary>
    public static OperationLogEntry Warning(string message, string details = "")
    {
        return new OperationLogEntry
        {
            Message = message,
            Details = details,
            Type = "Warning"
        };
    }

    /// <summary>
    /// Creates an error log entry
    /// </summary>
    public static OperationLogEntry Error(string message, string details = "")
    {
        return new OperationLogEntry
        {
            Message = message,
            Details = details,
            Type = "Error"
        };
    }

    /// <summary>
    /// Gets a CSS class for the entry type
    /// </summary>
    public string GetCssClass()
    {
        return Type.ToLower() switch
        {
            "success" => "text-success",
            "warning" => "text-warning",
            "error" => "text-danger",
            _ => "text-muted"
        };
    }

    /// <summary>
    /// Gets an icon class for the entry type
    /// </summary>
    public string GetIconClass()
    {
        return Type.ToLower() switch
        {
            "success" => "e-icons e-check-mark",
            "warning" => "e-icons e-warning",
            "error" => "e-icons e-circle-close",
            _ => "e-icons e-circle-info"
        };
    }
}

/// <summary>
/// Service interface for managing inventory procedure state.
/// Provides centralized state operations for session, input, and UI state.
/// </summary>
public interface ISfInventoryStateManager
{
    /// <summary>
    /// Resets the input state to default values
    /// </summary>
    InventoryInputState ResetInputState();

    /// <summary>
    /// Resets the UI state to default values
    /// </summary>
    InventoryUIState ResetUIState();

    /// <summary>
    /// Creates a clone of the input state
    /// </summary>
    InventoryInputState CloneInputState(InventoryInputState source);

    /// <summary>
    /// Creates a clone of the UI state
    /// </summary>
    InventoryUIState CloneUIState(InventoryUIState source);

    /// <summary>
    /// Validates that the session state is complete and valid
    /// </summary>
    bool ValidateSessionState(InventorySessionState? state);

    /// <summary>
    /// Validates that the input state is ready for confirmation
    /// </summary>
    bool ValidateInputState(InventoryInputState state);

    /// <summary>
    /// Checks if a session has expired based on inactivity
    /// </summary>
    bool IsSessionExpired(InventorySessionState state, TimeSpan maxInactivity);

    /// <summary>
    /// Creates operation log entries with consistent formatting
    /// </summary>
    OperationLogEntry CreateLogEntry(string type, string message, string details = "");
}

/// <summary>
/// Implementation of inventory procedure state management.
/// Provides testable, centralized state operations.
/// </summary>
public class SfInventoryStateManager : ISfInventoryStateManager
{
    private readonly ILogger<SfInventoryStateManager> _logger;

    public SfInventoryStateManager(ILogger<SfInventoryStateManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public InventoryInputState ResetInputState()
    {
        _logger.LogDebug("Resetting input state");
        return InventoryInputState.CreateCleared();
    }

    /// <inheritdoc/>
    public InventoryUIState ResetUIState()
    {
        _logger.LogDebug("Resetting UI state");
        return InventoryUIState.CreateCleared();
    }

    /// <inheritdoc/>
    public InventoryInputState CloneInputState(InventoryInputState source)
    {
        _logger.LogDebug("Cloning input state");
        return source.Clone();
    }

    /// <inheritdoc/>
    public InventoryUIState CloneUIState(InventoryUIState source)
    {
        _logger.LogDebug("Cloning UI state");
        return source.Clone();
    }

    /// <inheritdoc/>
    public bool ValidateSessionState(InventorySessionState? state)
    {
        if (state == null)
        {
            _logger.LogWarning("Session state is null");
            return false;
        }

        var isValid = state.IsValid();
        
        if (!isValid)
        {
            _logger.LogWarning("Session state validation failed: DocumentId={DocumentId}, WarehouseId={WarehouseId}, DocumentNumber={DocumentNumber}",
                state.DocumentId, state.WarehouseId, state.DocumentNumber);
        }
        else
        {
            _logger.LogDebug("Session state is valid");
        }

        return isValid;
    }

    /// <inheritdoc/>
    public bool ValidateInputState(InventoryInputState state)
    {
        var isReady = state.IsReadyForConfirm();

        if (!isReady)
        {
            _logger.LogDebug("Input state not ready for confirm: HasProduct={HasProduct}, HasLocation={HasLocation}, Quantity={Quantity}",
                state.CurrentProduct != null, state.SelectedLocationId.HasValue, state.Quantity);
        }
        else
        {
            _logger.LogDebug("Input state is ready for confirm");
        }

        return isReady;
    }

    /// <inheritdoc/>
    public bool IsSessionExpired(InventorySessionState state, TimeSpan maxInactivity)
    {
        var isExpired = state.IsExpired(maxInactivity);

        if (isExpired)
        {
            _logger.LogWarning("Session has expired. LastActivity={LastActivity}, MaxInactivity={MaxInactivity}",
                state.LastActivityTime, maxInactivity);
        }

        return isExpired;
    }

    /// <inheritdoc/>
    public OperationLogEntry CreateLogEntry(string type, string message, string details = "")
    {
        _logger.LogDebug("Creating log entry: Type={Type}, Message={Message}", type, message);

        return type.ToLower() switch
        {
            "success" => OperationLogEntry.Success(message, details),
            "warning" => OperationLogEntry.Warning(message, details),
            "error" => OperationLogEntry.Error(message, details),
            _ => OperationLogEntry.Info(message, details)
        };
    }
}
