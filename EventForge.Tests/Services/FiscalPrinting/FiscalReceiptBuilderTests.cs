using Prym.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;

namespace EventForge.Tests.Services.FiscalPrinting;

/// <summary>
/// Unit tests for <see cref="FiscalReceiptBuilder"/> verifying that the correct
/// command frames are generated for all 11 POS scenarios.
/// </summary>
[Trait("Category", "Unit")]
public class FiscalReceiptBuilderTests
{
    private readonly FiscalReceiptBuilder _builder = new();

    // -------------------------------------------------------------------------
    //  Helper: verify STX/ETX/checksum + extract ASCII payload
    // -------------------------------------------------------------------------

    private static void AssertValidFrame(byte[] frame)
    {
        Assert.True(frame.Length >= 4, $"Frame too short: {frame.Length} bytes");
        Assert.Equal(0x02, frame[0]);   // STX
        Assert.Equal(0x03, frame[^2]); // ETX
        byte expected = 0;
        for (int i = 1; i < frame.Length - 2; i++) expected ^= frame[i];
        Assert.Equal(expected, frame[^1]); // checksum
    }

    private static string Payload(byte[] frame)
        => System.Text.Encoding.ASCII.GetString(frame, 1, frame.Length - 3);

    // -------------------------------------------------------------------------
    //  Open / Close receipt
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildOpenReceiptCommand_ReturnsValidFrame_WithCode01()
    {
        var frame = _builder.BuildOpenReceiptCommand();
        AssertValidFrame(frame);
        Assert.StartsWith("01", Payload(frame));
    }

    [Fact]
    public void BuildCloseReceiptCommand_ReturnsValidFrame_WithCode05()
    {
        var frame = _builder.BuildCloseReceiptCommand();
        AssertValidFrame(frame);
        Assert.StartsWith("05", Payload(frame));
    }

    // -------------------------------------------------------------------------
    //  Case 1 – Vendita normale
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPrintItemCommand_NormalItem_UsesCmd02()
    {
        var item = new FiscalReceiptItem
        {
            Description = "Cappellino",
            Quantity = 1,
            UnitPrice = 15.00m,
            VatCode = 1,
            Department = 1,
            ItemFlag = CustomProtocolCommands.ITEM_FLAG_NORMAL
        };

        var frame = _builder.BuildPrintItemCommand(item);

        AssertValidFrame(frame);
        var payload = Payload(frame);
        Assert.StartsWith("02", payload);
        Assert.Contains("Cappellino", payload);
        Assert.Contains("1500", payload); // 15.00 → "1500"
    }

    // -------------------------------------------------------------------------
    //  Case 2 – Sconto riga %
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPrintItemCommand_LinePercentageDiscount_UsesCmd02S()
    {
        var item = new FiscalReceiptItem
        {
            Description = "Cappellino",
            Quantity = 1,
            UnitPrice = 15.00m,
            VatCode = 1,
            Department = 1,
            Discount = 10.00m,
            DiscountType = CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE,
            ItemFlag = CustomProtocolCommands.ITEM_FLAG_NORMAL
        };

        var frame = _builder.BuildPrintItemCommand(item);

        AssertValidFrame(frame);
        var payload = Payload(frame);
        Assert.StartsWith("02S", payload);
        Assert.Contains("1500", payload); // unit price
        Assert.Contains("1000", payload); // discount 10% → "1000"
        Assert.Contains("P", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 3 – Sconto riga €
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPrintItemCommand_LineAmountDiscount_UsesCmd02S()
    {
        var item = new FiscalReceiptItem
        {
            Description = "Zaino",
            Quantity = 1,
            UnitPrice = 89.90m,
            VatCode = 1,
            Department = 1,
            Discount = 20.00m,
            DiscountType = CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT,
            ItemFlag = CustomProtocolCommands.ITEM_FLAG_NORMAL
        };

        var frame = _builder.BuildPrintItemCommand(item);

        AssertValidFrame(frame);
        var payload = Payload(frame);
        Assert.StartsWith("02S", payload);
        Assert.Contains("8990", payload); // unit price
        Assert.Contains("2000", payload); // €20 → "2000"
        Assert.Contains("A", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 5 – Omaggio
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPrintItemCommand_FreeItem_UsesCmd02G()
    {
        var item = new FiscalReceiptItem
        {
            Description = "Gadget promozionale",
            Quantity = 1,
            UnitPrice = 5.00m,
            VatCode = 1,
            Department = 1,
            ItemFlag = CustomProtocolCommands.ITEM_FLAG_FREE
        };

        var frame = _builder.BuildPrintItemCommand(item);

        AssertValidFrame(frame);
        var payload = Payload(frame);
        Assert.StartsWith("02G", payload);
        Assert.Contains("Gadget promozionale", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 6 – Reso
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPrintItemCommand_ReturnItem_NegativeQuantityInPayload()
    {
        var item = new FiscalReceiptItem
        {
            Description = "Maglietta",
            Quantity = -1,
            UnitPrice = 29.90m,
            VatCode = 1,
            Department = 4,
            ItemFlag = CustomProtocolCommands.ITEM_FLAG_RETURN
        };

        var frame = _builder.BuildPrintItemCommand(item);

        AssertValidFrame(frame);
        var payload = Payload(frame);
        Assert.StartsWith("02", payload);
        Assert.Contains("-1", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 7 – Sconto globale %
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildGlobalDiscountCommand_Percentage_UsesCmd03S()
    {
        var discount = new FiscalDiscount
        {
            Value = 15.00m,
            Type = CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE,
            Description = "Sconto Fidelity"
        };

        var frame = _builder.BuildGlobalDiscountCommand(discount);

        AssertValidFrame(frame);
        var payload = Payload(frame);
        Assert.StartsWith("03S", payload);
        Assert.Contains("1500", payload);
        Assert.Contains("P", payload);
        Assert.Contains("Sconto Fidelity", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 8 – Sconto globale €
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildGlobalDiscountCommand_Amount_UsesCmd03S()
    {
        var discount = new FiscalDiscount
        {
            Value = 10.00m,
            Type = CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT,
            Description = "Buono sconto"
        };

        var frame = _builder.BuildGlobalDiscountCommand(discount);

        AssertValidFrame(frame);
        var payload = Payload(frame);
        Assert.StartsWith("03S", payload);
        Assert.Contains("1000", payload);
        Assert.Contains("A", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 9 – Maggiorazione globale
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildGlobalSurchargeCommand_UsesCmd03M()
    {
        var surcharge = new FiscalSurcharge
        {
            Value = 2.50m,
            Type = CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT,
            Description = "Coperto 2 persone",
            VatCode = 2
        };

        var frame = _builder.BuildGlobalSurchargeCommand(surcharge);

        AssertValidFrame(frame);
        var payload = Payload(frame);
        Assert.StartsWith("03M", payload);
        Assert.Contains("250", payload);
        Assert.Contains("Coperto 2 persone", payload);
    }

    // -------------------------------------------------------------------------
    //  Cases 10 & 11 – Pagamenti multipli / Resto
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPaymentCommands_MultiplePayments_ReturnsOneFrameEach()
    {
        var payments = new[]
        {
            new FiscalPayment { Amount = 20.00m, MethodCode = 1, Description = "Contanti" },
            new FiscalPayment { Amount = 30.00m, MethodCode = 4, Description = "Carta" }
        };

        var frames = _builder.BuildPaymentCommands(payments);

        Assert.Equal(2, frames.Count);
        foreach (var f in frames) AssertValidFrame(f);
        Assert.Contains("2000", Payload(frames[0]));
        Assert.Contains("3000", Payload(frames[1]));
        Assert.All(frames, f => Assert.StartsWith("04", Payload(f)));
    }

    [Fact]
    public void BuildPaymentCommands_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => _builder.BuildPaymentCommands([]));
    }

    [Fact]
    public void BuildPaymentCommands_Change_TenderedAmountEncoded()
    {
        // Case 11: pay €50 for a €47.50 total – printer calculates change automatically
        var payments = new[]
        {
            new FiscalPayment { Amount = 50.00m, MethodCode = 1, Description = "Contanti" }
        };

        var frames = _builder.BuildPaymentCommands(payments);

        Assert.Single(frames);
        AssertValidFrame(frames[0]);
        Assert.Contains("5000", Payload(frames[0])); // €50 tendered
    }

    // -------------------------------------------------------------------------
    //  Loyalty lines
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildLoyaltyLines_WithCard_ReturnsDescriptiveFrames()
    {
        var loyalty = new LoyaltyReceiptData
        {
            HasCard = true,
            CardType = "Gold",
            CardNumber = "1234567890",
            PointsEarned = 15,
            CurrentBalance = 1200
        };

        var frames = _builder.BuildLoyaltyLines(loyalty);

        Assert.NotEmpty(frames);
        foreach (var f in frames)
        {
            AssertValidFrame(f);
            Assert.StartsWith("20", Payload(f)); // CMD_PRINT_DESCRIPTIVE
        }
    }

    [Fact]
    public void BuildLoyaltyLines_NoCard_ReturnsEmpty()
    {
        var frames = _builder.BuildLoyaltyLines(new LoyaltyReceiptData { HasCard = false });
        Assert.Empty(frames);
    }

    [Fact]
    public void BuildLoyaltyLines_Null_ReturnsEmpty()
    {
        var frames = _builder.BuildLoyaltyLines(null);
        Assert.Empty(frames);
    }

    // -------------------------------------------------------------------------
    //  Full sequence
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildFullReceiptSequence_CompleteReceipt_CorrectOrder()
    {
        var receipt = new FiscalReceiptData
        {
            Items =
            [
                new FiscalReceiptItem
                {
                    Description = "Cappellino", Quantity = 1, UnitPrice = 15m, VatCode = 1, Department = 1,
                    Discount = 10m, DiscountType = "P"
                },
                new FiscalReceiptItem
                {
                    Description = "Zaino", Quantity = 1, UnitPrice = 89.90m, VatCode = 1, Department = 1
                }
            ],
            GlobalDiscount = new FiscalDiscount { Value = 5m, Type = "P", Description = "Sconto Fidelity" },
            Payments = [new FiscalPayment { Amount = 100m, MethodCode = 1 }],
            LoyaltyData = new LoyaltyReceiptData
            {
                HasCard = true, CardType = "Silver", CardNumber = "9876", PointsEarned = 10, CurrentBalance = 500
            }
        };

        var sequence = _builder.BuildFullReceiptSequence(receipt);

        Assert.NotEmpty(sequence);
        foreach (var f in sequence) AssertValidFrame(f);

        // First frame must be CMD_OPEN_RECEIPT
        Assert.StartsWith("01", Payload(sequence[0]));
        // Last frame must be CMD_CLOSE_RECEIPT
        Assert.StartsWith("05", Payload(sequence[^1]));
    }

    [Fact]
    public void BuildFullReceiptSequence_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _builder.BuildFullReceiptSequence(null!));
    }

    // -------------------------------------------------------------------------
    //  Refund sequence
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildRefundReceiptSequence_ItemsHaveNegativeQuantity()
    {
        var refund = new FiscalRefundData
        {
            OriginalReceiptNumber = "0001",
            Items =
            [
                new FiscalReceiptItem { Description = "Maglietta", Quantity = 1, UnitPrice = 29.90m, VatCode = 1 }
            ],
            Payments = [new FiscalPayment { Amount = 29.90m, MethodCode = 1 }]
        };

        var sequence = _builder.BuildRefundReceiptSequence(refund);

        Assert.NotEmpty(sequence);
        // Second frame must contain an item with quantity -1
        Assert.StartsWith("01", Payload(sequence[0])); // open
        Assert.Contains("-1", Payload(sequence[1]));   // item with negative qty
    }

    // -------------------------------------------------------------------------
    //  Null guards
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildPrintItemCommand_Null_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => _builder.BuildPrintItemCommand(null!));

    [Fact]
    public void BuildGlobalDiscountCommand_Null_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => _builder.BuildGlobalDiscountCommand(null!));

    [Fact]
    public void BuildGlobalSurchargeCommand_Null_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => _builder.BuildGlobalSurchargeCommand(null!));
}
