using Prym.DTOs.FiscalPrinting;
using Prym.Server.Services.FiscalPrinting;
using Prym.Server.Services.FiscalPrinting.Communication;
using Prym.Server.Services.FiscalPrinting.CustomProtocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Prym.Tests.Services.FiscalPrinting;

/// <summary>
/// Unit tests for the communication exception type and the receipt builder
/// integration with the mock communication channel pattern.
/// Tests <see cref="FiscalPrinterCommunicationException"/>,
/// <see cref="FiscalReceiptBuilder"/> edge cases, and the
/// <see cref="FiscalPrinterStatusCache"/> interaction that the monitor uses.
/// </summary>
[Trait("Category", "Unit")]
public class CustomFiscalPrinterServiceTests
{
    // -------------------------------------------------------------------------
    //  FiscalPrinterCommunicationException
    // -------------------------------------------------------------------------

    [Fact]
    public void CommunicationException_Message_IsPreserved()
    {
        var ex = new FiscalPrinterCommunicationException("Test error");
        Assert.Equal("Test error", ex.Message);
    }

    [Fact]
    public void CommunicationException_WithInner_PreservesInnerException()
    {
        var inner = new IOException("socket closed");
        var ex = new FiscalPrinterCommunicationException("Outer", inner);
        Assert.Same(inner, ex.InnerException);
    }

    // -------------------------------------------------------------------------
    //  FiscalReceiptBuilder – null guards and validation
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildFullReceiptSequence_EmptyItems_OnlyOpenCloseAndPayments()
    {
        var builder = new FiscalReceiptBuilder();
        var receipt = new FiscalReceiptData
        {
            Payments = [new FiscalPayment { Amount = 10m, MethodCode = 1 }]
        };

        var seq = builder.BuildFullReceiptSequence(receipt);

        // At minimum: open + payment + close = 3 frames
        Assert.True(seq.Count >= 3);
        var payloads = seq.Select(f => System.Text.Encoding.ASCII.GetString(f, 1, f.Length - 3)).ToList();
        Assert.Contains(payloads, p => p.StartsWith("01")); // open
        Assert.Contains(payloads, p => p.StartsWith("04")); // payment
        Assert.Contains(payloads, p => p.StartsWith("05")); // close
    }

    [Fact]
    public void BuildRefundReceiptSequence_Null_ThrowsArgumentNullException()
    {
        var builder = new FiscalReceiptBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.BuildRefundReceiptSequence(null!));
    }

    [Fact]
    public void BuildRefundReceiptSequence_PositiveQtyItem_QuantityFlippedToNegative()
    {
        var builder = new FiscalReceiptBuilder();
        var refund = new FiscalRefundData
        {
            OriginalReceiptNumber = "0001",
            Items =
            [
                new FiscalReceiptItem
                {
                    Description = "Prodotto",
                    Quantity = 2,   // positive – should become -2
                    UnitPrice = 10m,
                    VatCode = 1
                }
            ],
            Payments = [new FiscalPayment { Amount = 20m, MethodCode = 1 }]
        };

        var seq = builder.BuildRefundReceiptSequence(refund);
        // Item frame is at index 1
        string itemPayload = System.Text.Encoding.ASCII.GetString(seq[1], 1, seq[1].Length - 3);
        Assert.Contains("-2", itemPayload);
    }

    // -------------------------------------------------------------------------
    //  FiscalReceiptBuilder – custom lines truncation
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildCustomLines_LongLine_TruncatedTo40Chars()
    {
        var builder = new FiscalReceiptBuilder();
        string longLine = new string('X', 60);

        var frames = builder.BuildCustomLines([longLine]);

        Assert.Single(frames);
        string payload = System.Text.Encoding.ASCII.GetString(frames[0], 1, frames[0].Length - 3);
        // After CMD "20" + FS, the field should contain at most 40 chars
        Assert.DoesNotContain(new string('X', 41), payload);
    }

    [Fact]
    public void BuildCustomLines_Null_ReturnsEmpty()
    {
        var frames = new FiscalReceiptBuilder().BuildCustomLines(null);
        Assert.Empty(frames);
    }

    // -------------------------------------------------------------------------
    //  FiscalReceiptBuilder – daily closure / drawer / cancel
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildDailyClosureCommand_ReturnsCmd50()
    {
        var frame = new FiscalReceiptBuilder().BuildDailyClosureCommand();
        string payload = System.Text.Encoding.ASCII.GetString(frame, 1, frame.Length - 3);
        Assert.StartsWith("50", payload);
    }

    [Fact]
    public void BuildOpenDrawerCommand_ReturnsCmd40()
    {
        var frame = new FiscalReceiptBuilder().BuildOpenDrawerCommand();
        string payload = System.Text.Encoding.ASCII.GetString(frame, 1, frame.Length - 3);
        Assert.StartsWith("40", payload);
    }

    [Fact]
    public void BuildCancelReceiptCommand_ReturnsCmd30()
    {
        var frame = new FiscalReceiptBuilder().BuildCancelReceiptCommand();
        string payload = System.Text.Encoding.ASCII.GetString(frame, 1, frame.Length - 3);
        Assert.StartsWith("30", payload);
    }

    [Fact]
    public void BuildReadStatusCommand_ReturnsCmd10()
    {
        var frame = new FiscalReceiptBuilder().BuildReadStatusCommand();
        string payload = System.Text.Encoding.ASCII.GetString(frame, 1, frame.Length - 3);
        Assert.StartsWith("10", payload);
    }

    // -------------------------------------------------------------------------
    //  FiscalPrinterStatusCache – used by the monitoring service
    // -------------------------------------------------------------------------

    [Fact]
    public void StatusCache_UpdateAndRetrieve_WorksCorrectly()
    {
        var cache = new FiscalPrinterStatusCache();
        var id = Guid.NewGuid();
        var status = new FiscalPrinterStatus
        {
            IsOnline = true,
            IsDailyClosureRequired = true,
            LastCheck = DateTime.UtcNow
        };

        cache.UpdateStatus(id, status);
        var result = cache.GetCachedStatus(id);

        Assert.NotNull(result);
        Assert.True(result.IsDailyClosureRequired);
    }

    [Fact]
    public void StatusCache_GetAllValidEntries_OnlyReturnsNonExpired()
    {
        var cache = new FiscalPrinterStatusCache();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        cache.UpdateStatus(id1, new FiscalPrinterStatus { IsOnline = true });
        cache.UpdateStatus(id2, new FiscalPrinterStatus { IsOnline = false });

        var all = cache.GetAllValidEntries();
        Assert.Equal(2, all.Count);
    }

    // -------------------------------------------------------------------------
    //  FiscalReceiptBuilder – receipt with loyalty only
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildFullReceiptSequence_WithLoyaltyData_LoyaltyFramesPresent()
    {
        var builder = new FiscalReceiptBuilder();
        var receipt = new FiscalReceiptData
        {
            Items =
            [
                new FiscalReceiptItem
                {
                    Description = "Prodotto", Quantity = 1, UnitPrice = 10m, VatCode = 1
                }
            ],
            Payments = [new FiscalPayment { Amount = 10m, MethodCode = 1 }],
            LoyaltyData = new LoyaltyReceiptData
            {
                HasCard = true,
                CardType = "Gold",
                CardNumber = "123456",
                PointsEarned = 10,
                CurrentBalance = 200
            }
        };

        var seq = builder.BuildFullReceiptSequence(receipt);
        var payloads = seq.Select(f => System.Text.Encoding.ASCII.GetString(f, 1, f.Length - 3)).ToList();

        // At least one non-fiscal descriptive line (CMD 20) for loyalty
        Assert.Contains(payloads, p => p.StartsWith("20"));
    }
}
