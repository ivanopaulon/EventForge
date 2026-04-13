using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Prym.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;

namespace EventForge.Server.Services.FiscalPrinting.EpsonProtocol;

/// <summary>
/// Builds SOAP/XML request documents for the Epson POS Printer WebAPI (ePOS-Print XML).
/// All documents target the <c>POST /api/1/request</c> endpoint on the printer's
/// embedded HTTP server.
/// </summary>
/// <remarks>
/// The ePOS-Print XML specification defines a rich set of commands (<c>&lt;text&gt;</c>,
/// <c>&lt;cut&gt;</c>, <c>&lt;pulse&gt;</c>, <c>&lt;feed&gt;</c>, etc.) that control
/// the Epson TM-series printer.  For fiscal-style receipt printing (Italian mercato),
/// the builder formats structured receipt documents using <c>&lt;text&gt;</c> commands
/// with appropriate alignment and bold markers, followed by a <c>&lt;cut&gt;</c>.
/// </remarks>
public static class EpsonXmlBuilder
{
    // -------------------------------------------------------------------------
    //  Fiscal receipt
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the full SOAP/XML document for printing a fiscal-style receipt.
    /// </summary>
    public static string BuildFiscalReceipt(FiscalReceiptData receipt, string devid, int timeoutMs)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var body = CreateEposPrintElement(devid, timeoutMs);

        // --- Header lines (non-fiscal, descriptive) ---
        if (receipt.HeaderLines?.Count > 0)
        {
            foreach (var line in receipt.HeaderLines)
                body.Add(TextLine(line));
            body.Add(FeedLines(1));
        }

        // --- Receipt title ---
        body.Add(TextBoldCenter("** SCONTRINO FISCALE **"));
        body.Add(Separator());
        body.Add(FeedLines(1));

        // --- Items ---
        foreach (var item in receipt.Items)
            AddItemText(body, item);

        // --- Global discount ---
        if (receipt.GlobalDiscount is not null)
        {
            body.Add(Separator());
            string discLabel = receipt.GlobalDiscount.Type.Contains("P", StringComparison.OrdinalIgnoreCase)
                ? $"Sconto {receipt.GlobalDiscount.Value:F2}%"
                : $"Sconto €{receipt.GlobalDiscount.Value:F2}";
            if (!string.IsNullOrEmpty(receipt.GlobalDiscount.Description))
                discLabel = Truncate(receipt.GlobalDiscount.Description, 30);
            body.Add(TextRightAligned(discLabel, $"-€{receipt.GlobalDiscount.Value:F2}"));
        }

        // --- Global surcharge ---
        if (receipt.GlobalSurcharge is not null)
        {
            body.Add(Separator());
            string surLabel = Truncate(receipt.GlobalSurcharge.Description, 30);
            body.Add(TextRightAligned(surLabel, $"+€{receipt.GlobalSurcharge.Value:F2}"));
        }

        // --- Subtotal separator ---
        body.Add(Separator());

        // --- Payments ---
        decimal totalPaid = 0m;
        foreach (var payment in receipt.Payments)
        {
            string label = Truncate(payment.Description ?? "Pagamento", 24);
            body.Add(TextRightAligned(label, $"€{payment.Amount:F2}"));
            totalPaid += payment.Amount;
        }

        body.Add(Separator());

        // --- Total ---
        body.Add(TextBoldRightAligned("TOTALE", $"€{totalPaid:F2}"));
        body.Add(FeedLines(1));

        // --- Loyalty ---
        if (receipt.LoyaltyData is { HasCard: true } loyalty)
        {
            body.Add(Separator());
            foreach (var line in BuildLoyaltyLines(loyalty))
                body.Add(TextLine(line));
            body.Add(FeedLines(1));
        }

        // --- Footer lines ---
        if (receipt.FooterLines?.Count > 0)
        {
            body.Add(Separator());
            foreach (var line in receipt.FooterLines)
                body.Add(TextLine(line));
        }

        body.Add(FeedLines(3));
        body.Add(CutFeed());

        return WrapInSoap(body);
    }

    /// <summary>
    /// Builds the SOAP/XML document for cancelling the currently open receipt.
    /// Prints a void notice and cuts the paper.
    /// </summary>
    public static string BuildCancelReceipt(string devid, int timeoutMs)
    {
        var body = CreateEposPrintElement(devid, timeoutMs);

        body.Add(FeedLines(1));
        body.Add(TextBoldCenter("** ANNULLO SCONTRINO **"));
        body.Add(FeedLines(3));
        body.Add(CutFeed());

        return WrapInSoap(body);
    }

    /// <summary>
    /// Builds the SOAP/XML document for a full or partial refund receipt (reso).
    /// </summary>
    public static string BuildRefundReceipt(FiscalRefundData refund, string devid, int timeoutMs)
    {
        ArgumentNullException.ThrowIfNull(refund);

        var body = CreateEposPrintElement(devid, timeoutMs);

        body.Add(TextBoldCenter("** NOTA DI CREDITO **"));
        if (!string.IsNullOrEmpty(refund.OriginalReceiptNumber))
            body.Add(TextLine($"Rif. scontrino: {refund.OriginalReceiptNumber}"));
        body.Add(Separator());
        body.Add(FeedLines(1));

        foreach (var item in refund.Items)
        {
            var qty = Math.Abs(item.Quantity);
            body.Add(TextRightAligned(
                Truncate(item.Description, 24),
                $"-€{qty * item.UnitPrice:F2}"));
        }

        body.Add(Separator());

        foreach (var payment in refund.Payments)
        {
            string label = Truncate(payment.Description ?? "Rimborso", 24);
            body.Add(TextRightAligned(label, $"€{payment.Amount:F2}"));
        }

        body.Add(FeedLines(3));
        body.Add(CutFeed());

        return WrapInSoap(body);
    }

    // -------------------------------------------------------------------------
    //  Daily closure / Z-report
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the SOAP/XML document that prints the daily fiscal closure summary (Z-report).
    /// The summary is retrieved from the <see cref="DailyClosureResultDto"/> after the
    /// closure has been recorded in the EventForge database.
    /// </summary>
    public static string BuildZReport(DailyClosureResultDto closure, string devid, int timeoutMs)
    {
        ArgumentNullException.ThrowIfNull(closure);

        var body = CreateEposPrintElement(devid, timeoutMs);

        body.Add(TextBoldCenter("*** CHIUSURA GIORNALIERA ***"));
        body.Add(TextBoldCenter("** Z - REPORT **"));
        body.Add(Separator());
        body.Add(TextLine($"Data:         {closure.ClosedAt:dd/MM/yyyy HH:mm}"));
        body.Add(TextLine($"Operatore:    {Truncate(closure.Operator ?? "-", 20)}"));
        if (closure.ZReportNumber > 0)
            body.Add(TextLine($"Numero Z:     {closure.ZReportNumber:D4}"));
        body.Add(Separator());
        body.Add(TextRightAligned("N. scontrini:", $"{closure.ReceiptCount}"));
        body.Add(TextRightAligned("Totale:       ", $"€{closure.TotalAmount:F2}"));
        body.Add(TextRightAligned("di cui contanti:", $"€{closure.CashAmount:F2}"));
        body.Add(TextRightAligned("di cui elettronico:", $"€{(closure.TotalAmount - closure.CashAmount):F2}"));
        body.Add(Separator());
        body.Add(FeedLines(3));
        body.Add(CutFeed());

        return WrapInSoap(body);
    }

    // -------------------------------------------------------------------------
    //  Management commands
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the SOAP/XML document for opening the cash drawer.
    /// Uses the ePOS-Print <c>&lt;pulse&gt;</c> command on drawer port 1.
    /// </summary>
    public static string BuildOpenDrawer(string devid, int timeoutMs)
    {
        var body = CreateEposPrintElement(devid, timeoutMs);
        body.Add(new XElement("pulse",
            new XAttribute("drawer", EpsonProtocolConstants.Drawer1),
            new XAttribute("time", EpsonProtocolConstants.PulseTime100)));
        return WrapInSoap(body);
    }

    /// <summary>
    /// Builds the SOAP/XML document for querying the printer status.
    /// Sends an empty <c>&lt;epos-print&gt;</c> – the printer responds with
    /// its current status in the <c>status</c> attribute of the response element.
    /// </summary>
    public static string BuildStatusQuery(string devid, int timeoutMs)
    {
        // An empty epos-print body is enough to get a status response
        var body = CreateEposPrintElement(devid, timeoutMs);
        return WrapInSoap(body);
    }

    // -------------------------------------------------------------------------
    //  SOAP envelope helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Wraps an <c>&lt;epos-print&gt;</c> element in a standard SOAP envelope
    /// addressed to the Epson WebAPI endpoint.
    /// </summary>
    internal static string WrapInSoap(XElement eposPrintElement)
    {
        XNamespace soapNs = EpsonProtocolConstants.SoapEnvelopeNamespace;

        var envelope = new XElement(
            soapNs + "Envelope",
            new XAttribute(XNamespace.Xmlns + "SOAP-ENV", EpsonProtocolConstants.SoapEnvelopeNamespace),
            new XElement(
                soapNs + "Body",
                eposPrintElement));

        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="utf-8"?>""");
        sb.Append(envelope.ToString(SaveOptions.DisableFormatting));
        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    //  Private XML element helpers
    // -------------------------------------------------------------------------

    private static XElement CreateEposPrintElement(string devid, int timeoutMs)
    {
        XNamespace ns = EpsonProtocolConstants.EposPrintNamespace;
        return new XElement(ns + "epos-print",
            new XAttribute("devid", devid),
            new XAttribute("timeout", timeoutMs));
    }

    /// <summary>Plain text line (left-aligned, newline at end).</summary>
    private static XElement TextLine(string text) =>
        new("text",
            new XAttribute("align", EpsonProtocolConstants.AlignLeft),
            Sanitize(text) + "\n");

    /// <summary>Bold, centre-aligned text line.</summary>
    private static XElement TextBoldCenter(string text) =>
        new("text",
            new XAttribute("align", EpsonProtocolConstants.AlignCenter),
            new XAttribute("bold", "true"),
            Sanitize(text) + "\n");

    /// <summary>Right-aligned two-column line (label + value).</summary>
    private static XElement TextRightAligned(string label, string value)
    {
        int totalWidth = EpsonProtocolConstants.DefaultMaxCharsPerLine;
        int valueWidth = value.Length + 1;
        int labelWidth = totalWidth - valueWidth;
        string paddedLabel = label.Length > labelWidth
            ? label[..labelWidth]
            : label.PadRight(labelWidth);
        return new XElement("text",
            new XAttribute("align", EpsonProtocolConstants.AlignLeft),
            paddedLabel + value + "\n");
    }

    /// <summary>Bold, right-aligned two-column line.</summary>
    private static XElement TextBoldRightAligned(string label, string value)
    {
        int totalWidth = EpsonProtocolConstants.DefaultMaxCharsPerLine;
        int valueWidth = value.Length + 1;
        int labelWidth = totalWidth - valueWidth;
        string paddedLabel = label.Length > labelWidth
            ? label[..labelWidth]
            : label.PadRight(labelWidth);
        return new XElement("text",
            new XAttribute("align", EpsonProtocolConstants.AlignLeft),
            new XAttribute("bold", "true"),
            paddedLabel + value + "\n");
    }

    /// <summary>Dashed separator line.</summary>
    private static XElement Separator() =>
        new("text",
            new XAttribute("align", EpsonProtocolConstants.AlignLeft),
            new string('-', EpsonProtocolConstants.DefaultMaxCharsPerLine) + "\n");

    /// <summary>Paper feed command.</summary>
    private static XElement FeedLines(int lines) =>
        new("feed",
            new XAttribute("line", lines));

    /// <summary>Cut with feed.</summary>
    private static XElement CutFeed() =>
        new("cut",
            new XAttribute("type", EpsonProtocolConstants.CutFeed));

    private static void AddItemText(XElement parent, FiscalReceiptItem item)
    {
        decimal total = item.Quantity * item.UnitPrice;

        if (item.Quantity != 1m)
        {
            // Multi-line: qty × price = total
            parent.Add(TextLine(Truncate(item.Description, EpsonProtocolConstants.DefaultMaxCharsPerLine)));
            string detail = $"  {item.Quantity:F3} x €{item.UnitPrice:F2}";
            parent.Add(TextRightAligned(detail, $"€{total:F2}"));
        }
        else
        {
            // Single line: description + price
            parent.Add(TextRightAligned(
                Truncate(item.Description, EpsonProtocolConstants.DefaultMaxCharsPerLine - 10),
                $"€{item.UnitPrice:F2}"));
        }

        // Line discount
        if (item.Discount > 0 && !string.IsNullOrEmpty(item.DiscountType))
        {
            string discLabel = item.DiscountDescription
                ?? (item.DiscountType.Contains("P", StringComparison.OrdinalIgnoreCase)
                    ? $"  Sconto {item.Discount:F2}%"
                    : $"  Sconto €{item.Discount:F2}");
            parent.Add(TextLine(Truncate(discLabel, EpsonProtocolConstants.DefaultMaxCharsPerLine)));
        }
    }

    private static IEnumerable<string> BuildLoyaltyLines(LoyaltyReceiptData loyalty)
    {
        yield return "*** FIDELITY ***";
        yield return $"Carta: {loyalty.CardType} {loyalty.CardNumber}";
        yield return $"Punti acquistati: {loyalty.PointsEarned}";

        if (loyalty.PointsRedeemed > 0)
            yield return $"Punti utilizzati: {loyalty.PointsRedeemed}";

        yield return $"Saldo punti: {loyalty.CurrentBalance}";

        if (loyalty.NextRewardPoints > 0)
            yield return $"Prossimo premio: {loyalty.NextRewardPoints} punti";

        if (loyalty.DiscountApplied > 0)
            yield return $"Sconto fidelity: €{loyalty.DiscountApplied:F2}";
    }

    /// <summary>Truncates to <paramref name="maxLength"/> characters.</summary>
    internal static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length > maxLength ? text[..maxLength] : text;
    }

    /// <summary>
    /// Removes XML-unsafe characters from a text string while preserving printable content.
    /// </summary>
    private static string Sanitize(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
