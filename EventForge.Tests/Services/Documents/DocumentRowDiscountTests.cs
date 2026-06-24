using EventForge.Server.Data.Entities.Documents;
using Prym.DTOs.Common;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentRow discount calculations.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentRowDiscountTests
{
    [Fact]
    public void LineTotal_WithPercentageDiscount_CalculatesCorrectly()
    {
        // Arrange
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscount = 10m, // 10%
            DiscountType = DiscountType.Percentage
        };

        // Act
        var lineTotal = row.LineTotal;
        var discountTotal = row.DiscountTotal;

        // Assert
        Assert.Equal(180m, lineTotal); // (100 * 2) * (1 - 0.10) = 180
        Assert.Equal(20m, discountTotal); // (100 * 2) * 0.10 = 20
    }

    [Fact]
    public void LineTotal_WithValueDiscount_CalculatesCorrectly()
    {
        // Arrange
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscountValue = 30m,
            DiscountType = DiscountType.Value
        };

        // Act
        var lineTotal = row.LineTotal;
        var discountTotal = row.DiscountTotal;

        // Assert
        Assert.Equal(170m, lineTotal); // (100 * 2) - 30 = 170
        Assert.Equal(30m, discountTotal); // 30
    }

    [Fact]
    public void LineTotal_WithoutDiscount_CalculatesCorrectly()
    {
        // Arrange
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscount = 0m,
            LineDiscountValue = 0m,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var lineTotal = row.LineTotal;
        var discountTotal = row.DiscountTotal;

        // Assert
        Assert.Equal(200m, lineTotal); // 100 * 2 = 200
        Assert.Equal(0m, discountTotal);
    }

    [Fact]
    public void VatTotal_CalculatesOnDiscountedAmount()
    {
        // Arrange
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscount = 10m, // 10%
            DiscountType = DiscountType.Percentage,
            VatRate = 22m // 22%
        };

        // Act
        var lineTotal = row.LineTotal;
        var vatTotal = row.VatTotal;

        // Assert
        Assert.Equal(180m, lineTotal); // (100 * 2) * 0.9 = 180
        Assert.Equal(39.60m, vatTotal); // 180 * 0.22 = 39.60
    }

    [Fact]
    public void DiscountTotal_WithZeroQuantity_ReturnsZero()
    {
        // Arrange
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 0m,
            LineDiscount = 10m,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var discountTotal = row.DiscountTotal;

        // Assert
        Assert.Equal(0m, discountTotal);
    }

    [Fact]
    public void LineTotal_WhenValueDiscountExceedsSubtotal_ClampedToZero()
    {
        // Arrange
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscountValue = 300m, // Exceeds 200
            DiscountType = DiscountType.Value
        };

        // Act
        var lineTotal = row.LineTotal;
        var discountTotal = row.DiscountTotal;

        // Assert
        Assert.Equal(0m, lineTotal); // Should be clamped to 0
        Assert.Equal(200m, discountTotal); // Discount should be clamped to subtotal
    }

    [Fact]
    public void LineDiscountString_StoredAndReturnedUntouched()
    {
        // Arrange — verify that LineDiscountString is a plain data field, not computed
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 1m,
            LineDiscount = 14.5m, // Pre-computed equivalent of "10+5"
            LineDiscountString = "10+5",
            DiscountType = DiscountType.Percentage
        };

        // Assert — the string is preserved as-is; the decimal is used for calculations
        Assert.Equal("10+5", row.LineDiscountString);
        Assert.Equal(14.5m, row.LineDiscount);
        Assert.Equal(85.5m, row.LineTotal); // 100 * (1 - 0.145) = 85.5
        Assert.Equal(14.5m, row.DiscountTotal);
    }

    [Fact]
    public void LineTotal_WithChainedDiscountEquivalent_CalculatesCorrectly()
    {
        // "10+5" = 14.5% equivalent stored in LineDiscount
        var row = new DocumentRow
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscount = 14.5m,
            LineDiscountString = "10+5",
            DiscountType = DiscountType.Percentage
        };

        var lineTotal = row.LineTotal;
        var discountTotal = row.DiscountTotal;

        Assert.Equal(171m, lineTotal);     // 200 * (1 - 0.145) = 171
        Assert.Equal(29m, discountTotal);  // 200 * 0.145 = 29
    }
}
