using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Store;

/// <summary>Assignment type for a fiscal drawer.</summary>
public enum FiscalDrawerAssignmentType
{
    Fixed,    // Fixed to a POS
    Floating  // Assigned to an operator
}

/// <summary>Status of a fiscal drawer.</summary>
public enum FiscalDrawerStatus
{
    Active,
    Suspended,
    Closed
}

/// <summary>Type of cash denomination.</summary>
public enum DenominationType
{
    Banknote,
    Coin
}

/// <summary>Type of fiscal drawer transaction.</summary>
public enum FiscalDrawerTransactionType
{
    Sale,
    Deposit,
    Withdrawal,
    Adjustment,
    OpeningBalance,
    ClosingBalance
}

/// <summary>Payment type for a fiscal drawer transaction.</summary>
public enum FiscalDrawerPaymentType
{
    Cash,
    Card,
    Voucher,
    Transfer,
    Other
}

/// <summary>Status of a fiscal drawer session.</summary>
public enum FiscalDrawerSessionStatus
{
    Open,
    Closed
}

/// <summary>DTO for FiscalDrawer output/display operations.</summary>
public class FiscalDrawerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public FiscalDrawerAssignmentType AssignmentType { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public FiscalDrawerStatus Status { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public Guid? PosId { get; set; }
    public string? PosName { get; set; }
    public Guid? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Notes { get; set; }
    public bool HasOpenSession { get; set; }
    public Guid? CurrentSessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>DTO for creating a new FiscalDrawer.</summary>
public class CreateFiscalDrawerDto
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Code cannot exceed 20 characters.")]
    public string? Code { get; set; }

    [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    public string? Description { get; set; }

    [Required]
    public FiscalDrawerAssignmentType AssignmentType { get; set; } = FiscalDrawerAssignmentType.Fixed;

    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Invalid currency code. Use ISO 4217 format (e.g., EUR, USD).")]
    public string CurrencyCode { get; set; } = "EUR";

    public FiscalDrawerStatus Status { get; set; } = FiscalDrawerStatus.Active;

    [Range(0, double.MaxValue, ErrorMessage = "Opening balance must be non-negative.")]
    public decimal OpeningBalance { get; set; } = 0;

    public Guid? PosId { get; set; }
    public Guid? OperatorId { get; set; }

    [MaxLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
    public string? Notes { get; set; }
}

/// <summary>DTO for updating a FiscalDrawer.</summary>
public class UpdateFiscalDrawerDto
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Code cannot exceed 20 characters.")]
    public string? Code { get; set; }

    [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    public string? Description { get; set; }

    [Required]
    public FiscalDrawerAssignmentType AssignmentType { get; set; }

    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Invalid currency code.")]
    public string CurrencyCode { get; set; } = "EUR";

    public FiscalDrawerStatus Status { get; set; }

    public Guid? PosId { get; set; }
    public Guid? OperatorId { get; set; }

    [MaxLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
    public string? Notes { get; set; }
}

/// <summary>DTO for FiscalDrawerSession output/display.</summary>
public class FiscalDrawerSessionDto
{
    public Guid Id { get; set; }
    public Guid FiscalDrawerId { get; set; }
    public string FiscalDrawerName { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public int TransactionCount { get; set; }
    public Guid? OpenedByOperatorId { get; set; }
    public string? OpenedByOperatorName { get; set; }
    public Guid? ClosedByOperatorId { get; set; }
    public string? ClosedByOperatorName { get; set; }
    public FiscalDrawerSessionStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>DTO for opening a fiscal drawer session.</summary>
public class OpenFiscalDrawerSessionDto
{
    [Range(0, double.MaxValue, ErrorMessage = "Opening balance must be non-negative.")]
    public decimal OpeningBalance { get; set; }

    public Guid? OperatorId { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

/// <summary>DTO for closing a fiscal drawer session.</summary>
public class CloseFiscalDrawerSessionDto
{
    [Range(0, double.MaxValue, ErrorMessage = "Closing balance must be non-negative.")]
    public decimal ClosingBalance { get; set; }

    public Guid? OperatorId { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

/// <summary>DTO for FiscalDrawerTransaction output/display.</summary>
public class FiscalDrawerTransactionDto
{
    public Guid Id { get; set; }
    public Guid FiscalDrawerId { get; set; }
    public Guid? FiscalDrawerSessionId { get; set; }
    public FiscalDrawerTransactionType TransactionType { get; set; }
    public FiscalDrawerPaymentType PaymentType { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? SaleSessionId { get; set; }
    public DateTime TransactionAt { get; set; }
    public string? OperatorName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>DTO for creating a fiscal drawer transaction (deposit or withdrawal).</summary>
public class CreateFiscalDrawerTransactionDto
{
    [Required]
    public FiscalDrawerTransactionType TransactionType { get; set; }

    [Required]
    public FiscalDrawerPaymentType PaymentType { get; set; } = FiscalDrawerPaymentType.Cash;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
    public string? Description { get; set; }

    public Guid? OperatorId { get; set; }
}

/// <summary>DTO for a cash denomination (e.g., 2 banknotes of €10).</summary>
public class CashDenominationDto
{
    public Guid Id { get; set; }
    public Guid FiscalDrawerId { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public decimal Value { get; set; }
    public DenominationType DenominationType { get; set; }
    public int Quantity { get; set; }
    public int SortOrder { get; set; }
    public decimal TotalValue => Value * Quantity;
}

/// <summary>DTO for updating cash denomination quantities.</summary>
public class UpdateCashDenominationDto
{
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
    public int Quantity { get; set; }
}

/// <summary>Request DTO for change calculation.</summary>
public class CalculateChangeRequestDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than zero.")]
    public decimal TotalAmount { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Received amount must be greater than zero.")]
    public decimal ReceivedAmount { get; set; }
}

/// <summary>Item in a change calculation response.</summary>
public class ChangeItem
{
    public decimal Value { get; set; }
    public DenominationType DenominationType { get; set; }
    public int Quantity { get; set; }
    public decimal TotalValue => Value * Quantity;
}

/// <summary>Response DTO for change calculation.</summary>
public class CalculateChangeResponseDto
{
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public bool IsExact { get; set; }
    public bool HasSufficientFunds { get; set; }
    public List<ChangeItem> ChangeBreakdown { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>Summary DTO for fiscal drawer on POS page.</summary>
public class FiscalDrawerSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "EUR";
    public decimal CurrentBalance { get; set; }
    public FiscalDrawerStatus Status { get; set; }
    public bool HasOpenSession { get; set; }
    public Guid? CurrentSessionId { get; set; }
    public DateTime? SessionOpenedAt { get; set; }
    public decimal SessionOpeningBalance { get; set; }
    public decimal SessionTotalSales { get; set; }
    public decimal SessionTotalDeposits { get; set; }
    public decimal SessionTotalWithdrawals { get; set; }
}

/// <summary>DTO for sales dashboard statistics.</summary>
public class SalesDashboardDto
{
    public decimal TodayTotalSales { get; set; }
    public decimal TodayCashSales { get; set; }
    public decimal TodayCardSales { get; set; }
    public decimal TodayOtherSales { get; set; }
    public int TodayTransactionCount { get; set; }
    public decimal WeekTotalSales { get; set; }
    public decimal MonthTotalSales { get; set; }
    public decimal TotalDrawerBalance { get; set; }
    public int ActiveDrawersCount { get; set; }
    public int OpenSessionsCount { get; set; }
    public List<FiscalDrawerSummaryDto> DrawerSummaries { get; set; } = new();
    public List<DailySalesPointDto> WeeklySalesTrend { get; set; } = new();
}

/// <summary>Point in time sales data for trend charts.</summary>
public class DailySalesPointDto
{
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
    public int TransactionCount { get; set; }
}
