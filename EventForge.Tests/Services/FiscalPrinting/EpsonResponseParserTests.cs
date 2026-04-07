using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting.EpsonProtocol;

namespace EventForge.Tests.Services.FiscalPrinting;

/// <summary>
/// Unit tests for <see cref="EpsonResponseParser"/> – verifies SOAP response parsing,
/// status bitmask decoding, and error code mapping.
/// </summary>
[Trait("Category", "Unit")]
public class EpsonResponseParserTests
{
    // -------------------------------------------------------------------------
    //  Success response
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseResponse_SuccessTrue_ReturnsSuccess()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
              <SOAP-ENV:Body>
                <response xmlns="http://www.epson-pos.com/schemas/2011/03/epos-print"
                          success="true" code="" status="4"/>
              </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>
            """;

        var result = EpsonResponseParser.ParseResponse(xml);

        Assert.True(result.Success);
        Assert.Equal(string.Empty, result.Code);
        Assert.Equal("4", result.Status);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ParseResponse_SuccessFalse_ReturnsFailure()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
              <SOAP-ENV:Body>
                <response xmlns="http://www.epson-pos.com/schemas/2011/03/epos-print"
                          success="false" code="EPTR_REC_EMPTY" status="64"/>
              </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>
            """;

        var result = EpsonResponseParser.ParseResponse(xml);

        Assert.False(result.Success);
        Assert.Equal("EPTR_REC_EMPTY", result.Code);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Paper", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseResponse_EmptyString_ReturnsFailure()
    {
        var result = EpsonResponseParser.ParseResponse(string.Empty);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void ParseResponse_MalformedXml_ReturnsFailure()
    {
        var result = EpsonResponseParser.ParseResponse("<not valid xml>>>");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    // -------------------------------------------------------------------------
    //  Error code mapping
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("EPTR_COVER_OPEN", "cover")]
    [InlineData("EPTR_REC_EMPTY", "paper")]
    [InlineData("EPTR_MECHANICAL", "mechanical")]
    [InlineData("EPTR_AUTOCUTTER", "cutter")]
    [InlineData("EPTR_UNRECOVERABLE", "unrecoverable")]
    [InlineData("SchemaError", "XML")]
    [InlineData("DeviceNotFound", "device")]
    [InlineData("EX_TIMEOUT", "timed")]
    public void ParseResponse_KnownErrorCodes_ContainDescriptiveMessage(
        string code,
        string expectedKeyword)
    {
        var xml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
              <SOAP-ENV:Body>
                <response xmlns="http://www.epson-pos.com/schemas/2011/03/epos-print"
                          success="false" code="{code}" status="0"/>
              </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>
            """;

        var result = EpsonResponseParser.ParseResponse(xml);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(expectedKeyword, result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    //  Status bitmask parsing
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseStatusBits_OnlineBitSet_IsOnlineTrue()
    {
        var status = new FiscalPrinterStatus();
        // bit 2 (0x04) = online
        EpsonResponseParser.ParseStatusBits("4", status);

        Assert.True(status.IsOnline);
    }

    [Fact]
    public void ParseStatusBits_DrawerBitSet_IsDrawerOpenTrue()
    {
        var status = new FiscalPrinterStatus();
        // bit 1 (0x02) = drawer open
        EpsonResponseParser.ParseStatusBits("2", status);

        Assert.True(status.IsDrawerOpen);
    }

    [Fact]
    public void ParseStatusBits_PaperOutBitSet_IsPaperOutTrue()
    {
        var status = new FiscalPrinterStatus();
        // bit 6 (0x40 = 64) = paper out
        EpsonResponseParser.ParseStatusBits("64", status);

        Assert.True(status.IsPaperOut);
    }

    [Fact]
    public void ParseStatusBits_PaperNearEndBitSet_IsPaperLowTrue()
    {
        var status = new FiscalPrinterStatus();
        // bit 5 (0x20 = 32) = paper near-end
        EpsonResponseParser.ParseStatusBits("32", status);

        Assert.True(status.IsPaperLow);
    }

    [Fact]
    public void ParseStatusBits_CoverOpenBitSet_IsCoverOpenTrue()
    {
        var status = new FiscalPrinterStatus();
        // bit 9 (0x200 = 512) = cover open
        EpsonResponseParser.ParseStatusBits("512", status);

        Assert.True(status.IsCoverOpen);
    }

    [Fact]
    public void ParseStatusBits_AllOnline_NoDrawerOrPaperIssue()
    {
        var status = new FiscalPrinterStatus();
        // bit 2 only → online, no issues
        EpsonResponseParser.ParseStatusBits("4", status);

        Assert.True(status.IsOnline);
        Assert.False(status.IsDrawerOpen);
        Assert.False(status.IsPaperOut);
        Assert.False(status.IsPaperLow);
        Assert.False(status.IsCoverOpen);
    }

    [Fact]
    public void ParseStatusBits_HexString_ParsedCorrectly()
    {
        var status = new FiscalPrinterStatus();
        // decimal 70 = 0x46 = 0x04 (online) | 0x02 (drawer open) | 0x40 (paper out)
        EpsonResponseParser.ParseStatusBits("70", status);

        Assert.True(status.IsOnline);
        Assert.True(status.IsDrawerOpen);
        Assert.True(status.IsPaperOut);
    }

    // -------------------------------------------------------------------------
    //  ParseStatusResponse
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseStatusResponse_SuccessWithOnlineBit_ReturnsOnlineTrue()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
              <SOAP-ENV:Body>
                <response xmlns="http://www.epson-pos.com/schemas/2011/03/epos-print"
                          success="true" code="" status="4"/>
              </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>
            """;

        var status = EpsonResponseParser.ParseStatusResponse(xml);

        Assert.True(status.IsOnline);
    }

    [Fact]
    public void ParseStatusResponse_FailureResponse_IsOnlineFalse()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
              <SOAP-ENV:Body>
                <response xmlns="http://www.epson-pos.com/schemas/2011/03/epos-print"
                          success="false" code="DeviceNotFound" status="0"/>
              </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>
            """;

        var status = EpsonResponseParser.ParseStatusResponse(xml);

        Assert.False(status.IsOnline);
        Assert.NotNull(status.LastError);
    }

    [Fact]
    public void ParseStatusResponse_PaperOutBit_PaperStatusIsOut()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
              <SOAP-ENV:Body>
                <response xmlns="http://www.epson-pos.com/schemas/2011/03/epos-print"
                          success="true" code="" status="68"/>
              </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>
            """;
        // 68 = 0x44 = 0x04 (online) | 0x40 (paper out)

        var status = EpsonResponseParser.ParseStatusResponse(xml);

        Assert.True(status.IsPaperOut);
        Assert.Equal("OUT", status.PaperStatus);
    }
}
