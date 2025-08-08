using EventForge.DTOs.Printing;

namespace EventForge.Server.Services.Printing;

/// <summary>
/// Service for generating print-ready content for various document types
/// </summary>
public static class PrintContentGenerator
{
    /// <summary>
    /// Generates a receipt content for thermal printer
    /// </summary>
    public static string GenerateReceipt(ReceiptData receiptData)
    {
        var content = $@"
{receiptData.BusinessName.PadCenter(32)}
{receiptData.BusinessAddress.PadCenter(32)}
{receiptData.BusinessPhone.PadCenter(32)}
{new string('=', 32)}

Receipt #: {receiptData.ReceiptNumber}
Date: {receiptData.Date:yyyy-MM-dd HH:mm:ss}
Cashier: {receiptData.CashierName}

{new string('-', 32)}
ITEMS:
";

        foreach (var item in receiptData.Items)
        {
            content += $"{item.Name,-20} {item.Quantity,3}x\n";
            content += $"{"",20} {item.UnitPrice:C} {item.Total:C}\n";
        }

        content += $@"
{new string('-', 32)}
Subtotal: {receiptData.Subtotal:C,23}
Tax: {receiptData.Tax:C,28}
{new string('=', 32)}
TOTAL: {receiptData.Total:C,26}

Payment: {receiptData.PaymentMethod,-16} {receiptData.AmountPaid:C}
Change: {receiptData.Change:C,27}

{new string('=', 32)}
Thank you for your business!
{receiptData.FooterMessage.PadCenter(32)}

";
        return content;
    }

    /// <summary>
    /// Generates a test page for printer verification
    /// </summary>
    public static string GenerateTestPage(string printerName)
    {
        return $@"
EventForge Print Test
====================

Printer: {printerName}
Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Test ID: {Guid.NewGuid().ToString()[..8]}

Character Set Test:
ABCDEFGHIJKLMNOPQRSTUVWXYZ
abcdefghijklmnopqrstuvwxyz
0123456789
!@#$%^&*()_+-=[]{{}}|;':"",./<>?

Line Quality Test:
{new string('-', 32)}
{new string('=', 32)}
{new string('*', 32)}

Print Quality: GOOD
Status: TEST SUCCESSFUL
";
    }

    /// <summary>
    /// Generates a label for shipping or inventory
    /// </summary>
    public static string GenerateLabel(LabelData labelData)
    {
        return $@"
{labelData.Title.PadCenter(32)}
{new string('=', 32)}

{labelData.Line1}
{labelData.Line2}
{labelData.Line3}
{labelData.Line4}

{labelData.Barcode}
";
    }
}

/// <summary>
/// Data structure for receipt generation
/// </summary>
public class ReceiptData
{
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessAddress { get; set; } = string.Empty;
    public string BusinessPhone { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string CashierName { get; set; } = string.Empty;
    public List<ReceiptItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
    public string FooterMessage { get; set; } = "Visit us again soon!";
}

/// <summary>
/// Individual item on a receipt
/// </summary>
public class ReceiptItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// Data structure for label generation
/// </summary>
public class LabelData
{
    public string Title { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string Line2 { get; set; } = string.Empty;
    public string Line3 { get; set; } = string.Empty;
    public string Line4 { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
}

/// <summary>
/// Extension methods for string formatting
/// </summary>
public static class StringExtensions
{
    public static string PadCenter(this string text, int width)
    {
        if (text.Length >= width) return text;
        
        var totalPadding = width - text.Length;
        var leftPadding = totalPadding / 2;
        var rightPadding = totalPadding - leftPadding;
        
        return new string(' ', leftPadding) + text + new string(' ', rightPadding);
    }
}