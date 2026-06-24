using EventForge.Server.Data.Entities.Documents;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace EventForge.Tests.Services.Warehouse;

/// <summary>
/// Unit tests verifying that the effective net unit price formula
/// (UnitPrice × (1 − LineDiscount/100)) is correctly applied to
/// DocumentRow.LineTotal and that DiscountStringParser feeds the
/// right equivalent percentage for chained discounts.
/// These tests mirror the logic in StockReconciliationService.ComputeNetUnitPrice
/// and DocumentHeaderService.ComputeNetUnitPrice (both private helpers).
/// </summary>
[Trait("Category", "Unit")]
public class NetUnitPriceFormulaTests
{
    // ── DocumentRow.LineTotal already uses UnitPrice × (1 − LineDiscount/100) ──

    [Fact]
    public void DocumentRow_NoDiscount_LineTotalEqualsUnitPriceTimesQty()
    {
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscount = 0m,
            DiscountType = DiscountType.Percentage,
            Description = "Test"
        };

        Assert.Equal(200m, row.LineTotal);
    }

    [Fact]
    public void DocumentRow_SingleDiscount20Percent_LineTotalIsNet()
    {
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 1m,
            LineDiscount = 20m,
            DiscountType = DiscountType.Percentage,
            Description = "Test"
        };

        // 100 × (1 − 0.20) = 80
        Assert.Equal(80m, row.LineTotal);
    }

    [Fact]
    public void DocumentRow_ChainedDiscount10Plus5_LineTotalIsNet()
    {
        // "10+5" → DiscountStringParser computes 14.5%
        var parseResult = DiscountStringParser.Parse("10+5");
        Assert.True(parseResult.IsValid);

        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 1m,
            LineDiscount = parseResult.EquivalentPercentage,   // 14.5%
            LineDiscountString = "10+5",
            DiscountType = DiscountType.Percentage,
            Description = "Test"
        };

        // 100 × (1 − 0.145) = 85.5
        Assert.Equal(85.5m, row.LineTotal);
    }

    [Fact]
    public void DocumentRow_ChainedDiscount10Plus5Plus2_LineTotalIsNet()
    {
        // "10+5+2" cascaded: 1 − (0.90 × 0.95 × 0.98) = 1 − 0.8379 ≈ 16.21%
        var parseResult = DiscountStringParser.Parse("10+5+2");
        Assert.True(parseResult.IsValid);

        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 1m,
            LineDiscount = parseResult.EquivalentPercentage,
            LineDiscountString = "10+5+2",
            DiscountType = DiscountType.Percentage,
            Description = "Test"
        };

        var expected = Math.Round(100m * (1m - parseResult.EquivalentPercentage / 100m), 2);
        Assert.Equal(expected, row.LineTotal);
    }

    // ── ComputeNetUnitPrice formula isolated ────────────────────────────────

    /// <summary>
    /// Mimics the private ComputeNetUnitPrice helper used by both
    /// StockReconciliationService and DocumentHeaderService.
    /// </summary>
    private static decimal ComputeNetUnitPrice(decimal unitPrice, decimal lineDiscount)
    {
        if (lineDiscount <= 0m)
            return unitPrice;
        var netPrice = unitPrice * (1m - lineDiscount / 100m);
        return Math.Round(netPrice, 6, MidpointRounding.AwayFromZero);
    }

    [Fact]
    public void ComputeNetUnitPrice_NoDiscount_ReturnsUnitPrice()
    {
        Assert.Equal(100m, ComputeNetUnitPrice(100m, 0m));
    }

    [Fact]
    public void ComputeNetUnitPrice_SingleDiscount20_Returns80()
    {
        Assert.Equal(80m, ComputeNetUnitPrice(100m, 20m));
    }

    [Fact]
    public void ComputeNetUnitPrice_ChainedEquivalent14Point5_Returns85Point5()
    {
        // 10+5 cascaded ≈ 14.5%
        Assert.Equal(85.5m, ComputeNetUnitPrice(100m, 14.5m));
    }

    [Fact]
    public void ComputeNetUnitPrice_ZeroUnitPrice_ReturnsZero()
    {
        Assert.Equal(0m, ComputeNetUnitPrice(0m, 10m));
    }

    [Fact]
    public void ComputeNetUnitPrice_FullDiscount100_ReturnsZero()
    {
        Assert.Equal(0m, ComputeNetUnitPrice(100m, 100m));
    }

    // ── DiscountStringParser feeds the right equivalent to the formula ───────

    [Theory]
    [InlineData("10+5", 100, 85.5)]
    [InlineData("20", 100, 80)]
    [InlineData("0", 100, 100)]
    [InlineData("10+10+10", 100, 72.9)]     // 1 − 0.9^3 = 27.1% → 72.9 net
    public void DiscountParser_FeedsCorrectEquivalent_FormulaProducesExpectedNet(
        string discountString, decimal unitPrice, decimal expectedNet)
    {
        var result = DiscountStringParser.Parse(discountString);
        Assert.True(result.IsValid);

        var net = ComputeNetUnitPrice(unitPrice, result.EquivalentPercentage);

        Assert.Equal(Math.Round(expectedNet, 4), Math.Round(net, 4));
    }
}
