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
