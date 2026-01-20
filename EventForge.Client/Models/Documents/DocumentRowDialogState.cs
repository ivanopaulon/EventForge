using EventForge.DTOs.Documents;
using EventForge.DTOs.Products;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;

namespace EventForge.Client.Models.Documents;

/// <summary>
/// Unified state model for AddDocumentRowDialog
/// Consolidates ~40 scattered state variables into a structured object
/// </summary>
public class DocumentRowDialogState
{
    /// <summary>
    /// The document row being created or edited
    /// </summary>
    public CreateDocumentRowDto Model { get; set; } = new() { Quantity = 1m };

    /// <summary>
    /// The document header this row belongs to
    /// </summary>
    public DocumentHeaderDto? DocumentHeader { get; set; }

    /// <summary>
    /// Currently selected product in autocomplete
    /// </summary>
    public ProductDto? SelectedProduct { get; set; }

    /// <summary>
    /// Previously selected product (for change detection)
    /// </summary>
    public ProductDto? PreviousSelectedProduct { get; set; }

    /// <summary>
    /// Current dialog mode (Standard, QuickAdd, ContinuousScan)
    /// </summary>
    public DialogMode Mode { get; set; } = DialogMode.Standard;

    /// <summary>
    /// Processing state flags
    /// </summary>
    public ProcessingState Processing { get; set; } = new();

    /// <summary>
    /// Validation state
    /// </summary>
    public ValidationState Validation { get; set; } = new();

    /// <summary>
    /// Cached data (units, VAT rates, etc.)
    /// </summary>
    public CacheState Cache { get; set; } = new();

    /// <summary>
    /// UI state (panel expansion, barcode input, etc.)
    /// </summary>
    public UiState Ui { get; set; } = new();

    /// <summary>
    /// Barcode scanning state
    /// </summary>
    public BarcodeState Barcode { get; set; } = new();

    /// <summary>
    /// Continuous Scan mode tracking
    /// </summary>
    public ContinuousScanState ContinuousScan { get; set; } = new();

    /// <summary>
    /// Selected unit of measure ID
    /// </summary>
    public Guid? SelectedUnitOfMeasureId { get; set; }

    /// <summary>
    /// Selected VAT rate ID
    /// </summary>
    public Guid? SelectedVatRateId { get; set; }
}

/// <summary>
/// Tracks async operation states
/// </summary>
public class ProcessingState
{
    public bool IsLoadingData { get; set; }
    public bool IsSaving { get; set; }
    public bool IsProcessingBarcode { get; set; }
    public bool IsLoadingTransactions { get; set; }
}

/// <summary>
/// Tracks validation errors
/// </summary>
public class ValidationState
{
    public List<string> Errors { get; set; } = new();
    public bool IsValid => !Errors.Any();
}

/// <summary>
/// Cached reference data
/// </summary>
public class CacheState
{
    public List<ProductUnitDto> AvailableUnits { get; set; } = new();
    public List<UMDto> AllUnitsOfMeasure { get; set; } = new();
    public List<VatRateDto> AllVatRates { get; set; } = new();
    public List<RecentProductTransactionDto> RecentTransactions { get; set; } = new();
}

/// <summary>
/// UI interaction state
/// </summary>
public class UiState
{
    public bool VatPanelExpanded { get; set; }
    public bool DiscountsPanelExpanded { get; set; }
    public bool NotesPanelExpanded { get; set; }
}

/// <summary>
/// Barcode scanning state
/// </summary>
public class BarcodeState
{
    public string Input { get; set; } = string.Empty;
    public string ScannedBarcode { get; set; } = string.Empty;
    public Guid? ProductUnitId { get; set; }
}

/// <summary>
/// Continuous Scan mode state
/// </summary>
public class ContinuousScanState
{
    public List<ContinuousScanEntry> RecentScans { get; set; } = new();
    public HashSet<Guid> ScannedProductIds { get; set; } = new();
    public int ScanCount { get; set; }
    public int UniqueProductsCount { get; set; }
    public int ScansPerMinute { get; set; }
    public string LastScannedProduct { get; set; } = string.Empty;
    public bool IsProcessing { get; set; }
    public DateTime FirstScanTime { get; set; } = DateTime.UtcNow;
    public string Input { get; set; } = string.Empty;
}
