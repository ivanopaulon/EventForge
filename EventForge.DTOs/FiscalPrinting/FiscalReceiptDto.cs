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

    /// <summary>
    /// Global discount applied to the entire receipt total (optional).
    /// Sent as CMD_GLOBAL_DISCOUNT ("03S") after the last item and before payments.
    /// </summary>
    public FiscalDiscount? GlobalDiscount { get; set; }

    /// <summary>
    /// Global surcharge applied to the entire receipt total (optional).
    /// Sent as CMD_GLOBAL_SURCHARGE ("03M") after the last item and before payments.
    /// Typical use: cover charge (coperto), service fee.
    /// </summary>
    public FiscalSurcharge? GlobalSurcharge { get; set; }
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
    /// Quantity sold. Use a negative value for return items (<see cref="ItemFlag"/> = "2").
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

    /// <summary>
    /// Discount or surcharge value applied to this line item (optional).
    /// A positive value is a discount (reduces the price); the sign is determined by the command
    /// (<see cref="CustomProtocol.CustomProtocolCommands.CMD_PRINT_ITEM_WITH_DISCOUNT"/> vs
    /// <see cref="CustomProtocol.CustomProtocolCommands.CMD_PRINT_ITEM_WITH_SURCHARGE"/>).
    /// </summary>
    public decimal? Discount { get; set; }

    /// <summary>
    /// Discount or surcharge type for this line item.
    /// "P" = percentage (e.g., 10.00 means -10%), "A" = fixed amount (e.g., 5.00 means -€5.00).
    /// Defaults to null (no discount/surcharge). Use constants from
    /// <see cref="CustomProtocol.CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE"/> and
    /// <see cref="CustomProtocol.CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT"/>.
    /// </summary>
    public string? DiscountType { get; set; }

    /// <summary>
    /// Optional description printed alongside the discount/surcharge on the receipt
    /// (e.g., "Sconto Fidelity Gold", "Supplemento servizio").
    /// </summary>
    public string? DiscountDescription { get; set; }

    /// <summary>
    /// Item flag controlling how the item is printed:
    /// "0" = normal sale (default), "1" = free/gift (omaggio), "2" = return (reso).
    /// Use constants from <see cref="CustomProtocol.CustomProtocolCommands"/>.
    /// </summary>
    public string ItemFlag { get; set; } = "0";
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
/// Represents a global discount applied to the entire fiscal receipt total.
/// Sent as CMD_GLOBAL_DISCOUNT ("03S") after all items and before payments.
/// Example: -15% fidelity discount, -€10 gift voucher.
/// </summary>
public class FiscalDiscount
{
    /// <summary>
    /// Discount value. Interpretation depends on <see cref="Type"/>:
    /// for percentage, 10.00 means 10%; for amount, 10.00 means €10.00.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Discount type: "P" = percentage, "A" = fixed amount.
    /// Defaults to percentage. Use constants from
    /// <see cref="CustomProtocol.CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE"/> and
    /// <see cref="CustomProtocol.CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT"/>.
    /// </summary>
    public string Type { get; set; } = "P";

    /// <summary>
    /// Human-readable description printed on the receipt (e.g., "Sconto Fidelity Gold", "Buono sconto").
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a global surcharge applied to the entire fiscal receipt total.
/// Sent as CMD_GLOBAL_SURCHARGE ("03M") after all items and before payments.
/// Typical use: cover charge (coperto), service fee, delivery surcharge.
/// </summary>
public class FiscalSurcharge
{
    /// <summary>
    /// Surcharge value. Interpretation depends on <see cref="Type"/>:
    /// for percentage, 5.00 means 5%; for amount, 2.50 means €2.50.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Surcharge type: "P" = percentage, "A" = fixed amount.
    /// Defaults to fixed amount (most cover charges are fixed). Use constants from
    /// <see cref="CustomProtocol.CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE"/> and
    /// <see cref="CustomProtocol.CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT"/>.
    /// </summary>
    public string Type { get; set; } = "A";

    /// <summary>
    /// Human-readable description printed on the receipt (e.g., "Coperto 2 persone", "Servizio al tavolo").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// VAT code applied to the surcharge amount (1-10 for fiscal printer).
    /// Must match the VAT rate applicable to the service/surcharge type.
    /// </summary>
    public int VatCode { get; set; }
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
/// Paper status values reported by the Custom fiscal printer protocol.
/// </summary>
public enum FiscalPrinterPaperStatus
{
    /// <summary>Paper level is normal; printing is unaffected.</summary>
    Ok,

    /// <summary>Paper is running low; replace the roll soon (warning).</summary>
    Low,

    /// <summary>Paper is exhausted; printing is blocked until the roll is replaced.</summary>
    Out,

    /// <summary>Paper status could not be determined (e.g., printer offline).</summary>
    Unknown
}

/// <summary>
/// Status information for a Custom fiscal printer.
/// Fields are populated from the 3-byte bitmap returned by CMD_READ_STATUS ("10").
/// </summary>
public class FiscalPrinterStatus
{
    /// <summary>
    /// Indicates if the printer is online and reachable.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Paper status derived from the status bitmap (OK, LOW, OUT, UNKNOWN).
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

    // --- Custom protocol bitmap fields (byte 1) ---

    /// <summary>Byte 1 bit 0 – Paper is completely out; printing is blocked.</summary>
    public bool IsPaperOut { get; set; }

    /// <summary>Byte 1 bit 1 – Printer cover is open.</summary>
    public bool IsCoverOpen { get; set; }

    /// <summary>Byte 1 bit 2 – Print head error detected.</summary>
    public bool IsHeadError { get; set; }

    /// <summary>Byte 1 bit 3 – Paper cutter error detected.</summary>
    public bool IsCutterError { get; set; }

    /// <summary>Byte 1 bit 4 – Fiscal memory is 100% full; printing is blocked. Requires authorised technical intervention.</summary>
    public bool IsFiscalMemoryFull { get; set; }

    // --- Custom protocol bitmap fields (byte 2) ---

    /// <summary>Byte 2 bit 0 – Paper is running low; replace the roll soon.</summary>
    public bool IsPaperLow { get; set; }

    /// <summary>Byte 2 bit 1 – Fiscal memory is almost full (&gt;90%).</summary>
    public bool IsFiscalMemoryAlmostFull { get; set; }

    /// <summary>Byte 2 bit 2 – Cash drawer is open.</summary>
    public bool IsDrawerOpen { get; set; }

    /// <summary>Byte 2 bit 3 – Print head is overheating; printing may be temporarily suspended.</summary>
    public bool IsHeadOverheat { get; set; }

    // --- Custom protocol bitmap fields (byte 3) ---

    /// <summary>Byte 3 bit 0 – A receipt is currently open (not yet closed or cancelled).</summary>
    public bool IsReceiptOpen { get; set; }

    /// <summary>Byte 3 bit 1 – Printer is in active fiscal mode.</summary>
    public bool IsFiscalModeActive { get; set; }

    /// <summary>
    /// Byte 3 bit 2 – Daily fiscal closure (Z-report) is required.
    /// Custom printers block new receipts if closure is not performed within 24 hours.
    /// </summary>
    public bool IsDailyClosureRequired { get; set; }
}
