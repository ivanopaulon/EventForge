using Prym.Server.Services.FiscalPrinting.CustomProtocol;

namespace Prym.Tests.Services.FiscalPrinting;

/// <summary>
/// Unit tests for <see cref="CustomResponseParser"/> verifying ACK/NAK detection,
/// data frame extraction, and checksum verification.
/// </summary>
[Trait("Category", "Unit")]
public class CustomResponseParserTests
{
    // -------------------------------------------------------------------------
    //  Helper: build a valid data frame with correct checksum
    // -------------------------------------------------------------------------

    private static byte[] BuildDataFrame(string payload)
    {
        // STX | payload bytes | ETX | checksum
        var payloadBytes = System.Text.Encoding.ASCII.GetBytes(payload);
        var frame = new List<byte> { 0x02 };
        frame.AddRange(payloadBytes);
        frame.Add(0x03);

        byte checksum = CustomResponseParser.ComputeChecksum([.. frame], 1, frame.Count - 1);
        frame.Add(checksum);

        return [.. frame];
    }

    // -------------------------------------------------------------------------
    //  ACK / NAK detection
    // -------------------------------------------------------------------------

    [Fact]
    public void IsAck_SingleAckByte_ReturnsTrue()
    {
        Assert.True(CustomResponseParser.IsAck([0x06]));
    }

    [Fact]
    public void IsAck_SingleNakByte_ReturnsFalse()
    {
        Assert.False(CustomResponseParser.IsAck([0x15]));
    }

    [Fact]
    public void IsAck_Null_ReturnsFalse()
    {
        Assert.False(CustomResponseParser.IsAck(null));
    }

    [Fact]
    public void IsAck_EmptyArray_ReturnsFalse()
    {
        Assert.False(CustomResponseParser.IsAck([]));
    }

    [Fact]
    public void IsNak_SingleNakByte_ReturnsTrue()
    {
        Assert.True(CustomResponseParser.IsNak([0x15]));
    }

    [Fact]
    public void IsNak_SingleAckByte_ReturnsFalse()
    {
        Assert.False(CustomResponseParser.IsNak([0x06]));
    }

    [Fact]
    public void IsNak_Null_ReturnsFalse()
    {
        Assert.False(CustomResponseParser.IsNak(null));
    }

    // -------------------------------------------------------------------------
    //  ParseResponse
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseResponse_AckByte_ReturnsAckType()
    {
        var result = CustomResponseParser.ParseResponse([0x06]);
        Assert.Equal(CustomResponseType.Ack, result.Type);
        Assert.True(result.IsChecksumValid);
    }

    [Fact]
    public void ParseResponse_NakByte_ReturnsNakType()
    {
        var result = CustomResponseParser.ParseResponse([0x15]);
        Assert.Equal(CustomResponseType.Nak, result.Type);
        Assert.True(result.IsChecksumValid);
    }

    [Fact]
    public void ParseResponse_NullInput_ReturnsUnknownType()
    {
        var result = CustomResponseParser.ParseResponse(null);
        Assert.Equal(CustomResponseType.Unknown, result.Type);
        Assert.False(result.IsChecksumValid);
    }

    [Fact]
    public void ParseResponse_EmptyInput_ReturnsUnknownType()
    {
        var result = CustomResponseParser.ParseResponse([]);
        Assert.Equal(CustomResponseType.Unknown, result.Type);
    }

    [Fact]
    public void ParseResponse_ValidDataFrame_ReturnsDataType()
    {
        byte[] frame = BuildDataFrame("HELLO");
        var result = CustomResponseParser.ParseResponse(frame);
        Assert.Equal(CustomResponseType.Data, result.Type);
    }

    [Fact]
    public void ParseResponse_ValidDataFrame_ExtractsPayload()
    {
        byte[] frame = BuildDataFrame("0001234");
        var result = CustomResponseParser.ParseResponse(frame);
        Assert.Equal("0001234", result.Data);
    }

    [Fact]
    public void ParseResponse_ValidDataFrame_ChecksumIsValid()
    {
        byte[] frame = BuildDataFrame("STATUSDATA");
        var result = CustomResponseParser.ParseResponse(frame);
        Assert.True(result.IsChecksumValid);
        Assert.Equal(result.ComputedChecksum, result.RawChecksum);
    }

    [Fact]
    public void ParseResponse_CorruptedChecksum_ChecksumIsInvalid()
    {
        byte[] frame = BuildDataFrame("STATUSDATA");
        // Corrupt the checksum byte (last byte)
        frame[^1] ^= 0xFF;

        var result = CustomResponseParser.ParseResponse(frame);
        Assert.Equal(CustomResponseType.Data, result.Type);
        Assert.False(result.IsChecksumValid);
        Assert.NotEqual(result.ComputedChecksum, result.RawChecksum);
    }

    // -------------------------------------------------------------------------
    //  TryExtractData
    // -------------------------------------------------------------------------

    [Fact]
    public void TryExtractData_ValidDataFrame_ReturnsTrueAndPayload()
    {
        byte[] frame = BuildDataFrame("RECEIPT42");
        bool success = CustomResponseParser.TryExtractData(frame, out string? data);
        Assert.True(success);
        Assert.Equal("RECEIPT42", data);
    }

    [Fact]
    public void TryExtractData_AckFrame_ReturnsFalseAndNullData()
    {
        bool success = CustomResponseParser.TryExtractData([0x06], out string? data);
        Assert.False(success);
        Assert.Null(data);
    }

    [Fact]
    public void TryExtractData_NakFrame_ReturnsFalseAndNullData()
    {
        bool success = CustomResponseParser.TryExtractData([0x15], out string? data);
        Assert.False(success);
        Assert.Null(data);
    }

    [Fact]
    public void TryExtractData_NullInput_ReturnsFalse()
    {
        bool success = CustomResponseParser.TryExtractData(null, out string? data);
        Assert.False(success);
        Assert.Null(data);
    }

    // -------------------------------------------------------------------------
    //  VerifyChecksum
    // -------------------------------------------------------------------------

    [Fact]
    public void VerifyChecksum_ValidDataFrame_ReturnsTrue()
    {
        byte[] frame = BuildDataFrame("TEST");
        Assert.True(CustomResponseParser.VerifyChecksum(frame));
    }

    [Fact]
    public void VerifyChecksum_CorruptedFrame_ReturnsFalse()
    {
        byte[] frame = BuildDataFrame("TEST");
        frame[^1] = (byte)(frame[^1] ^ 0xFF); // corrupt checksum
        Assert.False(CustomResponseParser.VerifyChecksum(frame));
    }

    [Fact]
    public void VerifyChecksum_AckFrame_ReturnsTrue()
    {
        // ACK is considered valid (no checksum needed for single-byte frames)
        Assert.True(CustomResponseParser.VerifyChecksum([0x06]));
    }

    [Fact]
    public void VerifyChecksum_NullInput_ReturnsFalse()
    {
        Assert.False(CustomResponseParser.VerifyChecksum(null));
    }
}
