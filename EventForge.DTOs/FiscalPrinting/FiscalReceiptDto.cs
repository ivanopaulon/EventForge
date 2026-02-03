namespace EventForge.DTOs.FiscalPrinting;

/// <summary>
/// Data structure for a fiscal receipt.
/// </summary>
public class FiscalReceiptData
{
    /// <summary>
    /// Receipt items.
    /// </summary>
    public List<FiscalReceiptItem> Items { get; set; } = new();

    /// <summary>
    /// Payment methods used.
    /// </summary>
    public List<FiscalPayment> Payments { get; set; } = new();

    /// <summary>
    /// Customer information (optional).
    /// </summary>
    public string? CustomerInfo { get; set; }

    /// <summary>
    /// Loyalty program data (optional).
    /// </summary>
    public LoyaltyReceiptData? LoyaltyData { get; set; }

    /// <summary>
    /// Custom header lines for the receipt.
    /// </summary>
    public List<string> HeaderLines { get; set; } = new();

    /// <summary>
    /// Custom footer lines for the receipt.
    /// </summary>
    public List<string> FooterLines { get; set; } = new();
}

/// <summary>
/// Represents a single item on a fiscal receipt.
/// </summary>
public class FiscalReceiptItem
{
    /// <summary>
    /// Item description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity sold.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// VAT code (1-10 for fiscal printer).
    /// </summary>
    public int VatCode { get; set; }

    /// <summary>
    /// Department code (default: 1).
    /// </summary>
    public int Department { get; set; } = 1;
}

/// <summary>
/// Represents a payment method used in the fiscal receipt.
/// </summary>
public class FiscalPayment
{
    /// <summary>
    /// Amount paid with this method.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method code (1-10 for fiscal printer).
    /// </summary>
    public int MethodCode { get; set; }

    /// <summary>
    /// Payment description (optional).
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Loyalty program data for fiscal receipts.
/// </summary>
public class LoyaltyReceiptData
{
    /// <summary>
    /// Indicates if customer has a loyalty card.
    /// </summary>
    public bool HasCard { get; set; }

    /// <summary>
    /// Type of loyalty card.
    /// </summary>
    public string CardType { get; set; } = string.Empty;

    /// <summary>
    /// Card number.
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Points earned with this purchase.
    /// </summary>
    public int PointsEarned { get; set; }

    /// <summary>
    /// Points redeemed in this purchase.
    /// </summary>
    public int PointsRedeemed { get; set; }

    /// <summary>
    /// Current point balance after transaction.
    /// </summary>
    public int CurrentBalance { get; set; }

    /// <summary>
    /// Points needed to reach next reward.
    /// </summary>
    public int NextRewardPoints { get; set; }

    /// <summary>
    /// Discount amount applied from loyalty program.
    /// </summary>
    public decimal DiscountApplied { get; set; }
}

/// <summary>
/// Result of a fiscal print operation.
/// </summary>
public class FiscalPrintResult
{
    /// <summary>
    /// Indicates if the print was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Receipt number assigned by fiscal printer.
    /// </summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Fiscal printer serial number.
    /// </summary>
    public string? FiscalSerial { get; set; }

    /// <summary>
    /// Error message if print failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Date and time of the print operation.
    /// </summary>
    public DateTime? PrintDate { get; set; }
}

/// <summary>
/// Data structure for a fiscal refund/return.
/// </summary>
public class FiscalRefundData
{
    /// <summary>
    /// Original receipt number being refunded.
    /// </summary>
    public string OriginalReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Original receipt date.
    /// </summary>
    public DateTime OriginalReceiptDate { get; set; }

    /// <summary>
    /// Items being refunded.
    /// </summary>
    public List<FiscalReceiptItem> Items { get; set; } = new();

    /// <summary>
    /// Refund payment methods.
    /// </summary>
    public List<FiscalPayment> Payments { get; set; } = new();

    /// <summary>
    /// Reason for the refund.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Status information for a fiscal printer.
/// </summary>
public class FiscalPrinterStatus
{
    /// <summary>
    /// Indicates if the printer is online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Paper status (OK, LOW, OUT, UNKNOWN).
    /// </summary>
    public string PaperStatus { get; set; } = "UNKNOWN";

    /// <summary>
    /// Last error message from printer.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Last time status was checked.
    /// </summary>
    public DateTime? LastCheck { get; set; }
}
