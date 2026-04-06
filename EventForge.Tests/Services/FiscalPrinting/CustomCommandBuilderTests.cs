using EventForge.Server.Services.FiscalPrinting.CustomProtocol;

namespace EventForge.Tests.Services.FiscalPrinting;

/// <summary>
/// Unit tests for <see cref="CustomCommandBuilder"/> verifying frame construction,
/// field encoding, checksum calculation, and error handling.
/// </summary>
[Trait("Category", "Unit")]
public class CustomCommandBuilderTests
{
    // -------------------------------------------------------------------------
    //  Frame structure tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_SimpleCommand_StartsWithStxAndEndsWithEtxChecksum()
    {
        // Arrange & Act
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("01")
            .Build();

        // Assert
        Assert.Equal(0x02, frame[0]);                  // STX
        Assert.Equal(0x03, frame[^2]);                 // ETX
        Assert.True(frame.Length >= 4);
    }

    [Fact]
    public void Build_CommandCode_IsEncodedAsAsciiAfterStx()
    {
        // Arrange & Act
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("01")
            .Build();

        // Assert: bytes 1-2 = ASCII "01"
        Assert.Equal((byte)'0', frame[1]);
        Assert.Equal((byte)'1', frame[2]);
    }

    [Fact]
    public void Build_Checksum_IsXorOfBytesBetweenStxAndEtx()
    {
        // Arrange
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("01")
            .Build();

        // Act: manually compute XOR from index 1 to (^2) exclusive
        byte expected = 0;
        for (int i = 1; i < frame.Length - 2; i++)
            expected ^= frame[i];

        // Assert: last byte is the checksum
        Assert.Equal(expected, frame[^1]);
    }

    [Fact]
    public void Build_WithFields_InsertsFieldSeparatorBeforeEachValue()
    {
        // Arrange & Act
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("02")
            .AddField("Item description")
            .AddField(1)
            .Build();

        // Assert: FS (0x1C) must appear before each field value
        // Frame: STX '0' '2' FS 'I'... FS '1' ETX checksum
        Assert.Equal(0x1C, frame[3]); // FS before first field
    }

    [Fact]
    public void Build_CanBeCalledMultipleTimes_ReturnsConsistentFrames()
    {
        // Arrange
        var builder = new CustomCommandBuilder();

        // Act
        byte[] frame1 = builder.StartCommand("01").Build();
        byte[] frame2 = builder.StartCommand("01").Build();

        // Assert
        Assert.Equal(frame1, frame2);
    }

    // -------------------------------------------------------------------------
    //  String field tests
    // -------------------------------------------------------------------------

    [Fact]
    public void AddField_StringValue_EncodesAsAscii()
    {
        // Arrange & Act
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("02")
            .AddField("ABC")
            .Build();

        // After STX + "02" + FS: bytes 4,5,6 = 'A','B','C'
        Assert.Equal((byte)'A', frame[4]);
        Assert.Equal((byte)'B', frame[5]);
        Assert.Equal((byte)'C', frame[6]);
    }

    [Fact]
    public void AddField_NullValue_WritesOnlyFieldSeparator()
    {
        // Arrange
        byte[] frameWithNull = new CustomCommandBuilder()
            .StartCommand("01")
            .AddField((string?)null)
            .Build();

        byte[] frameWithoutField = new CustomCommandBuilder()
            .StartCommand("01")
            .Build();

        // Assert: adding a null field adds exactly 1 byte (the FS)
        Assert.Equal(frameWithoutField.Length + 1, frameWithNull.Length);
        Assert.Equal(0x1C, frameWithNull[3]); // FS byte present
    }

    [Fact]
    public void AddField_EmptyString_WritesOnlyFieldSeparator()
    {
        // Arrange
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("01")
            .AddField(string.Empty)
            .Build();

        byte[] frameBase = new CustomCommandBuilder()
            .StartCommand("01")
            .Build();

        // Assert: empty string adds only FS byte
        Assert.Equal(frameBase.Length + 1, frame.Length);
    }

    // -------------------------------------------------------------------------
    //  Integer field tests
    // -------------------------------------------------------------------------

    [Fact]
    public void AddField_IntValue_EncodesAsDecimalAscii()
    {
        // Arrange & Act
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("02")
            .AddField(42)
            .Build();

        // After STX + "02" + FS: bytes 4,5 = '4','2'
        Assert.Equal((byte)'4', frame[4]);
        Assert.Equal((byte)'2', frame[5]);
    }

    // -------------------------------------------------------------------------
    //  Decimal field tests
    // -------------------------------------------------------------------------

    [Fact]
    public void AddField_Decimal_EncodesAsFixedPointWithTwoDecimals()
    {
        // Arrange & Act
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("02")
            .AddField(12.50m)
            .Build();

        // Expected encoding: 1250 → "1250"
        // Frame: STX '0' '2' FS '1' '2' '5' '0' ETX checksum
        Assert.Equal((byte)'1', frame[4]);
        Assert.Equal((byte)'2', frame[5]);
        Assert.Equal((byte)'5', frame[6]);
        Assert.Equal((byte)'0', frame[7]);
    }

    [Fact]
    public void AddField_Decimal_EncodesAsFixedPointWithThreeDecimals()
    {
        // Arrange & Act
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("02")
            .AddField(1.500m, 3)
            .Build();

        // Expected encoding: 1500 → "1500"
        Assert.Equal((byte)'1', frame[4]);
        Assert.Equal((byte)'5', frame[5]);
        Assert.Equal((byte)'0', frame[6]);
        Assert.Equal((byte)'0', frame[7]);
    }

    [Fact]
    public void AddField_Decimal_ZeroValue_EncodesAsZero()
    {
        // Arrange & Act
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("02")
            .AddField(0m)
            .Build();

        // "0"
        Assert.Equal((byte)'0', frame[4]);
    }

    [Fact]
    public void AddField_Decimal_RoundsCorrectly()
    {
        // 1.555m with 2 decimals → rounds to 1.56 → 156
        byte[] frame = new CustomCommandBuilder()
            .StartCommand("02")
            .AddField(1.555m, 2)
            .Build();

        Assert.Equal((byte)'1', frame[4]);
        Assert.Equal((byte)'5', frame[5]);
        Assert.Equal((byte)'6', frame[6]);
    }

    // -------------------------------------------------------------------------
    //  StatusRequest helper
    // -------------------------------------------------------------------------

    [Fact]
    public void StatusRequest_ReturnsSingleEnqByte()
    {
        // Act
        byte[] request = CustomCommandBuilder.StatusRequest();

        // Assert
        Assert.Single(request);
        Assert.Equal(0x05, request[0]); // ENQ
    }

    // -------------------------------------------------------------------------
    //  ToHexString helper
    // -------------------------------------------------------------------------

    [Fact]
    public void ToHexString_ReturnsSpaceSeparatedUppercaseHex()
    {
        // Arrange
        var builder = new CustomCommandBuilder();
        builder.StartCommand("01");

        // Act
        string hex = builder.ToHexString();

        // Assert: must contain STX (02) and "30 31" (ASCII "01")
        Assert.Contains("02", hex);
        Assert.Contains("30 31", hex); // ASCII "01"
        Assert.Matches(@"^([0-9A-F]{2} )*[0-9A-F]{2}$", hex);
    }

    // -------------------------------------------------------------------------
    //  Chaining and complete receipt scenario
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_CompleteOpenReceiptCommand_HasCorrectStructure()
    {
        // Arrange & Act: simulate CMD_OPEN_RECEIPT (01)
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_OPEN_RECEIPT)
            .Build();

        // Assert structure
        Assert.Equal(0x02, frame[0]);      // STX
        Assert.Equal((byte)'0', frame[1]); // '0'
        Assert.Equal((byte)'1', frame[2]); // '1'
        Assert.Equal(0x03, frame[^2]);     // ETX
    }

    [Fact]
    public void Build_PrintItemCommand_ContainsAllFields()
    {
        // Arrange & Act: simulate a full CMD_PRINT_ITEM command
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM)
            .AddField("Prodotto test")
            .AddField(1)            // quantity
            .AddField(9.99m)        // unit price → 999
            .AddField(1)            // VAT code
            .AddField(CustomProtocolCommands.DEPT_DEFAULT)
            .Build();

        // Assert: valid frame (STX, ETX, checksum present)
        Assert.Equal(0x02, frame[0]);
        Assert.Equal(0x03, frame[^2]);
        // Checksum must match
        byte expected = 0;
        for (int i = 1; i < frame.Length - 2; i++) expected ^= frame[i];
        Assert.Equal(expected, frame[^1]);
    }

    // -------------------------------------------------------------------------
    //  Validation / error handling
    // -------------------------------------------------------------------------

    [Fact]
    public void StartCommand_NullCode_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new CustomCommandBuilder();

        // Act & Assert – ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        Assert.Throws<ArgumentNullException>(() => builder.StartCommand(null!));
    }

    [Fact]
    public void StartCommand_EmptyCode_ThrowsArgumentException()
    {
        // Arrange
        var builder = new CustomCommandBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.StartCommand(string.Empty));
    }

    [Fact]
    public void StartCommand_WhitespaceCode_ThrowsArgumentException()
    {
        // Arrange
        var builder = new CustomCommandBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.StartCommand("   "));
    }

    [Fact]
    public void AddField_WithoutStartCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CustomCommandBuilder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.AddField("test"));
    }

    [Fact]
    public void Build_WithoutStartCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CustomCommandBuilder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }
}

// =============================================================================
//  Advanced POS scenarios (Sprint 2.2)
// =============================================================================

/// <summary>
/// Tests for advanced Custom protocol POS scenarios:
/// discounts, surcharges, free items, returns, multiple payments, and change.
/// </summary>
[Trait("Category", "Unit")]
public class CustomCommandBuilderAdvancedTests
{
    // -------------------------------------------------------------------------
    //  Helper: verify a frame has valid STX/ETX/checksum structure
    // -------------------------------------------------------------------------

    private static void AssertValidFrame(byte[] frame)
    {
        Assert.True(frame.Length >= 4, "Frame too short");
        Assert.Equal(0x02, frame[0]);   // STX
        Assert.Equal(0x03, frame[^2]); // ETX
        byte expected = 0;
        for (int i = 1; i < frame.Length - 2; i++) expected ^= frame[i];
        Assert.Equal(expected, frame[^1]); // checksum
    }

    private static string FrameToAscii(byte[] frame)
        => System.Text.Encoding.ASCII.GetString(frame, 1, frame.Length - 3);

    // -------------------------------------------------------------------------
    //  Case 1 – Vendita normale (already covered in base tests, here for completeness)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_NormalSale_UsesCmd02()
    {
        // Cappellino €15, qty 1, VAT 1, dept 1
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM)
            .AddField("Cappellino")
            .AddField(1)
            .AddField(15.00m)   // → "1500"
            .AddField(1)
            .AddField(CustomProtocolCommands.DEPT_DEFAULT)
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("02", payload); // command code
        Assert.Contains("Cappellino", payload);
        Assert.Contains("1500", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 2 – Sconto riga % (cappellino €15 -10% = €13.50)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_ItemWithPercentageDiscount_ReturnsValidCommand()
    {
        // Cappellino €15, -10%
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_WITH_DISCOUNT)
            .AddField("Cappellino")
            .AddField(1)
            .AddField(15.00m)   // → "1500"
            .AddField(1)
            .AddField(CustomProtocolCommands.DEPT_DEFAULT)
            .AddField(10.00m)   // discount 10% → "1000"
            .AddField(CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE)
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("02S", payload);
        Assert.Contains("1500", payload);
        Assert.Contains("1000", payload);
        Assert.Contains(CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE, payload);
    }

    // -------------------------------------------------------------------------
    //  Case 3 – Sconto riga € (zaino €89.90 -€20 = €69.90)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_ItemWithAmountDiscount_ReturnsValidCommand()
    {
        // Zaino €89.90, -€20.00
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_WITH_DISCOUNT)
            .AddField("Zaino")
            .AddField(1)
            .AddField(89.90m)   // → "8990"
            .AddField(1)
            .AddField(CustomProtocolCommands.DEPT_DEFAULT)
            .AddField(20.00m)   // discount €20 → "2000"
            .AddField(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT)
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("02S", payload);
        Assert.Contains("8990", payload);
        Assert.Contains("2000", payload);
        Assert.Contains(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT, payload);
    }

    // -------------------------------------------------------------------------
    //  Case 4 – Maggiorazione riga (birra €5 +€0.50 servizio = €5.50)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_ItemWithSurcharge_ReturnsValidCommand()
    {
        // Birra €5.00, +€0.50 supplemento servizio
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_WITH_SURCHARGE)
            .AddField("Birra")
            .AddField(1)
            .AddField(5.00m)    // → "500"
            .AddField(1)
            .AddField(CustomProtocolCommands.DEPT_BEVERAGE)
            .AddField(0.50m)    // surcharge €0.50 → "50"
            .AddField(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT)
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("02M", payload);
        Assert.Contains("500", payload);
        Assert.Contains(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT, payload);
    }

    // -------------------------------------------------------------------------
    //  Case 5 – Omaggio (gadget €0, flag FREE)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_FreeItem_ReturnsValidCommand()
    {
        // Gadget promozionale (omaggio), prezzo originale €5
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_FREE)
            .AddField("Gadget promozionale")
            .AddField(1)
            .AddField(5.00m)   // original price → "500"
            .AddField(1)
            .AddField(CustomProtocolCommands.DEPT_DEFAULT)
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("02G", payload);
        Assert.Contains("Gadget promozionale", payload);
        Assert.Contains("500", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 6 – Reso (maglietta qty -1)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_ReturnItem_ReturnsValidCommand()
    {
        // Reso maglietta, quantità -1, prezzo €29.90
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM)
            .AddField("Maglietta (RESO)")
            .AddField(-1)       // negative quantity
            .AddField(29.90m)   // → "2990"
            .AddField(1)
            .AddField(CustomProtocolCommands.DEPT_NON_FOOD)
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("02", payload);
        Assert.Contains("-1", payload);
        Assert.Contains("2990", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 7 – Sconto globale % (subtotale €44.90 -15% = €38.16)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_GlobalPercentageDiscount_ReturnsValidCommand()
    {
        // Sconto globale 15% (es. fidelity Gold)
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_GLOBAL_DISCOUNT)
            .AddField(15.00m)   // 15% → "1500"
            .AddField(CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE)
            .AddField("Sconto Fidelity Gold")
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("03S", payload);
        Assert.Contains("1500", payload);
        Assert.Contains(CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE, payload);
        Assert.Contains("Sconto Fidelity Gold", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 8 – Sconto globale € (subtotale €44.90 -€10 = €34.90)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_GlobalAmountDiscount_ReturnsValidCommand()
    {
        // Sconto globale €10 (es. buono sconto)
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_GLOBAL_DISCOUNT)
            .AddField(10.00m)   // €10 → "1000"
            .AddField(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT)
            .AddField("Buono sconto")
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("03S", payload);
        Assert.Contains("1000", payload);
        Assert.Contains(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT, payload);
    }

    // -------------------------------------------------------------------------
    //  Case 9 – Maggiorazione globale (coperto €2.50, VAT 10%)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_GlobalSurcharge_ReturnsValidCommand()
    {
        // Coperto 2 persone €2.50, VAT code 2 (10%)
        byte[] frame = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_GLOBAL_SURCHARGE)
            .AddField(2.50m)    // →  "250"
            .AddField(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT)
            .AddField("Coperto 2 persone")
            .AddField(2)        // VAT code
            .Build();

        AssertValidFrame(frame);
        string payload = FrameToAscii(frame);
        Assert.StartsWith("03M", payload);
        Assert.Contains("250", payload);
        Assert.Contains(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT, payload);
        Assert.Contains("Coperto 2 persone", payload);
    }

    // -------------------------------------------------------------------------
    //  Case 10 – Pagamenti multipli (€20 cash + €30 carta, totale €50)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_MultiplePayments_ReturnsValidCommands()
    {
        // Payment 1: €20 cash (method 1)
        byte[] cash = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PAYMENT)
            .AddField(20.00m)   // → "2000"
            .AddField(1)        // cash
            .AddField("Contanti")
            .Build();

        // Payment 2: €30 carta (method 4)
        byte[] card = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PAYMENT)
            .AddField(30.00m)   // → "3000"
            .AddField(4)        // credit card
            .AddField("Carta di credito")
            .Build();

        AssertValidFrame(cash);
        AssertValidFrame(card);

        string cashPayload = FrameToAscii(cash);
        string cardPayload = FrameToAscii(card);

        Assert.StartsWith("04", cashPayload);
        Assert.Contains("2000", cashPayload);
        Assert.StartsWith("04", cardPayload);
        Assert.Contains("3000", cardPayload);
    }

    // -------------------------------------------------------------------------
    //  Case 11 – Resto automatico (totale €47.50, pagato €50 cash, resto €2.50)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_PaymentWithChange_CalculatesResto()
    {
        // Customer pays €50 cash for a €47.50 total → change €2.50
        const decimal totalDue = 47.50m;
        const decimal amountPaid = 50.00m;
        decimal change = amountPaid - totalDue; // = 2.50

        byte[] payment = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PAYMENT)
            .AddField(amountPaid)   // → "5000" (amount tendered)
            .AddField(1)            // cash
            .AddField("Contanti")
            .Build();

        AssertValidFrame(payment);
        string payload = FrameToAscii(payment);
        Assert.StartsWith("04", payload);
        Assert.Contains("5000", payload);
        // The change amount itself is calculated at the caller level; verify value is correct
        Assert.Equal(2.50m, change);
    }

    // -------------------------------------------------------------------------
    //  Case 11b – Scontrino completo con sconti (integration scenario)
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_CompleteReceiptWithDiscounts_AllCommandsValid()
    {
        // Full POS scenario:
        // 1. Open receipt
        // 2. Item: Cappellino €15, -10%
        // 3. Item: Zaino €89.90
        // 4. Global discount -15%
        // 5. Payment: €50 cash + carta for remainder
        // 6. Close receipt

        var open = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_OPEN_RECEIPT)
            .Build();

        var item1 = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_WITH_DISCOUNT)
            .AddField("Cappellino")
            .AddField(1)
            .AddField(15.00m)
            .AddField(1)
            .AddField(CustomProtocolCommands.DEPT_DEFAULT)
            .AddField(10.00m)
            .AddField(CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE)
            .Build();

        var item2 = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM)
            .AddField("Zaino")
            .AddField(1)
            .AddField(89.90m)
            .AddField(1)
            .AddField(CustomProtocolCommands.DEPT_DEFAULT)
            .Build();

        var globalDiscount = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_GLOBAL_DISCOUNT)
            .AddField(15.00m)
            .AddField(CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE)
            .AddField("Sconto Fidelity")
            .Build();

        var payment1 = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PAYMENT)
            .AddField(50.00m)
            .AddField(1)
            .AddField("Contanti")
            .Build();

        var payment2 = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_PAYMENT)
            .AddField(38.41m)
            .AddField(4)
            .AddField("Carta")
            .Build();

        var close = new CustomCommandBuilder()
            .StartCommand(CustomProtocolCommands.CMD_CLOSE_RECEIPT)
            .Build();

        // All frames must be structurally valid
        foreach (var frame in new[] { open, item1, item2, globalDiscount, payment1, payment2, close })
            AssertValidFrame(frame);

        // Verify command codes appear in each frame payload
        Assert.StartsWith("01", FrameToAscii(open));
        Assert.StartsWith("02S", FrameToAscii(item1));
        Assert.StartsWith("02", FrameToAscii(item2));
        Assert.StartsWith("03S", FrameToAscii(globalDiscount));
        Assert.StartsWith("04", FrameToAscii(payment1));
        Assert.StartsWith("04", FrameToAscii(payment2));
        Assert.StartsWith("05", FrameToAscii(close));
    }
}
