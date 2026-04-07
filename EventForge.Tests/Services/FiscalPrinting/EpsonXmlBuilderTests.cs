using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting.EpsonProtocol;

namespace EventForge.Tests.Services.FiscalPrinting;

/// <summary>
/// Unit tests for <see cref="EpsonXmlBuilder"/> – verifies that SOAP/XML documents
/// are built correctly for the Epson POS Printer WebAPI (ePOS-Print XML, Rev. A).
/// </summary>
[Trait("Category", "Unit")]
public class EpsonXmlBuilderTests
{
    private const string EposPrintNs = "http://www.epson-pos.com/schemas/2011/03/epos-print";
    private const string DefaultDevId = "local_printer";
    private const int DefaultTimeout = 10_000;

    // -------------------------------------------------------------------------
    //  SOAP envelope structure
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildStatusQuery_ContainsSoapEnvelope()
    {
        var xml = EpsonXmlBuilder.BuildStatusQuery(DefaultDevId, DefaultTimeout);

        Assert.Contains("SOAP-ENV:Envelope", xml);
        Assert.Contains("SOAP-ENV:Body", xml);
        Assert.Contains("epos-print", xml);
    }

    [Fact]
    public void BuildStatusQuery_EposPrintHasDevidAndTimeout()
    {
        var xml = EpsonXmlBuilder.BuildStatusQuery(DefaultDevId, DefaultTimeout);

        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var eposPrint = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "epos-print");

        Assert.NotNull(eposPrint);
        Assert.Equal(DefaultDevId, eposPrint.Attribute("devid")?.Value);
        Assert.Equal(DefaultTimeout.ToString(), eposPrint.Attribute("timeout")?.Value);
    }

    [Fact]
    public void BuildStatusQuery_UsesCorrectNamespace()
    {
        var xml = EpsonXmlBuilder.BuildStatusQuery(DefaultDevId, DefaultTimeout);

        Assert.Contains(EposPrintNs, xml);
    }

    // -------------------------------------------------------------------------
    //  Open drawer
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildOpenDrawer_ContainsPulseElement()
    {
        var xml = EpsonXmlBuilder.BuildOpenDrawer(DefaultDevId, DefaultTimeout);

        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var pulse = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "pulse");

        Assert.NotNull(pulse);
        Assert.Equal("drawer_1", pulse.Attribute("drawer")?.Value);
        Assert.Equal("pulse_100", pulse.Attribute("time")?.Value);
    }

    // -------------------------------------------------------------------------
    //  Receipt
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildFiscalReceipt_ContainsCutElement()
    {
        var receipt = MakeSingleItemReceipt();

        var xml = EpsonXmlBuilder.BuildFiscalReceipt(receipt, DefaultDevId, DefaultTimeout);
        var doc = System.Xml.Linq.XDocument.Parse(xml);

        var cut = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "cut");

        Assert.NotNull(cut);
        Assert.Equal("feed", cut.Attribute("type")?.Value);
    }

    [Fact]
    public void BuildFiscalReceipt_ContainsItemDescription()
    {
        var receipt = MakeSingleItemReceipt();

        var xml = EpsonXmlBuilder.BuildFiscalReceipt(receipt, DefaultDevId, DefaultTimeout);

        Assert.Contains("Cappellino", xml);
    }

    [Fact]
    public void BuildFiscalReceipt_ContainsPaymentDescription()
    {
        var receipt = MakeSingleItemReceipt();

        var xml = EpsonXmlBuilder.BuildFiscalReceipt(receipt, DefaultDevId, DefaultTimeout);

        Assert.Contains("CONTANTE", xml);
    }

    [Fact]
    public void BuildFiscalReceipt_GlobalDiscount_ContainsDiscountText()
    {
        var receipt = MakeSingleItemReceipt();
        receipt.GlobalDiscount = new FiscalDiscount
        {
            Value = 10m,
            Type = "P",
            Description = "Sconto Fidelity"
        };

        var xml = EpsonXmlBuilder.BuildFiscalReceipt(receipt, DefaultDevId, DefaultTimeout);

        Assert.Contains("Sconto Fidelity", xml);
    }

    [Fact]
    public void BuildFiscalReceipt_HeaderAndFooter_Included()
    {
        var receipt = MakeSingleItemReceipt();
        receipt.HeaderLines.Add("Negozio Test SRL");
        receipt.FooterLines.Add("Grazie per la visita!");

        var xml = EpsonXmlBuilder.BuildFiscalReceipt(receipt, DefaultDevId, DefaultTimeout);

        Assert.Contains("Negozio Test SRL", xml);
        Assert.Contains("Grazie per la visita!", xml);
    }

    // -------------------------------------------------------------------------
    //  Cancel receipt
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildCancelReceipt_ContainsAnnulloText()
    {
        var xml = EpsonXmlBuilder.BuildCancelReceipt(DefaultDevId, DefaultTimeout);

        Assert.Contains("ANNULLO", xml);
    }

    [Fact]
    public void BuildCancelReceipt_ContainsCutElement()
    {
        var xml = EpsonXmlBuilder.BuildCancelReceipt(DefaultDevId, DefaultTimeout);
        var doc = System.Xml.Linq.XDocument.Parse(xml);

        var cut = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "cut");

        Assert.NotNull(cut);
    }

    // -------------------------------------------------------------------------
    //  Refund receipt
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildRefundReceipt_ContainsNotaDiCredito()
    {
        var refund = new FiscalRefundData
        {
            OriginalReceiptNumber = "12345",
            Items = [new() { Description = "Maglietta", Quantity = 1m, UnitPrice = 29.90m }],
            Payments = [new() { Description = "CONTANTE", Amount = 29.90m }]
        };

        var xml = EpsonXmlBuilder.BuildRefundReceipt(refund, DefaultDevId, DefaultTimeout);

        Assert.Contains("NOTA DI CREDITO", xml);
        Assert.Contains("Maglietta", xml);
        Assert.Contains("12345", xml);
    }

    // -------------------------------------------------------------------------
    //  Z-Report
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildZReport_ContainsClosureData()
    {
        var closure = new DailyClosureResultDto
        {
            Success = true,
            ZReportNumber = 42,
            ClosedAt = new DateTime(2025, 4, 7, 23, 0, 0, DateTimeKind.Utc),
            ReceiptCount = 10,
            TotalAmount = 1500.50m,
            CashAmount = 800m,
            Operator = "Mario"
        };

        var xml = EpsonXmlBuilder.BuildZReport(closure, DefaultDevId, DefaultTimeout);

        Assert.Contains("Z - REPORT", xml);
        Assert.Contains("0042", xml);  // zero-padded Z number
        Assert.Contains("Mario", xml);
        Assert.Contains("1500.50", xml);
    }

    [Fact]
    public void BuildZReport_ContainsCutElement()
    {
        var closure = new DailyClosureResultDto { Success = true, ClosedAt = DateTime.UtcNow };

        var xml = EpsonXmlBuilder.BuildZReport(closure, DefaultDevId, DefaultTimeout);
        var doc = System.Xml.Linq.XDocument.Parse(xml);

        var cut = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "cut");

        Assert.NotNull(cut);
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static FiscalReceiptData MakeSingleItemReceipt() => new()
    {
        Items =
        [
            new()
            {
                Description = "Cappellino",
                Quantity = 1m,
                UnitPrice = 15.00m,
                VatCode = 1,
                Department = 1
            }
        ],
        Payments =
        [
            new()
            {
                Description = "CONTANTE",
                Amount = 15.00m,
                MethodCode = 1
            }
        ]
    };
}
