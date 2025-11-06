using EventForge.Server.Data.Entities.Documents;
using Xunit;

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
}
