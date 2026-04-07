using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;

namespace EventForge.Tests.Services.FiscalPrinting;

/// <summary>
/// Unit tests for <see cref="CustomStatusParser"/> verifying 3-byte bitmap parsing,
/// paper status derivation, and critical error detection.
/// </summary>
[Trait("Category", "Unit")]
public class CustomStatusParserTests
{
    // -------------------------------------------------------------------------
    //  Normal state (all zeros)
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_AllZeroBytes_ReturnsAllFlagsOff()
    {
        // Arrange
        byte[] statusData = [0x00, 0x00, 0x00];

        // Act
        FiscalPrinterStatus status = CustomStatusParser.Parse(statusData);

        // Assert – all error/warning flags must be false
        Assert.True(status.IsOnline);
        Assert.False(status.IsPaperOut);
        Assert.False(status.IsCoverOpen);
        Assert.False(status.IsHeadError);
        Assert.False(status.IsCutterError);
        Assert.False(status.IsFiscalMemoryFull);
        Assert.False(status.IsPaperLow);
        Assert.False(status.IsFiscalMemoryAlmostFull);
        Assert.False(status.IsDrawerOpen);
        Assert.False(status.IsHeadOverheat);
        Assert.False(status.IsReceiptOpen);
        Assert.False(status.IsFiscalModeActive);
        Assert.False(status.IsDailyClosureRequired);
        Assert.Equal("OK", status.PaperStatus);
        Assert.NotNull(status.LastCheck);
    }

    // -------------------------------------------------------------------------
    //  Byte 1 – Error flags
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_Byte1Bit0Set_IsPaperOutTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x01, 0x00, 0x00]);
        Assert.True(status.IsPaperOut);
        Assert.Equal("OUT", status.PaperStatus);
    }

    [Fact]
    public void Parse_Byte1Bit1Set_IsCoverOpenTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x02, 0x00, 0x00]);
        Assert.True(status.IsCoverOpen);
    }

    [Fact]
    public void Parse_Byte1Bit2Set_IsHeadErrorTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x04, 0x00, 0x00]);
        Assert.True(status.IsHeadError);
    }

    [Fact]
    public void Parse_Byte1Bit3Set_IsCutterErrorTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x08, 0x00, 0x00]);
        Assert.True(status.IsCutterError);
    }

    [Fact]
    public void Parse_Byte1Bit4Set_IsFiscalMemoryFullTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x10, 0x00, 0x00]);
        Assert.True(status.IsFiscalMemoryFull);
    }

    // -------------------------------------------------------------------------
    //  Byte 2 – Warning / info flags
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_Byte2Bit0Set_IsPaperLowTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x00, 0x01, 0x00]);
        Assert.True(status.IsPaperLow);
        Assert.Equal("LOW", status.PaperStatus);
    }

    [Fact]
    public void Parse_Byte2Bit1Set_IsFiscalMemoryAlmostFullTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x00, 0x02, 0x00]);
        Assert.True(status.IsFiscalMemoryAlmostFull);
    }

    [Fact]
    public void Parse_Byte2Bit2Set_IsDrawerOpenTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x00, 0x04, 0x00]);
        Assert.True(status.IsDrawerOpen);
    }

    [Fact]
    public void Parse_Byte2Bit3Set_IsHeadOverheatTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x00, 0x08, 0x00]);
        Assert.True(status.IsHeadOverheat);
    }

    // -------------------------------------------------------------------------
    //  Byte 3 – Operational flags
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_Byte3Bit0Set_IsReceiptOpenTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x00, 0x00, 0x01]);
        Assert.True(status.IsReceiptOpen);
    }

    [Fact]
    public void Parse_Byte3Bit1Set_IsFiscalModeActiveTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x00, 0x00, 0x02]);
        Assert.True(status.IsFiscalModeActive);
    }

    [Fact]
    public void Parse_Byte3Bit2Set_IsDailyClosureRequiredTrue()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x00, 0x00, 0x04]);
        Assert.True(status.IsDailyClosureRequired);
    }

    // -------------------------------------------------------------------------
    //  Paper status derivation
    // -------------------------------------------------------------------------

    [Fact]
    public void GetPaperStatusString_PaperOut_ReturnsOut()
    {
        var status = new FiscalPrinterStatus { IsPaperOut = true };
        Assert.Equal("OUT", CustomStatusParser.GetPaperStatusString(status));
    }

    [Fact]
    public void GetPaperStatusString_PaperLow_ReturnsLow()
    {
        var status = new FiscalPrinterStatus { IsPaperLow = true };
        Assert.Equal("LOW", CustomStatusParser.GetPaperStatusString(status));
    }

    [Fact]
    public void GetPaperStatusString_NormalState_ReturnsOk()
    {
        var status = new FiscalPrinterStatus();
        Assert.Equal("OK", CustomStatusParser.GetPaperStatusString(status));
    }

    [Fact]
    public void GetPaperStatusString_BothOutAndLow_ReturnsOut()
    {
        // IsPaperOut takes precedence over IsPaperLow
        var status = new FiscalPrinterStatus { IsPaperOut = true, IsPaperLow = true };
        Assert.Equal("OUT", CustomStatusParser.GetPaperStatusString(status));
    }

    // -------------------------------------------------------------------------
    //  HasCriticalErrors
    // -------------------------------------------------------------------------

    [Fact]
    public void HasCriticalErrors_AllFlagsOff_ReturnsFalse()
    {
        var status = new FiscalPrinterStatus();
        Assert.False(CustomStatusParser.HasCriticalErrors(status));
    }

    [Fact]
    public void HasCriticalErrors_PaperOut_ReturnsTrue()
    {
        var status = new FiscalPrinterStatus { IsPaperOut = true };
        Assert.True(CustomStatusParser.HasCriticalErrors(status));
    }

    [Fact]
    public void HasCriticalErrors_FiscalMemoryFull_ReturnsTrue()
    {
        var status = new FiscalPrinterStatus { IsFiscalMemoryFull = true };
        Assert.True(CustomStatusParser.HasCriticalErrors(status));
    }

    [Fact]
    public void HasCriticalErrors_HeadError_ReturnsTrue()
    {
        var status = new FiscalPrinterStatus { IsHeadError = true };
        Assert.True(CustomStatusParser.HasCriticalErrors(status));
    }

    [Fact]
    public void HasCriticalErrors_CutterError_ReturnsTrue()
    {
        var status = new FiscalPrinterStatus { IsCutterError = true };
        Assert.True(CustomStatusParser.HasCriticalErrors(status));
    }

    [Fact]
    public void HasCriticalErrors_OnlyWarnings_ReturnsFalse()
    {
        // Warnings (low paper, drawer open, overheat) are not critical
        var status = new FiscalPrinterStatus
        {
            IsPaperLow = true,
            IsDrawerOpen = true,
            IsHeadOverheat = true,
            IsFiscalMemoryAlmostFull = true
        };
        Assert.False(CustomStatusParser.HasCriticalErrors(status));
    }

    // -------------------------------------------------------------------------
    //  Edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_NullInput_ReturnsOnlineWithUnknownPaperStatus()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse(null);
        Assert.True(status.IsOnline);
        Assert.Equal("UNKNOWN", status.PaperStatus);
        Assert.False(status.IsPaperOut);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsOnlineWithUnknownPaperStatus()
    {
        FiscalPrinterStatus status = CustomStatusParser.Parse([]);
        Assert.True(status.IsOnline);
        Assert.Equal("UNKNOWN", status.PaperStatus);
    }

    [Fact]
    public void Parse_OnlyOneByte_ParsesByte1Only()
    {
        // Only byte 1 provided (paper out bit set), bytes 2 and 3 default to false
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x01]);
        Assert.True(status.IsPaperOut);
        Assert.False(status.IsPaperLow);
        Assert.False(status.IsDailyClosureRequired);
    }

    [Fact]
    public void Parse_AllBytesFf_AllKnownFlagsTrue()
    {
        // All bits set – every flag we use should be true
        FiscalPrinterStatus status = CustomStatusParser.Parse([0xFF, 0xFF, 0xFF]);
        Assert.True(status.IsPaperOut);
        Assert.True(status.IsCoverOpen);
        Assert.True(status.IsHeadError);
        Assert.True(status.IsCutterError);
        Assert.True(status.IsFiscalMemoryFull);
        Assert.True(status.IsPaperLow);
        Assert.True(status.IsFiscalMemoryAlmostFull);
        Assert.True(status.IsDrawerOpen);
        Assert.True(status.IsHeadOverheat);
        Assert.True(status.IsReceiptOpen);
        Assert.True(status.IsFiscalModeActive);
        Assert.True(status.IsDailyClosureRequired);
        Assert.Equal("OUT", status.PaperStatus);
        Assert.True(CustomStatusParser.HasCriticalErrors(status));
    }

    [Fact]
    public void Parse_SetsLastCheckToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        FiscalPrinterStatus status = CustomStatusParser.Parse([0x00, 0x00, 0x00]);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.NotNull(status.LastCheck);
        Assert.InRange(status.LastCheck!.Value, before, after);
    }
}
